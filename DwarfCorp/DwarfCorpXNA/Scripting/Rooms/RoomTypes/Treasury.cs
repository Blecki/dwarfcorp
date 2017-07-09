// Treasury.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json;
using System.Runtime.Serialization;


namespace DwarfCorp
{

    [JsonObject(IsReference = true)]
    public class Treasury : Room
    {
        private static uint maxID = 0;
        public List<Body> Coins { get; set; }
        public static string TreasuryName = "Treasury";
        public Faction Faction { get; set; }

        public static DwarfBux MoneyPerPile = 64m;

        [JsonProperty]
        private DwarfBux _money = 0m;

        [JsonIgnore]
        public DwarfBux Money
        {
            get { return _money; }
            set
            {
                _money = value;
                HandleCoins();
            }
        }

        public static uint NextID()
        {
            maxID++;
            return maxID;
        }

        public Treasury()
        {
            Coins = new List<Body>();
            ReplacementType = VoxelLibrary.GetVoxelType("Blue Tile");
            Faction = null;
        }

        public Treasury(Faction faction, IEnumerable<VoxelHandle> voxels, WorldManager world) :
            base(voxels, RoomLibrary.GetData(TreasuryName), world, faction)
        {
            Coins = new List<Body>();
            ReplacementType = VoxelLibrary.GetVoxelType("Blue Tile");
            if (faction.Treasurys.Count == 0)
            {
                DwarfBux factionmoney = faction.Economy.CurrentMoney;
                Money = Math.Min(factionmoney, Voxels.Count*MoneyPerPile);
            }
            faction.Treasurys.Add(this);
            Faction = faction;
        }

        public Treasury(Faction faction, bool designation, IEnumerable<VoxelHandle> designations, RoomData data, WorldManager world) :
            base(designation, designations, data, world, faction)
        {
            Coins = new List<Body>();
            if (faction.Treasurys.Count == 0)
            {
                Money = Faction.Economy.CurrentMoney;
                DwarfBux factionmoney = faction.Economy.CurrentMoney;
                Money = Math.Min(factionmoney, Voxels.Count*MoneyPerPile);
            }
            faction.Treasurys.Add(this);
            Faction = faction;
        }

        public void KillCoins(Body component)
        {
            ZoneBodies.Remove(component);
            EaseMotion deathMotion = new EaseMotion(0.8f, component.LocalTransform, component.LocalTransform.Translation + new Vector3(0, -1, 0));
            component.AnimationQueue.Add(deathMotion);
            deathMotion.OnComplete += component.Die;
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_ic_dwarf_stash_money, component.LocalTransform.Translation);
            Faction.World.ParticleManager.Trigger("puff", component.LocalTransform.Translation + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 90);
        }

