using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        /// <summary>
        /// Snaps the supplied coordinate into valid world space. Returns nearest valid voxel to the point.
        /// </summary>
        /// <param name="chunks"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static VoxelHandle FindValidVoxelNear(ChunkManager chunks, Microsoft.Xna.Framework.Vector3 pos)
        {
            Microsoft.Xna.Framework.BoundingBox bounds = chunks.Bounds;
            bounds.Max = new Microsoft.Xna.Framework.Vector3(bounds.Max.X, VoxelConstants.WorldSizeY, bounds.Max.Z);
            var clampedPos = MathFunctions.Clamp(pos, chunks.Bounds) + Microsoft.Xna.Framework.Vector3.Down * 0.05f;

            return chunks.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(clampedPos));
        }
    }
}
