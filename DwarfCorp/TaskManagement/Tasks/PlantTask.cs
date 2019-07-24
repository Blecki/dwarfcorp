using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class PlantTask : Task
    {
        public Farm FarmToWork;
        public string Plant;
        public List<ResourceAmount> RequiredResources;

        public PlantTask()
        {
            Priority = TaskPriority.Medium;
            Category = TaskCategory.Plant;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Default.Energy_Arduous;
        }

        public PlantTask(Farm farmToWork)
        {
            FarmToWork = farmToWork;
            Name = "Plant " + Plant + " at " + FarmToWork.Voxel.Coordinate;
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

            if (FarmToWork == null)
                return Feasibility.Infeasible;

            if (FarmToWork.Finished)
                return Feasibility.Infeasible;

            if (!agent.World.HasResources(RequiredResources))
                return Feasibility.Infeasible;

            return Feasibility.Feasible;
        }

        public override bool IsComplete(WorldManager World)
        {
            if (FarmToWork == null) return true;
            if (FarmToWork.Finished) return true;
            if (FarmToWork.Voxel.IsEmpty) return true;
            if (!FarmToWork.Voxel.Type.IsSoil) return true;
            return false;
        }

        private IEnumerable<Act.Status> Cleanup(CreatureAI creature)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
            {
                if (creature.Faction == creature.World.PlayerFaction)
                {
                    creature.World.MakeAnnouncement(String.Format("{0} cancelled farming task because it is unreachable", creature.Stats.FullName));
                    creature.World.TaskManager.CancelTask(this);
                }
                creature.SetMessage("Failed to plant. Task was unreachable.");
                yield return Act.Status.Fail;
                yield break;
            }
            yield return Act.Status.Success;
        }

        public override MaybeNull<Act> CreateScript(Creature agent)
        {
            return (new PlantAct(agent.AI) { Resources = RequiredResources, FarmToWork = FarmToWork, Name = "Work " + FarmToWork.Voxel.Coordinate } 
            | new Wrap(() => Cleanup(agent.AI))) & new Wrap(() => Cleanup(agent.AI));
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            if (FarmToWork == null) return float.MaxValue;
            else
            {
                return (FarmToWork.Voxel.WorldPosition - agent.AI.Position).LengthSquared();
            }
        }

        public override void OnEnqueued(WorldManager World)
        {
            World.PersistentData.Designations.AddVoxelDesignation(FarmToWork.Voxel, DesignationType.Plant, FarmToWork, this);
        }

        public override void OnDequeued(WorldManager World)
        {
            World.PersistentData.Designations.RemoveVoxelDesignation(FarmToWork.Voxel, DesignationType.Plant);
        }

        public override Vector3? GetCameraZoomLocation()
        {
            return FarmToWork?.Voxel.Center;
        }
    }
}
