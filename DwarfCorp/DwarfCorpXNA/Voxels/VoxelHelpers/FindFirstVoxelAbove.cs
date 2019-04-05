using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        /// <summary>
        /// Finds the first voxel above the given location, including the location itself.
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        public static VoxelHandle FindFirstVoxelAbove(VoxelHandle V)
        {
            while (true)
            {
                if (!V.IsValid) return VoxelHandle.InvalidHandle;
                if (!V.IsEmpty) return V;
                V = V.Chunk.Manager.CreateVoxelHandle(V.Coordinate + new GlobalVoxelOffset(0, 1, 0));
            }
        }
    }
}
