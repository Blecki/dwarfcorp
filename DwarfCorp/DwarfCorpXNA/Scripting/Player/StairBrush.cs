using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class StairBrush : IVoxelBrush
    {
        public bool CullUnseenVoxels { get { return false; } }

        public IEnumerable<GlobalVoxelCoordinate> Select(BoundingBox Bounds, Vector3 Start, Vector3 End, bool Invert)
        {
            // Todo: Can this be simplified to return voxels above or below the line?
            int minX = MathFunctions.FloorInt(Bounds.Min.X + 0.5f);
            int minY = MathFunctions.FloorInt(Bounds.Min.Y + 0.5f);
            int minZ = MathFunctions.FloorInt(Bounds.Min.Z + 0.5f);
            int maxX = MathFunctions.FloorInt(Bounds.Max.X - 0.5f);
            int maxY = MathFunctions.FloorInt(Bounds.Max.Y - 0.5f);
            int maxZ = MathFunctions.FloorInt(Bounds.Max.Z - 0.5f);

            // If not inverted, selects the Xs
            // If inverted, selects the Os
            //max y ----xOOOO
            //      --- xxOOO
            //      --- xxxOO
            //      --- xxxxO
            //min y --- xxxxx
            //        minx --- maxx
            float dx = Bounds.Max.X - Bounds.Min.X;
            float dz = Bounds.Max.Z - Bounds.Min.Z;
            Vector3 dir = End - Start;
            bool direction = dx > dz;
            bool positiveDir = direction ? dir.X < 0 : dir.Z < 0;
            int step = 0;

            // Always make staircases go exactly to the top or bottom of the selection.
            if (direction && Invert)
            {
                minY = maxY - (maxX - minX);
            }
            else if (direction)
            {
                maxY = minY + (maxX - minX);
            }
            else if (Invert)
            {
                minY = maxY - (maxZ - minZ);
            }
            else
            {
                maxY = minY + (maxZ - minZ);
            }
            int dy = maxY - minY;
            // Start from the bottom of the stairs up to the top.
            for (int y = minY; y <= maxY; y++)
            {
                int carve = Invert ? MathFunctions.Clamp(dy - step, 0, dy) : step;
                // If stairs are in x direction
                if (direction)
                {
                    if (positiveDir)
                    {
                        // Start from min x, and march up to maxY - y
                        for (int x = minX; x <= MathFunctions.Clamp(maxX - carve, minX, maxX); x++)
                        {
                            for (int z = minZ; z <= maxZ; z++)
                            {
                                yield return new GlobalVoxelCoordinate(x, y, z);
                            }
                        }
                    }
                    else
                    {
                        // Start from min x, and march up to maxY - y
                        for (int x = maxX; x >= MathFunctions.Clamp(minX + carve, minX, maxX); x--)
                        {
                            for (int z = minZ; z <= maxZ; z++)
                            {
                                yield return new GlobalVoxelCoordinate(x, y, z);
                            }
                        }
                    }
                    step++;
                }
                // Otherwise, they are in the z direction.
                else
                {
                    if (positiveDir)
                    {
                        // Start from min z, and march up to maxY - y
                        for (int z = minZ; z <= MathFunctions.Clamp(maxZ - carve, minZ, maxZ); z++)
                        {
                            for (int x = minX; x <= maxX; x++)
                            {
                                yield return new GlobalVoxelCoordinate(x, y, z);
                            }
                        }
                    }
                    else
                    {
                        // Start from min z, and march up to maxY - y
                        for (int z = maxZ; z >= MathFunctions.Clamp(minZ + carve, minZ, maxZ); z--)
                        {
                            for (int x = minX; x <= maxX; x++)
                            {
                                yield return new GlobalVoxelCoordinate(x, y, z);
                            }
                        }
                    }
                    step++;
                }
            }
        }
    }
}
