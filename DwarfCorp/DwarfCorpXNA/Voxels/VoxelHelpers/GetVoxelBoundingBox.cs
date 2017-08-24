using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static BoundingBox GetVoxelBoundingBox(IEnumerable<VoxelHandle> Voxels)
        {
            Vector3 maxPos = new Vector3(Single.MinValue, Single.MinValue, Single.MinValue);
            Vector3 minPos = new Vector3(Single.MaxValue, Single.MaxValue, Single.MaxValue);

            foreach (var v in Voxels)
            {
                if (v.Coordinate.X < minPos.X)
                    minPos.X = v.Coordinate.X;
                if (v.Coordinate.Y < minPos.Y)
                    minPos.Y = v.Coordinate.Y;
                if (v.Coordinate.Z < minPos.Z)
                    minPos.Z = v.Coordinate.Z;

                if (v.Coordinate.X > maxPos.X)
                    maxPos.X = v.Coordinate.X;
                if (v.Coordinate.Y > maxPos.Y)
                    maxPos.Y = v.Coordinate.Y;
                if (v.Coordinate.Z > maxPos.Z)
                    maxPos.Z = v.Coordinate.Z;

                
            }

            return new BoundingBox(minPos, maxPos + Vector3.One);
        }
    }
}
