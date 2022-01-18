using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DwarfCorp
{
    public static class MagicSpreaderVoxelHook
    {
        [VoxelUpdateHook("MAGICSPREADER")]
        private static void _hook(VoxelHandle Voxel, WorldManager World)
        {
            if (Library.GetVoxelType("MagicSpreader").HasValue(out var ocean))
            {
                foreach (var voxel in VoxelHelpers.EnumerateManhattanNeighbors2D_Y(Voxel.Coordinate).Select(c => World.ChunkManager.CreateVoxelHandle(c)))
                {
                    if (voxel.IsValid && voxel.IsEmpty)
                    {
                        var lv = voxel;
                        lv.Type = ocean;
                        LiquidCellHelpers.ClearVoxelOfLiquid(voxel);
                    }
                }

                var below = VoxelHelpers.GetVoxelBelow(Voxel);
                if (below.IsValid && below.IsEmpty)
                    below.Type = ocean;
            }
        }
    }
}
