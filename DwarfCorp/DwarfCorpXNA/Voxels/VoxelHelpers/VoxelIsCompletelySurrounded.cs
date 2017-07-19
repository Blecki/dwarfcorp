using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static bool VoxelIsCompletelySurrounded(TemporaryVoxelHandle V)
        {
            if (V.Chunk == null)
                return false;

            foreach (var neighborCoordinate in VoxelHelpers.EnumerateManhattanNeighbors(V.Coordinate))
            {
                var voxelHandle = new TemporaryVoxelHandle(V.Chunk.Manager.ChunkData, neighborCoordinate);
                if (!voxelHandle.IsValid) return false;
                if (voxelHandle.IsEmpty) return false;
            }

            return true;
        }
    }
}
