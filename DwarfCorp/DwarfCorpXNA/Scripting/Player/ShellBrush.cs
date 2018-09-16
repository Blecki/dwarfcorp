using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class ShellBrush : IVoxelBrush
    {
        public bool CullUnseenVoxels { get { return true; } }

        public IEnumerable<GlobalVoxelCoordinate> Select(BoundingBox Bounds, Vector3 Start, Vector3 End, bool Invert)
        {
            int minX = MathFunctions.FloorInt(Bounds.Min.X + 0.5f);
            int minY = MathFunctions.FloorInt(Bounds.Min.Y + 0.5f);
            int minZ = MathFunctions.FloorInt(Bounds.Min.Z + 0.5f);
            int maxX = MathFunctions.FloorInt(Bounds.Max.X - 0.5f);
            int maxY = MathFunctions.FloorInt(Bounds.Max.Y - 0.5f);
            int maxZ = MathFunctions.FloorInt(Bounds.Max.Z - 0.5f);

            for (var y = minY; y <= maxY; y++)
            {
                // yx planes
                for (var z = minZ; z <= maxZ; z++)
                {
                    yield return new GlobalVoxelCoordinate(minX, y, z);
                    yield return new GlobalVoxelCoordinate(maxX, y, z);
                }
                // yz planes
                for (var x = minX + 1; x < maxX; x++)
                {
                    yield return new GlobalVoxelCoordinate(x, y, minZ);
                    yield return new GlobalVoxelCoordinate(x, y, maxZ);
                }
            }
        }
    }
}
