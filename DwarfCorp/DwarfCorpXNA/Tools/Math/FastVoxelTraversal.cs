using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public static partial class MathFunctions
    {
        /// <summary>
        /// Rasterizes the line, producing a list of Point3's that intersect the line segment.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns></returns>
        public static IEnumerable<Point3> RasterizeLine(Vector3 start, Vector3 end)
        {
            // From "A Fast DestinationVoxel Traversal Algorithm for Ray Tracing"
            // by John Amanatides and Andrew Woo, 1987
            // <http://www.cse.yorku.ca/~amana/research/grid.pdf>
            // <http://citeseer.ist.psu.edu/viewdoc/summary?doi=10.1.1.42.3443>
            // Extensions to the described algorithm:
            //   • Imposed a distance limit.

            // The foundation of this algorithm is a parameterized representation of
            // the provided ray,
            //                    origin + t * direction,
            // except that t is not actually stored; rather, at any given point in the
            // traversal, we keep track of the *greater* t values which we would have
            // if we took a step sufficient to cross a cube boundary along that axis
            // (i.e. change the integer part of the coordinate) in the variables
            // tMaxX, tMaxY, and tMaxZ.

            // Cube containing origin point.
            var x = start.X;
            var y = start.Y;
            var z = start.Z;
            Vector3 direction = new Vector3(end.X, end.Y, end.Z) - new Vector3(start.X, start.Y, start.Z);

            if (L1(start, end) < 1e-12 || HasNan(start) || HasNan(end))
            {
                yield break;
            }

            float d1 = direction.Length();

            direction.Normalize();
            // Break out direction vector.
            var dx = direction.X;
            var dy = direction.Y;
            var dz = direction.Z;
            // Direction to increment x,y,z when stepping.
            var stepX = Math.Sign(dx);
            var stepY = Math.Sign(dy);
            var stepZ = Math.Sign(dz);
            // See description above. The initial values depend on the fractional
            // part of the origin.
            var tMaxX = IntBound(x, dx);
            var tMaxY = IntBound(y, dy);
            var tMaxZ = IntBound(z, dz);
            // The change in t when taking a step (always positive).
            var tDeltaX = stepX / dx;
            var tDeltaY = stepY / dy;
            var tDeltaZ = stepZ / dz;
            Vector3 curr = new Vector3(x, y, z);
            while (true)
            {
                curr.X = x;
                curr.Y = y;
                curr.Z = z;
                float len = (curr - end).Length();
                yield return new Point3(FloorInt(x), FloorInt(y), FloorInt(z));
                if (FloorInt(x) == FloorInt(end.X) && FloorInt(y) == FloorInt(end.Y) && FloorInt(z) == FloorInt(end.Z)) yield break;
                if (len > d1 * 1.1f) yield break;
                // tMaxX stores the t-value at which we cross a cube boundary along the
                // X axis, and similarly for Y and Z. Therefore, choosing the least tMax
                // chooses the closest cube boundary. Only the first case of the four
                // has been commented in detail.
                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        // Update which cube we are now in.
                        x += stepX;
                        // Adjust tMaxX to the next X-oriented boundary crossing.
                        tMaxX += tDeltaX;
                    }
                    else
                    {
                        z += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        y += stepY;
                        tMaxY += tDeltaY;
                    }
                    else
                    {
                        // Identical to the second case, repeated for simplicity in
                        // the conditionals.
                        z += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }
            }
        }


    }
}
