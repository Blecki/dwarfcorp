using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    internal class BuildZoneTask : Task
    {
        public BuildZoneOrder Zone;

        public BuildZoneTask()
        {
            Category = TaskCategory.BuildZone;
            Priority = TaskPriority.Medium;
            MaxAssignable = 3;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Default.Energy_Tiring;
        }

        public BuildZoneTask(BuildZoneOrder zone)
        {
            Category = TaskCategory.BuildZone;
            MaxAssignable = 3;
            Name = "Build Room " + zone.ToBuild.Type.Name + zone.ToBuild.ID;
            Zone = zone;
            Priority = TaskPriority.Medium;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Default.Energy_Tiring;
        }

        private bool IsZoneBuildOrder(WorldManager World, BuildZoneOrder buildRooom)
        {
            return World.IsActiveBuildZoneOrder(buildRooom);
        }


        public override Feasibility IsFeasible(Creature agent)
        {
            return Zone != null && !Zone.IsBuilt && IsZoneBuildOrder(agent.World, Zone) &&
                agent.Stats.IsTaskAllowed(TaskCategory.BuildZone) &&
                agent.World.HasResources(Zone.ListRequiredResources()) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
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

        public override Vector3? GetCameraZoomLocation()
        {
            return Zone.GetBoundingBox().Center();
        }
    }

}