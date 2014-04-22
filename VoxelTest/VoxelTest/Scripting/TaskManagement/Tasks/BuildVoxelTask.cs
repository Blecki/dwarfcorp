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
        public TagList Tags { get; set; }
        public VoxelType VoxType { get; set; }
        public VoxelRef Voxel { get; set; }

        public BuildVoxelTask()
        {
            
        }

        public BuildVoxelTask(TagList tags, VoxelRef voxel, VoxelType type)
        {
            Name = "Put voxel of type: " + type.Name + " on voxel " + voxel.WorldPosition;
            Tags = tags;
            Voxel = voxel;
            VoxType = type;
        }


        public override float ComputeCost(Creature agent)
        {
            return Voxel == null ? 1000 : (agent.AI.Position - Voxel.WorldPosition).LengthSquared();
        }

        public override Act CreateScript(Creature creature)
        {
            return new BuildVoxelAct(creature.AI, Voxel, Tags);
        }
    }

}