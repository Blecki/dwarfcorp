using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class PlantTask : Task
    {
        public Farm Farm;

        public PlantTask()
        {
            Priority = TaskPriority.Medium;
            Category = TaskCategory.Plant;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Default.Energy_Arduous;
        }

        public PlantTask(Farm Farm)
        {
            this.Farm = Farm;
            Name = "Plant " + Farm.SeedType + " at " + Farm.Voxel.Coordinate;
            Priority = TaskPriority.Medium;
            AutoRetry = true;
            Category = TaskCategory.Plant;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Default.Energy_Arduous;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return true;
        }

        public override bool ShouldDelete(Creature agent)
        {
            return IsFeasible(agent) == Feasibility.Infeasible;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.Stats.IsTaskAllowed(TaskCategory.Plant))
                return Feasibility.Infeasible;

            if (agent.AI.Stats.IsAsleep)
                return Feasibility.Infeasible;

            if (Farm == null)
                return Feasibility.Infeasible;

            if (Farm.Finished)
                return Feasibility.Infeasible;

            if (!agent.World.HasResources(new List<ResourceTypeAmount> { new ResourceTypeAmount(Farm.SeedType, 1) }))
                return Feasibility.Infeasible;

            return Feasibility.Feasible;
        }

        public override bool IsComplete(WorldManager World)
        {
            if (Farm == null) return true;
            if (Farm.Finished) return true;
            if (Farm.Voxel.IsEmpty) return true;
            if (!Farm.Voxel.Type.IsSoil) return true;
            return false;
        }

        private IEnumerable<Act.Status> Cleanup(CreatureAI creature)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
            {
                creature.SetMessage("Failed to plant. Task was unreachable.");
                yield return Act.Status.Fail;
                yield break;
            }
            yield return Act.Status.Success;
        }

        public override MaybeNull<Act> CreateScript(Creature agent)
        {
            return (new PlantAct(agent.AI, Farm) 
            | new Wrap(() => Cleanup(agent.AI))) & new Wrap(() => Cleanup(agent.AI));
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            if (Farm == null) return float.MaxValue;
            else
            {
                return (Farm.Voxel.WorldPosition - agent.AI.Position).LengthSquared();
            }
        }

        public override void OnEnqueued(WorldManager World)
        {
            World.PersistentData.Designations.AddVoxelDesignation(Farm.Voxel, DesignationType.Plant, Farm, this);
        }

        public override void OnDequeued(WorldManager World)
        {
            World.PersistentData.Designations.RemoveVoxelDesignation(Farm.Voxel, DesignationType.Plant);
        }

        public override Vector3? GetCameraZoomLocation()
        {
            return Farm?.Voxel.Center;
        }
    }
}
