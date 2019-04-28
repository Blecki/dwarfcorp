using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static VoxelHandle FindFirstVoxelBelowIncludingWater(VoxelHandle V)
        {
            if (!V.IsValid) return VoxelHandle.InvalidHandle;
            V = V.Chunk.Manager.CreateVoxelHandle(V.Coordinate + new GlobalVoxelOffset(0, -1, 0));

            while (true)
            {
                if (!V.IsValid) return VoxelHandle.InvalidHandle;
                if (!V.IsEmpty || V.LiquidLevel > 0) return V;
                V = V.Chunk.Manager.CreateVoxelHandle(V.Coordinate + new GlobalVoxelOffset(0, -1, 0));
            }
        }        
    }
}
