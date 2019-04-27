using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static VoxelHandle FindFirstVisibleVoxelOnRay(
            ChunkManager Data,
            Vector3 Start,
            Vector3 End)
        {
            foreach (var coordinate in MathFunctions.FastVoxelTraversal(Start, End))
            {
                var voxel = new VoxelHandle(Data, coordinate);

                if (voxel.IsValid && voxel.IsVisible && !voxel.IsEmpty)
                    return voxel;
            }

            return VoxelHandle.InvalidHandle;
        }

        public static VoxelHandle FindFirstVisibleVoxelOnRayEx(
            ChunkManager Data,
            Vector3 Start,
            Vector3 End,
            bool SelectEmpty,
            Func<VoxelHandle, bool> FilterPredicate)
        {
            if (FilterPredicate == null)
            {
                FilterPredicate = v => v.IsValid && !v.IsEmpty;
            }

            var prev = VoxelHandle.InvalidHandle;
            foreach (var coordinate in MathFunctions.FastVoxelTraversal(Start, End))
            {
                var voxel = new VoxelHandle(Data, coordinate);

                if (voxel.IsValid && voxel.IsVisible && FilterPredicate(voxel))
                {
                    if (SelectEmpty)
                        return prev;
                    else
                        return voxel;
                }

                prev = voxel;
            }

            return VoxelHandle.InvalidHandle;
        }

        public static VoxelHandle FindFirstVisibleVoxelOnScreenRay(
            ChunkManager Data,
            int X, int Y,
            Camera Camera,
            Viewport Viewport,
            float Distance,
            bool SelectEmpty,
            Func<VoxelHandle, bool> FilterPredicate)
        {
            var near = Viewport.Unproject(new Vector3(X, Y, 0),
                Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);
            var far = Viewport.Unproject(new Vector3(X, Y, 1),
                Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);

            return VoxelHelpers.FindFirstVisibleVoxelOnRayEx(Data, near, near + Vector3.Normalize(far - near) * Distance, SelectEmpty, FilterPredicate);
        }
    }
}
