using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DwarfCorp
{
    internal class BuildZoneTask : Task
    {
        public BuildZoneOrder Zone;

        public BuildZoneTask()
        {
            Category = TaskCategory.BuildZone;
            Priority = PriorityType.Medium;
            MaxAssignable = 3;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        public BuildZoneTask(BuildZoneOrder zone)
        {
            Category = TaskCategory.BuildZone;
            MaxAssignable = 3;
            Name = "Build Room " + zone.ToBuild.Type.Name + zone.ToBuild.ID;
            Zone = zone;
            Priority = PriorityType.Medium;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        private bool IsZoneBuildOrder(WorldManager World, BuildZoneOrder buildRooom)
        {
            return World.IsActiveBuildZoneOrder(buildRooom);
        }


        public override Feasibility IsFeasible(Creature agent)
        {
            return Zone != null && !Zone.IsBuilt && IsZoneBuildOrder(agent.World, Zone) &&
                agent.Stats.IsTaskAllowed(Task.TaskCategory.BuildZone) &&
                agent.World.HasResources(Zone.ListRequiredResources()) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override Act CreateScript(Creature creature)
        {
            if (Zone == null)
                return null;

            return new BuildRoomAct(creature.AI, Zone);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return (Zone == null || Zone.IsBuilt || Zone.IsDestroyed) ? 1000 : 1.0f;
        }

        public override bool ShouldDelete(Creature agent)
        {
            return Zone == null || Zone.IsBuilt || Zone.IsDestroyed || !IsZoneBuildOrder(agent.World, Zone);
        }

        public override bool ShouldRetry(Creature agent)
        {
            return Zone != null && !Zone.IsBuilt && !Zone.IsDestroyed;
        }

        public override bool IsComplete(WorldManager World)
        {
            return Zone == null || Zone.IsBuilt || !IsZoneBuildOrder(World, Zone);
        }

        public override void OnDequeued(WorldManager World)
        {
            if (!Zone.IsBuilt)
                Zone.Destroy();
        }
    }

}