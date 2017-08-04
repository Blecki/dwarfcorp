using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static bool DoesRayHitSolidVoxel(ChunkData Data, Vector3 Start, Vector3 End)
        {
            foreach (var coordinate in MathFunctions.FastVoxelTraversal(Start, End))
            {
                var voxel = new VoxelHandle(Data, coordinate);
                if (voxel.IsValid && !voxel.IsEmpty)
                    return true;
            }

            return false;
        }
    }
}
