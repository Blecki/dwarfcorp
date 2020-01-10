using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    internal class BuildVoxelTask : Task
    {
        public string VoxType;
        public VoxelHandle Voxel;

        public BuildVoxelTask()
        {
            Category = TaskCategory.BuildBlock;
            Priority = TaskPriority.Medium;
            BoredomIncrease = GameSettings.Current.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Current.Energy_Tiring;
        }

        public BuildVoxelTask(VoxelHandle voxel, string type)
        {
            Category = TaskCategory.BuildBlock;
            Name = "Place " + type + " at " + voxel.Coordinate;
            Voxel = voxel;
            VoxType = type;
            Priority = TaskPriority.Medium;
            BoredomIncrease = GameSettings.Current.Boredom_NormalTask;
            EnergyDecrease = GameSettings.Current.Energy_Tiring;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.AI.Stats.CurrentClass.IsTaskAllowed(TaskCategory.BuildBlock))
                return Feasibility.Infeasible;

            if (agent.AI.Stats.IsAsleep)
                return Feasibility.Infeasible;

            if (!agent.World.PersistentData.Designations.IsVoxelDesignation(Voxel, DesignationType.Put))
                return Feasibility.Infeasible;

            if (Library.GetVoxelType(VoxType).HasValue(out VoxelType voxtype))
                return agent.World.CanBuildVoxel(voxtype) ? Feasibility.Feasible : Feasibility.Infeasible;
            else
                return Feasibility.Infeasible;
        }

        public override bool ShouldDelete(Creature agent)
        {
            return !Voxel.IsValid || !agent.World.PersistentData.Designations.IsVoxelDesignation(Voxel, DesignationType.Put);
        }

        public override bool ShouldRetry(Creature agent)
        {
            return Voxel.IsValid && agent.World.PersistentData.Designations.IsVoxelDesignation(Voxel, DesignationType.Put);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return !Voxel.IsValid ? 1000 : 0.01f * (agent.AI.Position - Voxel.WorldPosition).LengthSquared() + (Voxel.Coordinate.Y);
        }

        public bool Validate(CreatureAI creature, VoxelHandle voxel, ResourceTypeAmount Resource)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
                return false;

            return creature.Creature.Inventory.HasResource(Resource);
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
        {
            if (Library.GetVoxelType(VoxType).HasValue(out VoxelType voxtype))
            {
                var resource = creature.World.ListResources().Where(r => Library.GetResourceType(r.Key).HasValue(out var res) && voxtype.CanBuildWith(res)).FirstOrDefault();

                if (resource.Key == null)
                    return null;

                var resources = new ResourceTypeAmount(resource.Value.Type, 1);

                return new Select(
                    new Sequence(
                        ActHelper.CreateEquipmentCheckAct(creature.AI, "Tool", ActHelper.EquipmentFallback.NoFallback, "Hammer"),
                        new GetResourcesOfType(creature.AI, new List<ResourceTypeAmount>() { resources }) { BlackboardEntry = "stashed-resource" },
                        new Domain(() => Validate(creature.AI, Voxel, resources),
                            new GoToVoxelAct(Voxel, PlanAct.PlanType.Radius, creature.AI, 4.0f)),
                        new PlaceVoxelAct(Voxel, creature.AI, "stashed-resource", VoxType)),
                    new Wrap(creature.RestockAll))
                { Name = "Build Voxel" };
            }
            else
                return null;
        }

        public override void Render(DwarfTime time)
        {
            base.Render(time);
        }

        public override bool IsComplete(WorldManager World)
        {
            return Voxel.IsValid && Voxel.Type.Name == VoxType;
        }

        public override void OnEnqueued(WorldManager World)
        {
            World.PersistentData.Designations.AddVoxelDesignation(Voxel, DesignationType.Put, VoxType, this);
        }

        public override void OnDequeued(WorldManager World)
        {
            World.PersistentData.Designations.RemoveVoxelDesignation(Voxel, DesignationType.Put);
        }

        public override Vector3? GetCameraZoomLocation()
        {
            return Voxel.Center;
        }
    }
}