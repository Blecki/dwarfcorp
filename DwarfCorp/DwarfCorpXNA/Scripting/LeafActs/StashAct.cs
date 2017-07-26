// StashAct.cs
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
    /// A creature grabs a given item and puts it in their inventory
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class StashAct : CreatureAct
    {
        public enum PickUpType
        {
            None,
            Stockpile,
            Room
        }

        public Zone Zone { get; set; }

        public PickUpType PickType { get; set; }

        public string TargetName { get; set; }

        public string StashedItemOut { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public Body Target { get { return GetTarget(); } set { SetTarget(value); } }

        public StashAct()
        {

        }

        public StashAct(CreatureAI agent, PickUpType type, Zone zone, string targetName, string stashedItemOut) :
            base(agent)
        {
            Name = "Stash " + targetName;
            PickType = type;
            Zone = zone;
            TargetName = targetName;
            StashedItemOut = stashedItemOut;
        }

        public Body GetTarget()
        {
            return Agent.Blackboard.GetData<Body>(TargetName);
        }

        public void SetTarget(Body targt)
        {
            Agent.Blackboard.SetData(TargetName, targt);
        }


        public override IEnumerable<Status> Run()
        {
            if(Target == null)
            {
                yield return Status.Fail;
            }

            switch (PickType)
            {
                case (PickUpType.Room):
                case (PickUpType.Stockpile):
                    {
                        if (Zone == null)
                        {
                            yield return Status.Fail;
                            break;
                        }
                        bool removed = Zone.Resources.RemoveResource(new ResourceAmount(Target.Tags[0]));

                        if (removed)
                        {
                            if(Creature.Inventory.Pickup(Target, Inventory.RestockType.RestockResource))
                            {
                                Agent.Blackboard.SetData(StashedItemOut, new ResourceAmount(Target));
                                Agent.Creature.NoiseMaker.MakeNoise("Stash", Agent.Position);
                                yield return Status.Success;
                            }
                            else
                            {
                                yield return Status.Fail;
                            }
                        }
                        else
                        {
                            yield return Status.Fail;
                        }
                        break;
                    }
                case (PickUpType.None):
                    {
                        if (Target is CoinPile)
                        {
                            DwarfBux money = (Target as CoinPile).Money;
                            Creature.AI.AddMoney(money);
                            Target.Die();
                        }
                        else if (!Creature.Inventory.Pickup(Target, Inventory.RestockType.RestockResource))
                        {
                            yield return Status.Fail;
                        }

                        if (Creature.Faction.GatherDesignations.Contains(Target))
                        {
                            Creature.Faction.GatherDesignations.Remove(Target);
                        }
                        else
                        {
                            yield return Status.Fail;
                            break;
                        }

                        ResourceAmount resource = new ResourceAmount(Target);
                        Agent.Blackboard.SetData(StashedItemOut, resource);
                        //Creature.DrawIndicator(resource.ResourceType.Image, resource.ResourceType.Tint);
                        Agent.Creature.NoiseMaker.MakeNoise("Stash", Agent.Position);
                        yield return Status.Success;
                        break;
                    }
            }
        }
        
    }
    
}

