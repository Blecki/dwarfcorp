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
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        public BuildVoxelTask(VoxelHandle voxel, string type)
        {
            Category = TaskCategory.BuildBlock;
            Name = "Put voxel of type: " + type + " on voxel " + voxel.Coordinate;
            Voxel = voxel;
            VoxType = type;
            Priority = PriorityType.Medium;
            BoredomIncrease = GameSettings.Default.Boredom_NormalTask;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.AI.Stats.CurrentClass.IsTaskAllowed(TaskCategory.BuildBlock))
                return Feasibility.Infeasible;

            if (agent.AI.Stats.IsAsleep)
                return Feasibility.Infeasible;

            if (!agent.AI.Faction.Designations.IsVoxelDesignation(Voxel, DesignationType.Put))
            {
                return Feasibility.Infeasible;
            }


            var voxtype = Library.GetVoxelType(VoxType);
            return agent.Faction.CanBuildVoxel(voxtype) ? Feasibility.Feasible : Feasibility.Infeasible;
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
                    creature.World.TaskManager.CancelTask(this);
                }
                return false;
            }

            return creature.Creature.Inventory.HasResource(resources);
        }

        public override Act CreateScript(Creature creature)
        {
            var voxtype = Library.GetVoxelType(VoxType);
            var resource = creature.World.ListResources().Where(r => voxtype.CanBuildWith(ResourceLibrary.GetResourceByName(r.Key))).FirstOrDefault();
            
            if (resource.Key == null)
            {
                return null;
            }

            var resources = new ResourceAmount(resource.Value.Type, 1);

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