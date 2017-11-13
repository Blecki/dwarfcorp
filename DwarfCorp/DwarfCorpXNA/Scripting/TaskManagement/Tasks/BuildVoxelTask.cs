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
            Priority = PriorityType.Low;
        }

        public BuildVoxelTask(VoxelHandle voxel, string type)
        {
            Name = "Put voxel of type: " + type + " on voxel " + voxel.Coordinate;
            Voxel = voxel;
            VoxType = type;
            Priority = PriorityType.Low;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.Stats.CurrentClass.Actions.Contains(GameMaster.ToolMode.BuildWall))
                return Feasibility.Infeasible;

            return Voxel.IsValid && agent.Faction.Designations.IsVoxelDesignation(Voxel, DesignationType.Put) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override bool ShouldDelete(Creature agent)
        {
            return !Voxel.IsValid || !agent.Faction.Designations.IsVoxelDesignation(Voxel, DesignationType.Put);
        }

        public override bool ShouldRetry(Creature agent)
        {
            return Voxel.IsValid && agent.Faction.Designations.IsVoxelDesignation(Voxel, DesignationType.Put);
        }

        public override Task Clone()
        {
            return new BuildVoxelTask(Voxel, VoxType);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return !Voxel.IsValid ? 1000 : 0.01f * (agent.AI.Position - Voxel.WorldPosition).LengthSquared() + (Voxel.Coordinate.Y);
        }

        public IEnumerable<Act.Status> AddBuildOrder(Creature creature)
        {
            creature.AI.GatherManager.AddVoxelOrder(new GatherManager.BuildVoxelOrder() { Type = VoxType, Voxel = Voxel });
            yield return Act.Status.Success;
        }

        public override Act CreateScript(Creature creature)
        {
            return new Wrap(() => AddBuildOrder(creature));
        }

        public override void Render(DwarfTime time)
        {
            base.Render(time);
        }
    }

    [Newtonsoft.Json.JsonObject(IsReference = true)]
    class BuildVoxelsTask : Task
    {
        public List<KeyValuePair<VoxelHandle, string>> Voxels { get; set; }

        public BuildVoxelsTask()
        {
            
        }

        public BuildVoxelsTask(List<KeyValuePair<VoxelHandle, string>> voxels)
        {
            Name = "Build " + voxels.Count + " blocks";
            Voxels = voxels;
        }

        public override Task Clone()
        {
           return new BuildVoxelsTask(Voxels);
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            Dictionary<ResourceLibrary.ResourceType, int> numResources = new Dictionary<ResourceLibrary.ResourceType, int>();
            int numFeasibleVoxels = 0;
            var factionResources = agent.Faction.ListResources();
            foreach (var pair in Voxels)
            {
                if (!agent.Faction.Designations.IsVoxelDesignation(pair.Key, DesignationType.Put))
                {
                    continue;
                }
                var voxtype = VoxelLibrary.GetVoxelType(pair.Value);
                if (!numResources.ContainsKey(voxtype.ResourceToRelease))
                {
                    numResources.Add(voxtype.ResourceToRelease, 0);
                }
                int num = numResources[voxtype.ResourceToRelease] + 1;
                if (!factionResources.ContainsKey(voxtype.ResourceToRelease))
                {
                    continue;
                }
                var numInStocks = factionResources[voxtype.ResourceToRelease];
                if (numInStocks.NumResources < num) continue;
                numResources[voxtype.ResourceToRelease]++;
                numFeasibleVoxels++;
            }
            return numFeasibleVoxels > 0 ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return Voxels.Count*10;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return Voxels.Count > 0;
        }

        private IEnumerable<Act.Status> Reloop(Creature agent)
        {
            List<KeyValuePair<VoxelHandle, string>> feasibleVoxels = Voxels.Where(voxel => agent.Faction.Designations.IsVoxelDesignation(voxel.Key, DesignationType.Put)).ToList();

            if (feasibleVoxels.Count > 0)
            {
                agent.AI.AssignTask(new BuildVoxelsTask(feasibleVoxels));
            }
            yield return Act.Status.Success;
        }

        private IEnumerable<Act.Status> Fail()
        {
            yield return Act.Status.Fail;
        }

        private IEnumerable<Act.Status> Succeed()
        {
            yield return Act.Status.Success;
        }

        public bool Validate(CreatureAI creature, VoxelHandle voxel, ResourceAmount resources)
        {
            return creature.Faction.Designations.IsVoxelDesignation(voxel, DesignationType.Put) && 
                creature.Creature.Inventory.HasResource(resources);
        }

        public override Act CreateScript(Creature agent)
        {
             List<KeyValuePair<VoxelHandle, string>> feasibleVoxels = new List<KeyValuePair<VoxelHandle, string>>();
            Dictionary<ResourceLibrary.ResourceType, int> numResources = new Dictionary<ResourceLibrary.ResourceType, int>();

            List<ResourceAmount> resources = new List<ResourceAmount>();
            var factionResources = agent.Faction.ListResources();
            foreach (var pair in Voxels)
            {
                if (!agent.Faction.Designations.IsVoxelDesignation(pair.Key, DesignationType.Put))
                {
                    continue;
                }
                var voxType = VoxelLibrary.GetVoxelType(pair.Value);
                if (!numResources.ContainsKey(voxType.ResourceToRelease))
                {
                    numResources.Add(voxType.ResourceToRelease, 0);
                }
                int num = numResources[voxType.ResourceToRelease] + 1;
                if (!factionResources.ContainsKey(voxType.ResourceToRelease))
                {
                    continue;
                }
                var numInStocks = factionResources[voxType.ResourceToRelease];
                if (numInStocks.NumResources < num) continue;
                numResources[voxType.ResourceToRelease]++;
                feasibleVoxels.Add(pair);
                resources.Add(new ResourceAmount(voxType.ResourceToRelease));
            }

            List<Act> children = new List<Act>()
            {
                new GetResourcesAct(agent.AI, resources)
            };

            int i = 0;
            foreach (var pair in feasibleVoxels)
            {
                int local = i;
                var localVox = pair.Key;
                children.Add(new Select(new Sequence(new Domain(() => Validate(agent.AI, pair.Key, resources[local]), 
                             new GoToVoxelAct(pair.Key, PlanAct.PlanType.Radius, agent.AI, 4.0f)),
                             new PlaceVoxelAct(pair.Key, agent.AI, resources[i])),
                             new Wrap(Succeed)));
                i++;
            }

            children.Add(new Wrap(Fail));
            children.Add(new Wrap(agent.RestockAll));
            children.Add(new Wrap(() => Reloop(agent)));

            return new Select(new Sequence(children), new Sequence(new Wrap(()=> Reloop(agent)), new Wrap(agent.RestockAll)))
            {
                Name = "Build Blocks"
            };
        }
    }

}