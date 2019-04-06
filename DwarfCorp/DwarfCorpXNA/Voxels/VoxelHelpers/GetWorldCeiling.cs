using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static VoxelHandle GetWorldCeiling(ChunkManager ChunkManager, int X, int Z)
        {
            return ChunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(X, VoxelConstants.WorldSizeY - 1, Z));
        }
    }
}
