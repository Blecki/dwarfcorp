using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DwarfCorp
{
    public static class OceanVoxelHook
    {
        [VoxelUpdateHook("OCEAN")]
        private static void _hook(VoxelHandle Voxel, WorldManager World)
        {
            // Water update should take care of this part.
            Voxel.LiquidLevel = WaterManager.maxWaterLevel;
            Voxel.LiquidType = LiquidType.Water;

            
            if (Library.GetVoxelType("Ocean").HasValue(out var ocean))
            {
                foreach (var voxel in VoxelHelpers.EnumerateManhattanNeighbors2D_Y(Voxel.Coordinate).Select(c => World.ChunkManager.CreateVoxelHandle(c)))
                {
                    if (voxel.IsValid && voxel.IsEmpty)
                    {
                        var lv = voxel;
                        lv.Type = ocean;
                    }
                }

                var below = VoxelHelpers.GetVoxelBelow(Voxel);
                if (below.IsValid && below.IsEmpty)
                    below.Type = ocean;
            }
        }
    }
}
