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
        public CraftDesignation Designation { get; set; }

        public CraftItemTask()
        {
            MaxAssignable = 3;
            Priority = PriorityType.Low;
            AutoRetry = true;
        }

        public CraftItemTask(CraftDesignation type)
        {
            Category = TaskCategory.BuildObject;
            MaxAssignable = 3;
            Name = string.Format("Craft {0} at {1}", type.ItemType.Name, type.Location);
            Priority = PriorityType.Low;
            AutoRetry = true;
            Designation = type;
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
            return agent.Faction.Designations.IsDesignation(Designation.Entity, DesignationType.Craft) && !IsComplete(agent.Faction);
        }


        public override bool ShouldDelete(Creature agent)
        {
            return !agent.Faction.Designations.IsDesignation(Designation.Entity, DesignationType.Craft) || Designation.Progress > 1.0f;
        }

        public override bool IsComplete(Faction faction)
        {
            return Designation.Entity.Active;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.Stats.IsTaskAllowed(TaskCategory.BuildObject))
            {
                return Feasibility.Infeasible;
            }

            if (agent.AI.Status.IsAsleep)
                return Feasibility.Infeasible;

            return CanBuild(agent) && !IsComplete(agent.Faction) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public bool CanBuild(Creature agent)
        {            
            if (!String.IsNullOrEmpty(Designation.ItemType.CraftLocation))
            {
                var nearestBuildLocation = agent.Faction.FindNearestItemWithTags(Designation.ItemType.CraftLocation, Vector3.Zero, false);

                if (nearestBuildLocation == null)
                    return false;
            }
            else if (!agent.Faction.Designations.IsDesignation(Designation.Entity, DesignationType.Craft))
            {
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
}