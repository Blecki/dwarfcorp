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
        /// Rasterizes the line, producing a list of GlobalVoxelCoordinates's that intersect
        /// the line segment.
        /// </summary>
        /// <param name="Start">The start.</param>
        /// <param name="End">The end.</param>
        /// <returns></returns>
        public static IEnumerable<GlobalVoxelCoordinate> FastVoxelTraversal(Vector3 Start, Vector3 End)
        {
            if (L1(Start, End) < 1e-12 || HasNan(Start) || HasNan(End))
                yield break;

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

            Vector3 direction = End - Start;
            var cutoffLength = direction.LengthSquared() * 1.01f;
            direction.Normalize();
            
            // Direction to increment x,y,z when stepping.
            var stepX = Math.Sign(direction.X);
            var stepY = Math.Sign(direction.Y);
            var stepZ = Math.Sign(direction.Z);
            
            // See description above. The initial values depend on the fractional
            // part of the origin.
            var tMaxX = IntBound(Start.X, direction.X);
            var tMaxY = IntBound(Start.Y, direction.Y);
            var tMaxZ = IntBound(Start.Z, direction.Z);
            
            // The change in t when taking a step (always positive).
            var tDeltaX = stepX / direction.X;
            var tDeltaY = stepY / direction.Y;
            var tDeltaZ = stepZ / direction.Z;

            var endX = FloorInt(End.X);
            var endY = FloorInt(End.Y);
            var endZ = FloorInt(End.Z);

            while (true)
            {
                var r = GlobalVoxelCoordinate.FromVector3(Start);
                yield return r;

                if (r.X == endX && r.Y == endY && r.Z == endZ) yield break;
                if ((End - Start).LengthSquared() > cutoffLength) yield break;

                // tMaxX stores the t-value at which we cross a cube boundary along the
                // X axis, and similarly for Y and Z. Therefore, choosing the least tMax
                // chooses the closest cube boundary. Only the first case of the four
                // has been commented in detail.
                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        // Update which cube we are now in.
                        Start.X += stepX;
                        // Adjust tMaxX to the next X-oriented boundary crossing.
                        tMaxX += tDeltaX;
                    }
                    else
                    {
                        Start.Z += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        Start.Y += stepY;
                        tMaxY += tDeltaY;
                    }
                    else
                    {
                        // Identical to the second case, repeated for simplicity in
                        // the conditionals.
                        Start.Z += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }
            }
        }


    }
}
