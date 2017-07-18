using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class VoxelConstants
    {
        public const Int32 ChunkSizeX = 16;
        public const Int32 ChunkSizeY = 64;
        public const Int32 ChunkSizeZ = 16;
        public const Int32 ChunkVoxelCount = 16 * 64 * 16;

        public const Int32 XDivShift = 4;
        public const Int32 YDivShift = 6;
        public const Int32 ZDivShift = 4;

        public static Int32 DataIndexOf(LocalVoxelCoordinate C)
        {
            return (C.Z * ChunkSizeY + C.Y) * ChunkSizeX + C.X;
        }
    }
}
