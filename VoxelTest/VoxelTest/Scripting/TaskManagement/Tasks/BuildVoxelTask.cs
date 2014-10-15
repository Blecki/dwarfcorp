using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            
        }

        public BuildVoxelTask(Voxel voxel, VoxelType type)
        {
            Name = "Put voxel of type: " + type.Name + " on voxel " + voxel.Position;
            Voxel = voxel;
            VoxType = type;
        }

        public override Task Clone()
        {
            return new BuildVoxelTask(Voxel, VoxType);
        }

        public override float ComputeCost(Creature agent)
        {
            return Voxel == null ? 1000 : (agent.AI.Position - Voxel.Position).LengthSquared();
        }

        public override Act CreateScript(Creature creature)
        {
            return new BuildVoxelAct(creature.AI, Voxel, VoxType);
        }
    }

}