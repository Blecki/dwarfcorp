// CraftItemTask.cs
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
    class CraftResourceTask : Task
    {
        public int TaskID = 0;
        private static int MaxID = 0;
        public CraftDesignation Item { get; set; }
        private string noise;
        public bool IsAutonomous { get; set; }
        public int NumRepeats;

        public CraftResourceTask()
        {
            Category = TaskCategory.CraftItem;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        public CraftResourceTask(CraftItem selectedResource, int NumRepeats, List<ResourceAmount> SelectedResources, int id = -1)
        {
            this.NumRepeats = NumRepeats;

            TaskID = id < 0 ? MaxID : id;
            MaxID++;
            Item = new CraftDesignation()
            {
                ItemType = selectedResource,
                Location = VoxelHandle.InvalidHandle,
                Valid = true,
                SelectedResources = SelectedResources
            };
            Name = String.Format("Craft order {0}", TaskID);
            Priority = PriorityType.Low;

            if (ResourceLibrary.GetResourceByName(Item.ItemType.ResourceCreated).Tags.Contains(Resource.ResourceTags.Edible))
            {
                noise = "Cook";
                Category = TaskCategory.Cook;
            }
            else
            {
                noise = "Craft";
                Category = selectedResource.IsMagical ? TaskCategory.Research : TaskCategory.CraftItem;
            }

            AutoRetry = true;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        public IEnumerable<Act.Status> Repeat(Creature creature)
        {
            NumRepeats--;
            if (NumRepeats >= 1)
            {
                Item.Progress = 0;
                Item.HasResources = false;
                if (creature.AI.Faction == creature.World.PlayerFaction)
                {
                    creature.World.Master.TaskManager.AddTask(new CraftResourceTask(Item.ItemType, NumRepeats, Item.SelectedResources, TaskID));
                }
                else
                {
                    creature.AI.AssignTask(new CraftResourceTask(Item.ItemType, NumRepeats, Item.SelectedResources, TaskID));
                }
            }
            yield return Act.Status.Success;
        }

        public override bool ShouldDelete(Creature agent)
        {
            if (Item.Progress > 0.999f && NumRepeats <= 0)
            {
                return true;
            }
            return false;
        }

        public override bool IsComplete(Faction faction)
        {
            return Item.Progress > 0.999f && NumRepeats <= 0;
        }

        private bool HasResources(Creature agent)
        {
            if (Item.HasResources)
                return true;

            if (Item.SelectedResources.Count != 0)
                return agent.Faction.HasResources(Item.SelectedResources);
            return agent.Faction.HasResources(Item.ItemType.RequiredResources);
        }

        private bool HasLocation(Creature agent)
        {
            if (Item.ItemType.CraftLocation != "")
            {
                var anyCraftLocation = agent.Faction.OwnedObjects.Any(o => o.Tags.Contains(Item.ItemType.CraftLocation) && (!o.IsReserved || o.ReservedFor == agent.AI));
                if (!anyCraftLocation)
                    return false;
            }
            return true;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.Stats.IsTaskAllowed(Category))
                return Feasibility.Infeasible;

            return HasResources(agent) && HasLocation(agent) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public IEnumerable<Act.Status> Cleanup(CreatureAI creature)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
            {
                if (creature.Faction == creature.World.PlayerFaction)
                {
                    creature.World.MakeAnnouncement(
                        StringLibrary.GetString("cancelled-crafting-unreachable", creature.Stats.FullName, Item.ItemType.DisplayName));
                    creature.World.Master.TaskManager.CancelTask(this);
                }
            }
            yield return Act.Status.Success;
        }

        public override Act CreateScript(Creature creature)
        {
            return new Sequence(new CraftItemAct(creature.AI, Item)
            {
                Noise = noise
            }, new Wrap(() => Repeat(creature))) | new Wrap(() => Cleanup(creature.AI));
        }
    }
}