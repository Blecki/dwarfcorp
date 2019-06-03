using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static VoxelHandle GetVoxelBelow(VoxelHandle V)
        {
            if (!V.IsValid) return VoxelHandle.InvalidHandle;
            return new VoxelHandle(V.Chunk.Manager, new GlobalVoxelCoordinate(V.Coordinate.X, V.Coordinate.Y - 1, V.Coordinate.Z));
        }
    }
}
