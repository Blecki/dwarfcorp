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
        public CraftDesignation CraftDesignation { get; set; }

        public CraftItemTask()
        {
            MaxAssignable = 3;
            Priority = PriorityType.Medium;
            AutoRetry = true;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        public CraftItemTask(CraftDesignation CraftDesignation)
        {
            Category = TaskCategory.BuildObject;
            MaxAssignable = 3;
            Name = StringLibrary.GetString("craft-at", CraftDesignation.Entity.GlobalID, CraftDesignation.ItemType.DisplayName, CraftDesignation.Location);
            Priority = PriorityType.Medium;
            AutoRetry = true;
            this.CraftDesignation = CraftDesignation;

            foreach (var tinter in CraftDesignation.Entity.EnumerateAll().OfType<Tinter>())
                tinter.Stipple = true;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            if (CraftDesignation.ItemType.IsMagical)
                Category = TaskCategory.Research;
        }

        public override void OnEnqueued(Faction Faction)
        {
            Faction.Designations.AddEntityDesignation(CraftDesignation.Entity, DesignationType.Craft, CraftDesignation, this);
        }

        public override void OnDequeued(Faction Faction)
        {
            if (!CraftDesignation.Finished)
            {
                if (CraftDesignation.WorkPile != null) CraftDesignation.WorkPile.GetRoot().Delete();
                if (CraftDesignation.HasResources)
                    foreach (var resource in CraftDesignation.SelectedResources)
                    {
                        var resourceEntity = new ResourceEntity(Faction.World.ComponentManager, resource, CraftDesignation.Entity.GlobalTransform.Translation);
                        Faction.World.ComponentManager.RootComponent.AddChild(resourceEntity);
                    }
                CraftDesignation.Entity.GetRoot().Delete();
            }

            Faction.Designations.RemoveEntityDesignation(CraftDesignation.Entity, DesignationType.Craft);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return !CraftDesignation.Location.IsValid || !CanBuild(agent) ? 1000 : (agent.AI.Position - CraftDesignation.Location.WorldPosition).LengthSquared();
        }

        public override Act CreateScript(Creature creature)
        {
            return new CraftItemAct(creature.AI, CraftDesignation);
        }

        public override bool ShouldRetry(Creature agent)
        {
            return !IsComplete(agent.Faction);
        }


        public override bool ShouldDelete(Creature agent)
        {
            return CraftDesignation.Finished;
        }

        public override bool IsComplete(Faction faction)
        {
            return CraftDesignation.Finished;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!CraftDesignation.ItemType.IsMagical && !agent.Stats.IsTaskAllowed(TaskCategory.BuildObject))
                return Feasibility.Infeasible;

            if (CraftDesignation.ItemType.IsMagical && !agent.Stats.IsTaskAllowed(TaskCategory.Research))
                return Feasibility.Infeasible;

            if (agent.AI.Status.IsAsleep)
                return Feasibility.Infeasible;

            return CanBuild(agent) && !IsComplete(agent.Faction) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public bool CanBuild(Creature agent)
        {
            if (CraftDesignation.ExistingResource != null) // This is a placement of an existing item.
                return true;

            if (!String.IsNullOrEmpty(CraftDesignation.ItemType.CraftLocation))
            {
                var nearestBuildLocation = agent.Faction.FindNearestItemWithTags(CraftDesignation.ItemType.CraftLocation, Vector3.Zero, false, agent.AI);

                if (nearestBuildLocation == null)
                    return false;
            }
            
            foreach (var resourceAmount in CraftDesignation.ItemType.RequiredResources)
            {
                var resources = agent.Faction.ListResourcesWithTag(resourceAmount.ResourceType, CraftDesignation.ItemType.AllowHeterogenous);
                if (resources.Count == 0 || !resources.Any(r => r.NumResources >= resourceAmount.NumResources))
                {
                    return false;
                }
            }

            return true;
        }

    }
}