using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static TemporaryVoxelHandle GetFirstVoxelAbove(TemporaryVoxelHandle V)
        {
            var p = V.Coordinate.GetLocalVoxelCoordinate();

            for (int y = p.Y; y < VoxelConstants.ChunkSizeY; y++)
            {
                var above = new TemporaryVoxelHandle(V.Chunk, new LocalVoxelCoordinate(p.X, y, p.Z));
                if (above.IsValid && !above.IsEmpty)
                    return above;
            }

            return new TemporaryVoxelHandle(null, p);
        }
    }
}
