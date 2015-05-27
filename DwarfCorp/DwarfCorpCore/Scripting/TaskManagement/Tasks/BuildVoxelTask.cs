using System;
using System.Collections.Generic;
using System.Linq;
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
        public VoxelType VoxType { get; set; }
        public Voxel Voxel { get; set; }

        public BuildVoxelTask()
        {
            Priority = PriorityType.Low;
        }

        public BuildVoxelTask(Voxel voxel, VoxelType type)
        {
            Name = "Put voxel of type: " + type.Name + " on voxel " + voxel.Position;
            Voxel = voxel;
            VoxType = type;
            Priority = PriorityType.Low;
        }

        public override bool IsFeasible(Creature agent)
        {
            return Voxel != null && agent.Faction.WallBuilder.IsDesignation(Voxel);
        }

        public override bool ShouldDelete(Creature agent)
        {
            return Voxel == null || !agent.Faction.WallBuilder.IsDesignation(Voxel);
        }

        public override bool ShouldRetry(Creature agent)
        {
            return Voxel != null && agent.Faction.WallBuilder.IsDesignation(Voxel);
        }

        public override Task Clone()
        {
            return new BuildVoxelTask(Voxel, VoxType);
        }

        public override float ComputeCost(Creature agent)
        {
            return Voxel == null ? 1000 : 0.01f * (agent.AI.Position - Voxel.Position).LengthSquared() + (Voxel.Position.Y);
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
            if (Voxel != null)
            {
                
            }
            base.Render(time);
        }
    }

}