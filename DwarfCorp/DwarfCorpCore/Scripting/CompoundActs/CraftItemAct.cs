// CraftItemAct.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature goes to a voxel location, and places an object with the desired tags there to build it.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class CraftItemAct : CompoundCreatureAct
    {
        public CraftLibrary.CraftItemType ItemType { get; set; }
        public Voxel Voxel { get; set; }
        public CraftItemAct()
        {

        }

        public IEnumerable<Status> DestroyResources()
        {
            Creature.Inventory.Remove(CraftLibrary.CraftItems[ItemType].RequiredResources);
            yield return Status.Success;
        }


        public IEnumerable<Status> WaitAndHit(float time)
        {
            Body objectToHit = Creature.AI.Blackboard.GetData<Body>("Anvil");
            Timer timer = new Timer(time, true);
            while (!timer.HasTriggered)
            {
                timer.Update(DwarfTime.LastTime);

                Creature.CurrentCharacterMode = Creature.CharacterMode.Attacking;
                Creature.Physics.Velocity *= 0.1f;
                Creature.Attacks[0].PerformNoDamage(DwarfTime.LastTime, Creature.AI.Position);
                Drawer2D.DrawLoadBar(Agent.Position + Vector3.Up, Color.White, Color.Black, 100, 16, timer.CurrentTimeSeconds / time);

                yield return Status.Running;
            }

            Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
            Creature.AI.AddThought(Thought.ThoughtType.Crafted);
            Creature.AI.AddXP((int)(time * 5));

            if (objectToHit != null)
            {
                objectToHit.IsReserved = false;
                objectToHit.ReservedFor = null;
            }
            yield return Status.Success;
        }


        public CraftItemAct(CreatureAI creature, Voxel voxel, CraftLibrary.CraftItemType type) :
            base(creature)
        {
            ItemType = type;
            Voxel = voxel;
            Name = "Build craft item";
        }

        public override void Initialize()
        {
            Act unreserveAct = new Wrap(() => Creature.Unreserve("Anvil"));
            float time = CraftLibrary.CraftItems[ItemType].BaseCraftTime / Creature.AI.Stats.BuffedInt;
            Tree = new Sequence(
                new Wrap(() => Creature.FindAndReserve("Anvil", "Anvil")),
                new GetResourcesAct(Agent, CraftLibrary.CraftItems[ItemType].RequiredResources),
                new Sequence
                    (
                        new GoToTaggedObjectAct(Agent) { Tag = "Anvil", Teleport = false, TeleportOffset = new Vector3(1, 0, 0), ObjectName = "Anvil"},
                        new Wrap(() => WaitAndHit(time)),
                        new Wrap(DestroyResources),
                        unreserveAct,
                        new GoToVoxelAct(Voxel, PlanAct.PlanType.Adjacent, Agent),
                        new CreateCraftItemAct(Voxel, Creature.AI, ItemType)
                    ) | new Sequence(unreserveAct, new Wrap(Creature.RestockAll), false)
                    ) | new Sequence(unreserveAct, false);
            base.Initialize();
        }


        public override void OnCanceled()
        {
            foreach (var statuses in Creature.Unreserve("Anvil"))
            {
                continue;
            }
            base.OnCanceled();
        }

       
    }
}