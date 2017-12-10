// StashResourcesAct.cs
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
    public class StashResourcesAct : CreatureAct
    {
        public List<ResourceAmount> Resources { get; set; }

        public StashResourcesAct()
        {

        }

        public StashResourcesAct(CreatureAI agent, List<ResourceAmount> resources) :
            base(agent)
        {
            Resources = resources;
            Name = "Stash " + Resources.ToString();
        }

        public override IEnumerable<Status> Run()
        {
            Zone zone = Agent.Faction.GetNearestStockpile(Agent.Position, (s) => !s.IsFull() && Resources.All(resource => s.IsAllowed(resource.ResourceType)));

            if (zone != null)
            {
                var resourcesToStock = Creature.Inventory.Resources.Where(a => a.MarkedForRestock).ToList();
                foreach (var resource in resourcesToStock)
                {
                    List<Body> createdItems = Creature.Inventory.RemoveAndCreate(new ResourceAmount(resource.Resource), Inventory.RestockType.RestockResource);

                    foreach (Body b in createdItems)
                    {
                        if (zone.AddItem(b))
                        {
                            Creature.NoiseMaker.MakeNoise("Stockpile", Creature.AI.Position);
                            Creature.Stats.NumItemsGathered++;
                            Creature.CurrentCharacterMode = CharacterMode.Attacking;
                            Creature.Sprite.ResetAnimations(CharacterMode.Attacking);
                            Creature.Sprite.PlayAnimations(CharacterMode.Attacking);

                            while (!Creature.Sprite.CurrentAnimation.IsDone())
                            {
                                yield return Status.Running;
                            }

                            yield return Status.Running;
                        }
                    }
                }
            }

            Timer waitTimer = new Timer(1.0f, true);
            bool removed = Agent.Faction.RemoveResources(Resources, Agent.Position);

            if(!removed)
            {
                yield return Status.Fail;
            }
            else
            {
                foreach(ResourceAmount resource in Resources)
                {
                    Agent.Creature.Inventory.AddResource(resource.CloneResource(), Inventory.RestockType.None);   
                }

                while (!waitTimer.HasTriggered)
                {
                    waitTimer.Update(DwarfTime.LastTime);
                    yield return Status.Running;
                }
                yield return Status.Success;
            }

        }

    }

}

