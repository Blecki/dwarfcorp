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
        }

        public BuildVoxelTask(VoxelHandle voxel, string type)
        {
            Category = TaskCategory.BuildBlock;
            Name = "Put voxel of type: " + type + " on voxel " + voxel.Coordinate;
            Voxel = voxel;
            VoxType = type;
            Priority = PriorityType.Medium;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.AI.Stats.CurrentClass.IsTaskAllowed(TaskCategory.BuildBlock))
                return Feasibility.Infeasible;

            if (agent.AI.Status.IsAsleep)
                return Feasibility.Infeasible;

            Dictionary<ResourceLibrary.ResourceType, int> numResources = new Dictionary<ResourceLibrary.ResourceType, int>();
            int numFeasibleVoxels = 0;
            var factionResources = agent.Faction.ListResources();
            if (!agent.Faction.Designations.IsVoxelDesignation(Voxel, DesignationType.Put))
            {
                return Feasibility.Infeasible;
            }
            var voxtype = VoxelLibrary.GetVoxelType(VoxType);
            if (!numResources.ContainsKey(voxtype.ResourceToRelease))
            {
                numResources.Add(voxtype.ResourceToRelease, 0);
            }
            int num = numResources[voxtype.ResourceToRelease] + 1;
            if (!factionResources.ContainsKey(voxtype.ResourceToRelease))
            {
                return Feasibility.Infeasible;
            }
            var numInStocks = factionResources[voxtype.ResourceToRelease];
            if (numInStocks.NumResources < num)
            {
                return Feasibility.Infeasible;
            }
            numResources[voxtype.ResourceToRelease]++;
            numFeasibleVoxels++;
            return numFeasibleVoxels > 0 ? Feasibility.Feasible : Feasibility.Infeasible;
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
            return new BuildVoxelTask(Voxel, VoxType) { Priority = this.Priority };
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return !Voxel.IsValid ? 1000 : 0.01f * (agent.AI.Position - Voxel.WorldPosition).LengthSquared() + (Voxel.Coordinate.Y);
        }

        public bool Validate(CreatureAI creature, VoxelHandle voxel, ResourceAmount resources)
        {
            bool success =  creature.Faction.Designations.IsVoxelDesignation(voxel, DesignationType.Put) &&
                creature.Creature.Inventory.HasResource(resources);
            return success;
        }

        public override Act CreateScript(Creature creature)
        {
            var voxType = VoxelLibrary.GetVoxelType(VoxType);
            var resources = new ResourceAmount(voxType.ResourceToRelease, 1);
            return new Select(new Sequence(new Domain(() => creature.Faction.Designations.IsVoxelDesignation(Voxel, DesignationType.Put), 
                                                      new GetResourcesAct(creature.AI, new List<ResourceAmount>() { resources })), 
                new Domain(() => Validate(creature.AI, Voxel, resources),
                             new GoToVoxelAct(Voxel, PlanAct.PlanType.Radius, creature.AI, 4.0f)),
                             new PlaceVoxelAct(Voxel, creature.AI, resources)), new Wrap(creature.RestockAll)
                            )
            { Name = "Build Voxel" };
        }

        public override void Render(DwarfTime time)
        {
            base.Render(time);
        }
    }

    /*
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    class BuildVoxelsTask : Task
    {
        public List<KeyValuePair<VoxelHandle, string>> Voxels { get; set; }

        public BuildVoxelsTask()
        {
            Category = TaskCategory.BuildBlock;
        }

        public BuildVoxelsTask(List<KeyValuePair<VoxelHandle, string>> voxels)
        {
            StringBuilder nameBuilder = new StringBuilder();
            nameBuilder.Append("Place blocks at ");
            foreach(var voxel in voxels)
            {
                nameBuilder.Append(voxel.Key.Coordinate.ToString());
            }
            Name = nameBuilder.ToString();
            Voxels = voxels;
            Priority = PriorityType.Medium;
            Category = TaskCategory.BuildBlock;
        }

        public override Task Clone()
        {
            return new BuildVoxelsTask(Voxels) { Priority = this.Priority };
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.AI.Stats.CurrentClass.IsTaskAllowed(TaskCategory.BuildBlock))
                return Feasibility.Infeasible;

            if (agent.AI.Status.IsAsleep)
                return Feasibility.Infeasible;

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
                //agent.AI.AssignTask(new BuildVoxelsTask(feasibleVoxels) { Priority = PriorityType.Medium });
                // This is an obscene hack to allow dwarfs to share build voxel tasks.
                for (int i = 0; i <= feasibleVoxels.Count / 4; i++)
                {
                    int k = i;
                    int k4 = Math.Min(i + 4, feasibleVoxels.Count - 1);
                    if (k >= feasibleVoxels.Count)
                        break;
                    agent.World.Master.TaskManager.AddTask(new BuildVoxelsTask(feasibleVoxels.GetRange(k, k4)) { Priority = PriorityType.Medium });
                }
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
                children.Add(new Select(new Sequence(new Domain(() => Validate(agent.AI, localVox, resources[local]), 
                             new GoToVoxelAct(localVox, PlanAct.PlanType.Radius, agent.AI, 4.0f)),
                             new PlaceVoxelAct(localVox, agent.AI, resources[local])),
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
    */
}