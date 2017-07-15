using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static TemporaryVoxelHandle FindFirstVoxelBelow(TemporaryVoxelHandle V)
        {
            if (!V.IsValid) return V;

            var p = V.Coordinate.GetLocalVoxelCoordinate();

            for (int y = p.Y; y >= 0; --y)
            {
                var vox = new TemporaryVoxelHandle(V.Chunk, new LocalVoxelCoordinate(p.X, y, p.Z));
                if (vox.IsValid && !vox.IsEmpty)
                    return vox;
            }

            return TemporaryVoxelHandle.InvalidHandle;
        }

        public static TemporaryVoxelHandle FindFirstVoxelBelowIncludeWater(TemporaryVoxelHandle V)
        {
            var p = V.Coordinate.GetLocalVoxelCoordinate();

            for (int y = p.Y; y >= 0; --y)
            {
                var vox = new TemporaryVoxelHandle(V.Chunk, new LocalVoxelCoordinate(p.X, y, p.Z));
                if (vox.IsValid && (!vox.IsEmpty || vox.WaterCell.WaterLevel > 0))
                    return vox;
            }

            return TemporaryVoxelHandle.InvalidHandle;
        }
    }
}
