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
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class CraftItemTask : Task
    {
        public CraftBuilder.CraftDesignation Designation { get; set; }
        public CraftItemTask()
        {
            MaxAssignable = 3;
            Priority = PriorityType.Low;
            AutoRetry = true;
        }

        public CraftItemTask(CraftBuilder.CraftDesignation type)
        {
            MaxAssignable = 3;
            Name = string.Format("Craft {0} at {1}", type.ItemType.Name, type.Location);
            Priority = PriorityType.Low;
            AutoRetry = true;
            Designation = type;
        }

        public override Task Clone()
        {
            return new CraftItemTask(Designation);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return !Designation.Location.IsValid || !CanBuild(agent) ? 1000 : (agent.AI.Position - Designation.Location.WorldPosition).LengthSquared();
        }

        public override Act CreateScript(Creature creature)
        {
            return new CraftItemAct(creature.AI, Designation);
        }

        public override bool ShouldRetry(Creature agent)
        {
            if (!agent.Faction.CraftBuilder.IsDesignation(Designation.Location))
            {
                return false;
            }

            return true;
        }


        public override bool ShouldDelete(Creature agent)
        {
            return !agent.Faction.CraftBuilder.IsDesignation(Designation.Location) || Designation.Progress > 1.0f;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.Stats.CurrentClass.Actions.Contains(GameMaster.ToolMode.Craft))
            {
                return Feasibility.Infeasible;
            }

            return CanBuild(agent) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public bool CanBuild(Creature agent)
        {
            if (!agent.Faction.CraftBuilder.IsDesignation(Designation.Location))
            {
                return false;
            }
            if (!String.IsNullOrEmpty(Designation.ItemType.CraftLocation))
            {
                var nearestBuildLocation = agent.Faction.FindNearestItemWithTags(Designation.ItemType.CraftLocation, Vector3.Zero, false);

                if (nearestBuildLocation == null)
                    return false;
            }

            foreach (var resourceAmount in Designation.ItemType.RequiredResources)
            {
                var resources = agent.Faction.ListResourcesWithTag(resourceAmount.ResourceType, Designation.ItemType.AllowHeterogenous);
                if (resources.Count == 0 || !resources.Any(r => r.NumResources >= resourceAmount.NumResources))
                {
                    return false;
                }
            }

            return true;
        }

    }


    class CraftResourceTask : Task
    {
        public int TaskID = 0;
        private static int MaxID = 0;
        public CraftBuilder.CraftDesignation Item { get; set; }
        private string noise;
        public bool IsAutonomous { get; set; }

        public CraftResourceTask()
        {
            
        }

        public CraftResourceTask(CraftItem selectedResource, int id = -1)
        {
            TaskID = id < 0 ? MaxID : id;
            MaxID++;
            Item = new CraftBuilder.CraftDesignation()
            {
                ItemType = selectedResource.Clone(),
                Location = VoxelHandle.InvalidHandle,
                Valid = true
            };
            Name = String.Format("Craft order {0}", TaskID);
            Priority = PriorityType.Low;

            noise = ResourceLibrary.GetResourceByName(Item.ItemType.ResourceCreated).Tags.Contains(Resource.ResourceTags.Edible)
                ? "Cook"
                : "Craft";
            AutoRetry = true;
        }

        public IEnumerable<Act.Status> Repeat(Creature creature)
        {
            CraftItem newItem = Item.ItemType.Clone();
            newItem.NumRepeats--;
            if (newItem.NumRepeats >= 1)
            {
                if (creature.AI.Faction == creature.World.PlayerFaction)
                {
                    creature.World.Master.TaskManager.AddTask(new CraftResourceTask(newItem, TaskID));
                }
                else
                {
                    creature.AI.AssignTask(new CraftResourceTask(newItem, TaskID));
                }
            }
            yield return Act.Status.Success;
        }

        public override bool ShouldDelete(Creature agent)
        {
            if (Item.Progress > 1.0f)
            {
                return true;
            }
            return false;
        }

        private bool HasResources(Creature agent)
        {
            if (Item.HasResources)
            {
                return true;
            }

            var resources = agent.Faction.HasResources(Item.ItemType.SelectedResources);
            if (!resources)
            {
                return false;
            }
            return true;
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
            if (!agent.Stats.CurrentClass.Actions.Contains(GameMaster.ToolMode.Craft))
                return Feasibility.Infeasible;
            return HasResources(agent) && HasLocation(agent) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override Act CreateScript(Creature creature)
        {
            return new Sequence(new CraftItemAct(creature.AI, Item)
            {
                Noise = noise
            }, new Wrap(() => Repeat(creature)));
        }

        public override Task Clone()
        {
            return new CraftResourceTask(Item.ItemType, TaskID) {IsAutonomous = this.IsAutonomous};
        }
    }

}