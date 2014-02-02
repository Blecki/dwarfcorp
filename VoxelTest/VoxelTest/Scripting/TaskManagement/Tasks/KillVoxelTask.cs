using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should destroy a voxel via digging.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class KillVoxelTask : Task
    {
        public VoxelRef VoxelToKill = null;

        public KillVoxelTask()
        {

        }

        public KillVoxelTask(VoxelRef vox)
        {
            Name = "Kill Voxel: " + vox.WorldPosition;
            VoxelToKill = vox;
        }

        public override Act CreateScript(Creature creature)
        {
            return new KillVoxelAct(VoxelToKill, creature.AI);
        }

        public override bool IsFeasible(Creature agent)
        {
            if(VoxelToKill == null || VoxelToKill.TypeName == "empty")
            {
                return false;
            }

            Voxel vox = VoxelToKill.GetVoxel(false);

            if(vox == null)
            {
                return false;
            }

            return true;
        }

        public override float ComputeCost(Creature agent)
        {
            return VoxelToKill == null ? 1000 : (agent.AI.Position - VoxelToKill.WorldPosition).LengthSquared();
        }
    }

}