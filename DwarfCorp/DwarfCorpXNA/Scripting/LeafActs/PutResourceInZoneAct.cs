// PutResourceInZoneAct.cs
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
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A creature puts a specified resource (in its inventory) into a zone.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PutResourceInZone : CreatureAct
    {
        [JsonIgnore]
        public Zone Zone { get { return GetZone(); } set { SetZone(value); } }

        public Zone GetZone()
        {
            return Agent.Blackboard.GetData<Stockpile>(StockpileName);
        }

        public void SetZone(Zone pile)
        {
            Agent.Blackboard.SetData(StockpileName, pile);
        }


        [JsonIgnore]
        public VoxelHandle Voxel { get { return GetVoxel(); } set { SetVoxel(value); } }

        public VoxelHandle GetVoxel()
        {
            return Agent.Blackboard.GetData<VoxelHandle>(VoxelName);
        }

        public void SetVoxel(VoxelHandle voxel)
        {
            Agent.Blackboard.SetData(VoxelName, voxel);
        }

        public string StockpileName { get; set; }
        public string VoxelName { get; set; }

        [JsonIgnore]
        public ResourceAmount Resource { get { return GetResource(); } set { SetResource(value); } }

        public ResourceAmount GetResource()
        {
            return Agent.Blackboard.GetData<ResourceAmount>(ResourceName);
        }

        public void SetResource(ResourceAmount amount)
        {
            Agent.Blackboard.SetData(ResourceName, amount);
        }

        public string ResourceName { get; set; }

        public PutResourceInZone(CreatureAI agent, string stockpileName, string voxelname, string resourceName) :
            base(agent)
        {
            VoxelName = voxelname;
            Name = "Put Item";
            StockpileName = stockpileName;
            ResourceName = resourceName;
        }

        public override IEnumerable<Status> Run()
        {
            if (Zone == null)
            {
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                yield return Status.Fail;
                yield break;
            }

            if (Resource.NumResources <= 0)
            {
                yield return Status.Success;
                yield break;
            }

            List<Body> createdItems = Creature.Inventory.RemoveAndCreate(Resource);
            if(createdItems.Count == 0)
            {
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                yield return Status.Fail;
            }

            foreach (Body b in createdItems)
            {
                if (Zone.AddItem(b))
                {
                    Creature.NoiseMaker.MakeNoise("Stockpile", Creature.AI.Position);
                    Creature.Stats.NumItemsGathered++;
                    Creature.AI.AddXP(1);
                    Creature.CurrentCharacterMode = CharacterMode.Attacking;
                    Creature.Sprite.ResetAnimations(CharacterMode.Attacking);
                    Creature.Sprite.PlayAnimations(CharacterMode.Attacking);

                    while (!Creature.Sprite.CurrentAnimation.IsDone())
                    {
                        yield return Status.Running;
                    }

                    yield return Status.Running;
                }
                else
                {
                    Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    Creature.CurrentCharacterMode = CharacterMode.Idle;
                    yield return Status.Fail;
                }   
            }
            Creature.CurrentCharacterMode = CharacterMode.Idle;
            yield return Status.Success;
        }
    }

}