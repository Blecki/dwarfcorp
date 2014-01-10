using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should guard a voxel.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class GuardVoxelTask : Task
    {
        public VoxelRef VoxelToGuard = null;

        public GuardVoxelTask(VoxelRef vox)
        {
            Name = "Guard Voxel: " + vox.WorldPosition;
            VoxelToGuard = vox;
        }

        public override Act CreateScript(Creature agent)
        {
            return new GuardVoxelAct(agent.AI, VoxelToGuard);
        }

        public override float ComputeCost(Creature agent)
        {
            return VoxelToGuard == null ? 1000 : (agent.AI.Position - VoxelToGuard.WorldPosition).LengthSquared();
        }
    }

}