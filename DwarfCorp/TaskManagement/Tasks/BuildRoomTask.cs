using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DwarfCorp
{

    public class GoToZoneTask : Task
    {
        public Zone Zone;
        public bool Wait;

        public GoToZoneTask()
        {

        }

        public GoToZoneTask(Zone zone)
        {
            Zone = zone;
            Category = TaskCategory.Other;
            Priority = PriorityType.Medium;
            ReassignOnDeath = false;
            Name = "Go to " + Zone.ID;
        }

        public override Act CreateScript(Creature agent)
        {
            if (!Wait)
                return new GoToZoneAct(agent.AI, Zone);

            return new GoToZoneAct(agent.AI, Zone) & new Wait(999) { Name = "Wait." };
        }
    }

    /// <summary>
    /// Tells a creature that it should find an item with the specified
    /// tags and put it in a given zone.
    /// </summary>
    internal class BuildRoomTask : Task
    {
        public BuildZoneOrder Zone;

        public BuildRoomTask()
        {
            Category = TaskCategory.BuildZone;
            Priority = PriorityType.Medium;
            MaxAssignable = 3;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        public BuildRoomTask(BuildZoneOrder zone, ZoneBuilder Builder)
        {
            Category = TaskCategory.BuildZone;
            MaxAssignable = 3;
            Name = "Build Room " + zone.ToBuild.Type.Name + zone.ToBuild.ID;
            Zone = zone;
            Priority = PriorityType.Medium;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        private bool IsRoomBuildOrder(Faction faction, BuildZoneOrder buildRooom)
        {
            return faction.World.ZoneBuilder.IsActiveBuildZoneOrder(buildRooom);
        }


        public override Feasibility IsFeasible(Creature agent)
        {
            return Zone != null && !Zone.IsBuilt && IsRoomBuildOrder(agent.Faction, Zone) &&
                agent.Stats.IsTaskAllowed(Task.TaskCategory.BuildZone) &&
                agent.World.HasResources(Zone.ListRequiredResources()) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override Act CreateScript(Creature creature)
        {
            if (Zone == null)
                return null;

            return new BuildRoomAct(creature.AI, Zone, creature.World.ZoneBuilder);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return (Zone == null || Zone.IsBuilt || Zone.IsDestroyed) ? 1000 : 1.0f;
        }

        public override bool ShouldDelete(Creature agent)
        {
            return Zone == null || Zone.IsBuilt || Zone.IsDestroyed || !IsRoomBuildOrder(agent.Faction, Zone);
        }

        public override bool ShouldRetry(Creature agent)
        {
            return Zone != null && !Zone.IsBuilt && !Zone.IsDestroyed;
        }

        public override bool IsComplete(Faction faction)
        {
            return Zone == null || Zone.IsBuilt || !IsRoomBuildOrder(faction, Zone);
        }

        public override void OnDequeued(Faction Faction)
        {
            if (!Zone.IsBuilt)
                Zone.Destroy();
        }
    }

}