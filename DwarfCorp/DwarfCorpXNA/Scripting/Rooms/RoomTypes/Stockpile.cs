// Stockpile.cs
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

namespace DwarfCorp
{

    /// <summary>
    /// A stockpile is a kind of zone which contains items on top of it.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Stockpile : Room
    {
        [RoomFactory("Stockpile")]
        private static Room _factory(RoomData Data, Faction Faction, WorldManager World)
        {
            return new Stockpile(Data, Faction, World);
        }

        public Stockpile()
        {

        }

        protected Stockpile(RoomData Data, Faction Faction, WorldManager World) :
            base(Data, World, Faction)
        {
            Boxes = new List<Body>();
            Faction.Stockpiles.Add(this);
            ReplacementType = VoxelLibrary.GetVoxelType("Stockpile");
            this.Faction = Faction;
            BlacklistResources = new List<Resource.ResourceTags>()
            {
                Resource.ResourceTags.Corpse,
                Resource.ResourceTags.Money
            };

        }

        private static uint maxID = 0;
        public List<Body> Boxes { get; set; }
        public static string StockpileName = "Stockpile";
        public string BoxType = "Crate";
        public Vector3 BoxOffset = Vector3.Zero;

        // If this is empty, all resources are allowed if and only if whitelist is empty. Otherwise,
        // all but these resources are allowed.
        public List<Resource.ResourceTags> BlacklistResources = new List<Resource.ResourceTags>();
        // If this is empty, all resources are allowed if and only if blacklist is empty. Otherwise,
        // only these resources are allowed.
        public List<Resource.ResourceTags> WhitelistResources = new List<Resource.ResourceTags>(); 

        public static uint NextID()
        {
            maxID++;
            return maxID;
        }

        public bool IsAllowed(ResourceType type)
        {
            Resource resource = ResourceLibrary.GetResourceByName(type);
            if (WhitelistResources.Count == 0)
            {
                if (BlacklistResources.Count == 0)
                {
                    return true;
                }

                return !BlacklistResources.Any(tag => resource.Tags.Any(otherTag => otherTag == tag));
            }

            if (BlacklistResources.Count != 0) return true;
            return WhitelistResources.Count == 0 || WhitelistResources.Any(tag => resource.Tags.Any(otherTag => otherTag == tag));
        }

        public void KillBox(Body component)
        {
            ZoneBodies.Remove(component);
            EaseMotion deathMotion = new EaseMotion(0.8f, component.LocalTransform, component.LocalTransform.Translation + new Vector3(0, -1, 0));
            component.AnimationQueue.Add(deathMotion);
            deathMotion.OnComplete += component.Die;
            SoundManager.PlaySound(ContentPaths.Audio.whoosh, component.LocalTransform.Translation);
            Faction.World.ParticleManager.Trigger("puff", component.LocalTransform.Translation + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 90);
        }

        public void CreateBox(Vector3 pos)
        {
            Vector3 startPos = pos + new Vector3(0.0f, -0.1f, 0.0f) + BoxOffset;
            Vector3 endPos = pos + new Vector3(0.0f, 0.9f, 0.0f) + BoxOffset;

            Body crate = EntityFactory.CreateEntity<Body>(BoxType, startPos);
            crate.AnimationQueue.Add(new EaseMotion(0.8f, crate.LocalTransform, endPos));
            Boxes.Add(crate);
            AddBody(crate, false);
            SoundManager.PlaySound(ContentPaths.Audio.whoosh, startPos);
            if (Faction != null)
                Faction.World.ParticleManager.Trigger("puff", pos + new Vector3(0.5f, 1.5f, 0.5f), Color.White, 90);
        }

        public void HandleBoxes()
        {
            if (Voxels == null || Boxes == null)
            {
                return;
            }

            if(Voxels.Count == 0)
            {
                foreach(Body component in Boxes)
                {
                    KillBox(component);
                }
                Boxes.Clear();
            }

            int numBoxes = Math.Min(Math.Max(Resources.CurrentResourceCount / ResourcesPerVoxel, 1), Voxels.Count);
            if (Resources.CurrentResourceCount == 0)
            {
                numBoxes = 0;
            }
            if (Boxes.Count > numBoxes)
            {
                for (int i = Boxes.Count - 1; i >= numBoxes; i--)
                {
                    KillBox(Boxes[i]);
                    Boxes.RemoveAt(i);
                }
            }
            else if (Boxes.Count < numBoxes)
            {
                for (int i = Boxes.Count; i < numBoxes; i++)
                {
                    CreateBox(Voxels[i].WorldPosition + VertexNoise.GetNoiseVectorFromRepeatingTexture(Voxels[i].WorldPosition));
                }
            }
        }

       

        public override bool AddItem(Body component)
        {
            bool worked =  base.AddItem(component);
            HandleBoxes();

            if (Boxes.Count > 0)
            {
                TossMotion toss = new TossMotion(1.0f, 2.5f, component.LocalTransform,
                    Boxes[Boxes.Count - 1].LocalTransform.Translation + new Vector3(0.5f, 0.5f, 0.5f));
                component.GetRoot().GetComponent<Physics>().CollideMode = Physics.CollisionMode.None;
                component.AnimationQueue.Add(toss);
                toss.OnComplete += component.Die;
            }
            else
            {
                component.Die();
            }

            return worked;
        }


        public override void Destroy()
        {
            var box = GetBoundingBox();
            box.Min += Vector3.Up;
            box.Max += Vector3.Up;
            foreach(var resource in EntityFactory.CreateResourcePiles(Resources.Resources.Values, box))
            {

            }

            if (Faction != null)
            {
                Faction.Stockpiles.Remove(this);
            }
            base.Destroy();
        }

        public override void RecalculateMaxResources()
        {

            HandleBoxes();
            base.RecalculateMaxResources();
        }        
    }
}
