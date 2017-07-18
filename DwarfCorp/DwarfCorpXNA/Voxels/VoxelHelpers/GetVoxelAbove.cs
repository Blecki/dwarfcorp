using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static TemporaryVoxelHandle GetVoxelAbove(TemporaryVoxelHandle V)
        {
            return new TemporaryVoxelHandle(V.Chunk.Manager.ChunkData,
                new GlobalVoxelCoordinate(V.Coordinate.X, V.Coordinate.Y + 1, V.Coordinate.Z));
        }
    }
}
