using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DwarfCorp
{
    public static class MagicDuplicatorVoxelHook
    {
        [VoxelUpdateHook("MAGICDUPLICATOR")]
        private static void _hook(VoxelHandle Voxel, WorldManager World)
        {
            var above = VoxelHelpers.GetVoxelAbove(Voxel);
            if (above.IsValid && !above.IsEmpty)
            {
                VoxelHelpers.KillVoxel(World, Voxel);
                Voxel.Type = above.Type;
            }
        }
    }
}
