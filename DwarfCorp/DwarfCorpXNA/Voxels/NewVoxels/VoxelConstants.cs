using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class VoxelConstants
    {
        // Todo: %KILL% all other chunk size variables.
        public const Int32 ChunkSizeX = 16;
        public const Int32 ChunkSizeY = 64;
        public const Int32 ChunkSizeZ = 16;

        public static Int32 DataIndexOf(LocalVoxelCoordinate C)
        {
            return (C.Z * ChunkSizeY + C.Y) * ChunkSizeX + C.X;
        }
    }
}
