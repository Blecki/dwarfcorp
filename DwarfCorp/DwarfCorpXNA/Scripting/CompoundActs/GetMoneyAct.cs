// GetResourcesAct.cs
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
using System.Text;

namespace DwarfCorp
{

    /// <summary>
    /// A creature finds money at a treasury and puts it in his/her wallet.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GetMoneyAct : CompoundCreatureAct
    {
        public DwarfBux Money { get; set; }
        public Faction Faction { get; set; }
        public GetMoneyAct()
        {

        }

        public GetMoneyAct(CreatureAI agent, DwarfBux money, Faction faction = null) :
            base(agent)
        {
            Name = "Get Money";
            Money = money;
            if (faction == null)
            {
                Faction = Creature.Faction;
            }
            else
            {
                Faction = faction;
            }
        }

        public IEnumerable<Act.Status> GetNextTreasury()
        {
            List<Treasury> treasuries = new List<Treasury>(Faction.Treasurys);
            treasuries.Sort((a, b) => a == b ? 0 : dist(a) < dist(b) ? -1 : 1);

            foreach (var zone in treasuries)
            {
                if (zone.Money > 0)
                {
                    Agent.Blackboard.SetData<Treasury>("Treasury", zone);
                    yield return Act.Status.Success;
                    yield break;
                }
            }
            Agent.Blackboard.SetData<Treasury>("Treasury", null);
            yield return Act.Status.Fail;
        }

        public IEnumerable<Act.Status> SetMoneyNeeded(DwarfBux money)
        {
            Agent.Blackboard.SetData<DwarfBux>("MoneyNeeded", money);
            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> ShouldContinue()
        {
            if (!Agent.Blackboard.Has("MoneyNeeded"))
            {
                yield return Act.Status.Fail;
                yield break;
            }
            var needed = Agent.Blackboard.GetData<DwarfBux>("MoneyNeeded");
            if (needed <= 0)
            {
                yield return Act.Status.Fail;
                yield break;
            }

            if (Faction.Economy.CurrentMoney < needed)
            {
                Agent.World.MakeAnnouncement(String.Format("Could not pay {0}, not enough money!", Agent.Stats.FullName));
                yield return Act.Status.Fail;
                yield break;
            }
            
            yield return Act.Status.Success;
        }



        private float dist(Zone zone)
        {
            return (zone.GetBoundingBox().Center() - Creature.AI.Position).LengthSquared();
        }

        public override void Initialize()
        {
            Agent.Blackboard.SetData<DwarfBux>("MoneyNeeded", Money);

            Tree = new WhileLoop(new Sequence(new Wrap(() => GetNextTreasury()),
                                              new GoToZoneAct(Agent, "Treasury"),
                                              new StashMoneyAct(Agent, "MoneyNeeded", "Treasury")),
                                 new Wrap(() => ShouldContinue())
                )         ;

            base.Initialize();

        }
    }

}