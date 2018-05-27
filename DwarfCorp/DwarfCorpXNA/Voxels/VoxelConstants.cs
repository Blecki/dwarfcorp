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
        public const Int32 ChunkVoxelCount = ChunkSizeX * ChunkSizeY * ChunkSizeZ;

        public const Int32 XDivShift = 4;
        public const Int32 YDivShift = 6;
        public const Int32 ZDivShift = 4;

        public const Int32 XModMask = 0x0000000F;
        public const Int32 YModMask = 0x0000003F;
        public const Int32 ZModMask = 0x0000000F;

        public static Int32 DataIndexOf(LocalVoxelCoordinate C)
        {
            return (C.Y * ChunkSizeX * ChunkSizeZ) + (C.Z * ChunkSizeX) + C.X;
        }
    }
}
