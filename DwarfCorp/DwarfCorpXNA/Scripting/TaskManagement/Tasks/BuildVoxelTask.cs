// BuildVoxelTask.cs
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
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Text;
using DwarfCorp.GameStates;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should get a resource, and put it into a voxel
    /// to build it.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class BuildVoxelTask : Task
    {
        public string VoxType { get; set; }
        public VoxelHandle Voxel { get; set; }

        public BuildVoxelTask()
        {
            Category = TaskCategory.BuildBlock;
            Priority = PriorityType.Medium;
            BoredomIncrease = 0.1f;
        }

        public BuildVoxelTask(VoxelHandle voxel, string type)
        {
            Category = TaskCategory.BuildBlock;
            Name = "Put voxel of type: " + type + " on voxel " + voxel.Coordinate;
            Voxel = voxel;
            VoxType = type;
            Priority = PriorityType.Medium;
            BoredomIncrease = 0.1f;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.AI.Stats.CurrentClass.IsTaskAllowed(TaskCategory.BuildBlock))
                return Feasibility.Infeasible;

            if (agent.AI.Status.IsAsleep)
                return Feasibility.Infeasible;

            if (!agent.AI.Faction.Designations.IsVoxelDesignation(Voxel, DesignationType.Put))
            {
                return Feasibility.Infeasible;
            }

            Dictionary<ResourceType, int> numResources = new Dictionary<ResourceType, int>();
            int numFeasibleVoxels = 0;

            var voxtype = VoxelLibrary.GetVoxelType(VoxType);
            int count = agent.Faction.ListResources().Where(r => voxtype.CanBuildWith(ResourceLibrary.GetResourceByName(r.Key))).Sum(r => r.Value.NumResources);
            if (count == 0)
            {
                return Feasibility.Infeasible;
            }
            numFeasibleVoxels++;
            return numFeasibleVoxels > 0 ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override bool ShouldDelete(Creature agent)
        {
            return !Voxel.IsValid || !agent.AI.Faction.Designations.IsVoxelDesignation(Voxel, DesignationType.Put);
        }

        public override bool ShouldRetry(Creature agent)
        {
            return Voxel.IsValid && agent.AI.Faction.Designations.IsVoxelDesignation(Voxel, DesignationType.Put);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return !Voxel.IsValid ? 1000 : 0.01f * (agent.AI.Position - Voxel.WorldPosition).LengthSquared() + (Voxel.Coordinate.Y);
        }

        public bool Validate(CreatureAI creature, VoxelHandle voxel, ResourceAmount resources)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
            {
                if (creature.Faction == creature.World.PlayerFaction)
                {
                    creature.World.MakeAnnouncement(String.Format("{0} cancelled build task because it is unreachable", creature.Stats.FullName));
                    creature.World.Master.TaskManager.CancelTask(this);
                }
                return false;
            }

            return creature.Creature.Inventory.HasResource(resources);
        }

        public override Act CreateScript(Creature creature)
        {
            var voxtype = VoxelLibrary.GetVoxelType(VoxType);
            var resource = creature.Faction.ListResources().Where(r => voxtype.CanBuildWith(ResourceLibrary.GetResourceByName(r.Key))).FirstOrDefault();
            
            if (resource.Key == null)
            {
                return null;
            }

            var resources = new ResourceAmount(resource.Value.ResourceType, 1);

            return new Select(
                new Sequence(
                    new GetResourcesAct(creature.AI, new List<ResourceAmount>() { resources }),
                    new Domain(() => Validate(creature.AI, Voxel, resources),
                        new GoToVoxelAct(Voxel, PlanAct.PlanType.Radius, creature.AI, 4.0f)),
                    new PlaceVoxelAct(Voxel, creature.AI, resources, VoxType)), 
                new Wrap(creature.RestockAll))
            { Name = "Build Voxel" };
        }

        public override void Render(DwarfTime time)
        {
            base.Render(time);
        }

        public override bool IsComplete(Faction faction)
        {
            return Voxel.IsValid && Voxel.Type.Name == VoxType;
        }

        public override void OnEnqueued(Faction Faction)
        {
            Faction.Designations.AddVoxelDesignation(Voxel, DesignationType.Put, VoxType, this);
        }

        public override void OnDequeued(Faction Faction)
        {
            Faction.Designations.RemoveVoxelDesignation(Voxel, DesignationType.Put);
        }
    }
}