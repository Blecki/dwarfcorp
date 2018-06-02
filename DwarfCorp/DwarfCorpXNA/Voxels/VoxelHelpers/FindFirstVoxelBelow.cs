using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static VoxelHandle FindFirstVoxelBelow(VoxelHandle V)
        {
            if (!V.IsValid) return V;

            var p = V.Coordinate.GetLocalVoxelCoordinate();

            for (int y = p.Y; y >= 0; --y)
            {
                var vox = new VoxelHandle(V.Chunk, new LocalVoxelCoordinate(p.X, y, p.Z));
                if (!vox.IsEmpty)
                    return vox;
            }

            return VoxelHandle.InvalidHandle;
        }

        public static VoxelHandle FindFirstVoxelBelowIncludeWater(VoxelHandle V)
        {
            if (!V.IsValid) return V;

            var p = V.Coordinate.GetLocalVoxelCoordinate();

            for (int y = p.Y; y >= 0; --y)
            {
                var vox = new VoxelHandle(V.Chunk, new LocalVoxelCoordinate(p.X, y, p.Z));
                if (vox.IsValid && (!vox.IsEmpty || vox.LiquidLevel > 0))
                    return vox;
            }

            return VoxelHandle.InvalidHandle;
        }
    }
}
