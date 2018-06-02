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

        //  Todo: Use inverse mask naming pattern here.
        public const Int32 MaximumGrassTypes = 16;
        public const Int32 GrassTypeShift = 4;
        public const Int32 GrassTypeMask = 0xF0;
        public const Int32 MaximumGrassDecay = 16;
        public const Int32 GrassDecayMask = 0x0F;

        public const Int32 RampTypeShift = 0x0;
        public const Int32 RampTypeMask = 0x1F;
        public const Int32 InverseRampTypeMask = 0xE0;
        public const Int32 SunlightShift = 5;
        public const Int32 SunlightMask = 0x20;
        public const Int32 InverseSunlightMask = 0xDF;
        public const Int32 ExploredShift = 6;
        public const Int32 ExploredMask = 0x40;
        public const Int32 InverseExploredMask = 0xBF;
        public const Int32 PathingObjectPresentShift = 7;
        public const Int32 PathingObjectPresentMask = 0x80;
        public const Int32 InversePathingObjectPresentMask = 0x7F;

        public const Int32 LiquidTypeShift = 6;
        public const Int32 LiquidTypeMask = 0xC0;
        public const Int32 InverseLiquidTypeMask = 0x3F;
        public const Int32 LiquidLevelMask = 0x3F;
        public const Int32 InverseLiquidLevelMask = 0xC0;
    }
}
