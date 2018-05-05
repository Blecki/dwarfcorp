using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        /// <summary>
        /// Use this to hash voxel handles for effecient lookup in containers.
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        public static ulong GetVoxelQuickCompare(VoxelHandle V)
        {
            var coord = V.Coordinate.GetGlobalChunkCoordinate();
            var index = VoxelConstants.DataIndexOf(V.Coordinate.GetLocalVoxelCoordinate());

            ulong q = 0;
            q |= (((ulong)coord.X & 0xFFFF) << 48);
            q |= (((ulong)coord.Y & 0xFFFF) << 32);
            q |= (((ulong)coord.Z & 0xFFFF) << 16);
            q |= ((ulong)index & 0xFFFF);
            return q;
        }       
    }
}
