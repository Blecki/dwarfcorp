using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static bool VoxelIsCompletelySurrounded(VoxelHandle V)
        {
            if (V.Chunk == null)
                return false;

            foreach (var neighborCoordinate in VoxelHelpers.EnumerateManhattanNeighbors(V.Coordinate))
            {
                var voxelHandle = new VoxelHandle(V.Chunk.Manager, neighborCoordinate);
                if (!voxelHandle.IsValid) return false;
                if (voxelHandle.IsEmpty) return false;
            }

            return true;
        }

        public static bool VoxelIsSurroundedByWater(VoxelHandle V)
        {
            if (V.Chunk == null)
                return false;

            foreach (var neighborCoordinate in VoxelHelpers.EnumerateManhattanNeighbors(V.Coordinate))
            {
                var voxelHandle = new VoxelHandle(V.Chunk.Manager, neighborCoordinate);
                if (!voxelHandle.IsValid) return false;
                if (voxelHandle.IsEmpty && LiquidCellHelpers.CountCellsWithWater(voxelHandle) < 5) return false;
            }

            return true;
        }
    }
}