        public void CreateCoins(Vector3 pos)
        {
            Vector3 startPos = pos + new Vector3(0.5f, -0.1f, 0.5f);
            Vector3 endPos = pos + new Vector3(0.5f, 1.5f, 0.5f);

            Body coins = EntityFactory.CreateEntity<Body>("Coins", startPos);
            coins.AnimationQueue.Add(new EaseMotion(0.8f, coins.LocalTransform, endPos));
            Coins.Add(coins);
            AddBody(coins);
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_ic_dwarf_stash_money, startPos);
            if (Faction != null)
                Faction.World.ParticleManager.Trigger("puff", pos + new Vector3(0.5f, 1.5f, 0.5f), Color.White, 90);
        }

        public void HandleCoins()
        {
            if (Voxels == null || Coins == null)
            {
                return;
            }

            if (Voxels.Count == 0)
            {
                foreach (Body component in Coins)
                {
                    KillCoins(component);
                }
                Coins.Clear();
            }

            int numCoins = (int)Math.Min(Math.Max(Money / MoneyPerPile, 1), Voxels.Count);
            if (Money == 0m)
            {
                numCoins = 0;
            }
            if (Coins.Count > numCoins)
            {
                for (int i = Coins.Count - 1; i >= numCoins; i--)
                {
                    KillCoins(Coins[i]);
                    Coins.RemoveAt(i);
                }
            }
            else if (Coins.Count < numCoins)
            {
                for (int i = Coins.Count; i < numCoins; i++)
                {
                    CreateCoins(Voxels[i].Position + VertexNoise.GetNoiseVectorFromRepeatingTexture(Voxels[i].Position));
                }
            }

            if (IsFull() && Faction.Treasurys.Count(t => !t.IsFull()) == 0 && Voxels.Count > 0)
            {
                Faction.World.MakeAnnouncement("Our treasury is full! Build more treasuries to store more money.");
            }
        }


        public bool RemoveMoney(Vector3 dwarfPos, DwarfBux money)
        {
            if (Money < money)
            {
                return false;
            }

            Body lastCoins = Coins.LastOrDefault();

            if (lastCoins == null)
            {
                return false;
            }
            Body component = EntityFactory.CreateEntity<Body>("Coins", lastCoins.Position);
            TossMotion toss = new TossMotion(1.0f, 2.5f, component.LocalTransform, dwarfPos);
            component.AnimationQueue.Add(toss);
            toss.OnComplete += component.Die;

            Money -= money;
            Faction.Economy.CurrentMoney -= money;
            return true;
        }

        public bool AddMoney(Vector3 dwarfPos, DwarfBux money, ref DwarfBux moneyPut)
        {
            DwarfBux totalMoney = MoneyPerPile*Voxels.Count;
            DwarfBux moneyToPut = money;
            if (Money + money > totalMoney)
            {
                DwarfBux remainder = totalMoney - Money;

                if (remainder <= 0)
                {
                    moneyPut = 0;
                    return false;
                }

                moneyToPut = remainder;
            }

            Vector3 targetToss = Coins.Count == 0 ? Voxels[0].Position + new Vector3(0.5f, 0.5f, 0.5f) : Coins[Coins.Count - 1].LocalTransform.Translation + new Vector3(0.5f, 0.5f, 0.5f);
            Body component = EntityFactory.CreateEntity<Body>("Coins", dwarfPos);
            TossMotion toss = new TossMotion(1.0f, 2.5f, component.LocalTransform,
               targetToss);
            component.AnimationQueue.Add(toss);
            toss.OnComplete += component.Die;

            Money += moneyToPut;
            Faction.Economy.CurrentMoney += moneyToPut;
            moneyPut = moneyToPut;
            return moneyPut >= money;
        }


        public override void Destroy()
        {
            DwarfBux moneyLeft = Money;
            foreach (Body coinPile in Coins)
            {
                CoinPile coins = EntityFactory.CreateEntity<CoinPile>("Coins Resource", coinPile.Position);
                coins.Money = Math.Min(MoneyPerPile, moneyLeft);
                moneyLeft -= coins.Money;
            }
            Faction.Economy.CurrentMoney -= Money;
            if (Faction != null)
            {
                Faction.Treasurys.Remove(this);
            }
            base.Destroy();
        }

        public override void RecalculateMaxResources()
        {
            HandleCoins();
            base.RecalculateMaxResources();
        }

        public static RoomData InitializeData()
        {
            List<RoomTemplate> TreasuryTemplates = new List<RoomTemplate>();
            Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> roomResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>()
            {
            };

            return new RoomData(TreasuryName, 12, "Blue Tile", roomResources, TreasuryTemplates,
                new Gui.TileReference("rooms", 14))
            {
                Description = "Money is stored here. Can store " + MoneyPerPile + " per tile.",
            };
        }

        public override bool IsFull()
        {
            return Money >= MoneyPerPile*Voxels.Count;
        }

        public bool CanAddMoney(DwarfBux toPut)
        {
            return Money + toPut <= MoneyPerPile*Voxels.Count;
        }
    }

}
