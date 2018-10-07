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
        /// It is perfectly safe to assume the search space is entirely in one chunk as the world
        /// is only one chunk deep on the vertical axis.
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        public static VoxelHandle FindFirstVoxelAbove(VoxelHandle V)
        {
            var p = V.Coordinate.GetLocalVoxelCoordinate();
            if (V.Chunk == null)
            {
                return VoxelHandle.InvalidHandle;
            }
            for (int y = p.Y; y < VoxelConstants.ChunkSizeY; ++y)
            {
                var above = new VoxelHandle(V.Chunk, new LocalVoxelCoordinate(p.X, y, p.Z));
                if (above.IsValid && !above.IsEmpty)
                    return above;
            }

            return VoxelHandle.InvalidHandle;
        }

        public static VoxelHandle GetValidVoxelNear(ChunkManager chunks, Microsoft.Xna.Framework.Vector3 pos)
        {
            Microsoft.Xna.Framework.BoundingBox bounds = chunks.Bounds;
            bounds.Max = new Microsoft.Xna.Framework.Vector3(bounds.Max.X, VoxelConstants.ChunkSizeY, bounds.Max.Z);
            var clampedPos = MathFunctions.Clamp(pos, chunks.Bounds) + Microsoft.Xna.Framework.Vector3.Down * 0.05f;
            return new VoxelHandle(chunks.ChunkData, GlobalVoxelCoordinate.FromVector3(clampedPos));
        }
    }
}
