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
        public ResourceAmount Resources { get; set; }
        public Faction Faction = null;
        public Stockpile Zone = null;
        public Inventory.RestockType RestockType = Inventory.RestockType.None;

        public StashResourcesAct()
        {

        }

        public StashResourcesAct(CreatureAI agent, Stockpile zone, ResourceAmount resources) :
            base(agent)
        {
            Zone = zone;
            Resources = resources;
            Name = "Stash " + Resources.Type;
        }

        public override IEnumerable<Status> Run()
        {
            Creature.IsCloaked = false;
            if (Faction == null)
            {
                Faction = Agent.Faction;
            }
            if (Zone != null)
            {
                var resourcesToStock = Creature.Inventory.Resources.Where(a => a.MarkedForRestock && Zone.IsAllowed(a.Resource)).ToList();
                foreach (var resource in resourcesToStock)
                {
                    List<Body> createdItems = Creature.Inventory.RemoveAndCreate(new ResourceAmount(resource.Resource), Inventory.RestockType.RestockResource);

                    foreach (Body b in createdItems)
                    {
                        if (Zone.AddItem(b))
                        {
                            Creature.NoiseMaker.MakeNoise("Stockpile", Creature.AI.Position);
                            Creature.Stats.NumItemsGathered++;
                            Creature.CurrentCharacterMode = Creature.AttackMode;
                            Creature.Sprite.ResetAnimations(Creature.AttackMode);
                            Creature.Sprite.PlayAnimations(Creature.AttackMode);

                            while (!Creature.Sprite.AnimPlayer.IsDone())
                            {
                                yield return Status.Running;
                            }

                            yield return Status.Running;
                        }
                        else
                        {
                            Creature.Inventory.AddResource(new ResourceAmount(resource.Resource, 1), Inventory.RestockType.RestockResource);
                            b.Delete();
                        }
                    }
                }
            }

            Timer waitTimer = new Timer(1.0f, true);
            bool removed = Faction.RemoveResources(Resources, Agent.Position, Zone, true);

            if(!removed)
            {
                yield return Status.Fail;
            }
            else
            {
                Agent.Creature.Inventory.AddResource(Resources.CloneResource(), Inventory.RestockType.None);
                Agent.Creature.Sprite.ResetAnimations(Creature.AttackMode);
                while (!waitTimer.HasTriggered)
                {
                    Agent.Creature.CurrentCharacterMode = Creature.AttackMode;
                    waitTimer.Update(DwarfTime.LastTime);
                    yield return Status.Running;
                }
                Agent.Creature.CurrentCharacterMode = CharacterMode.Idle;
                yield return Status.Success;
            }

        }

    }

}

