using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class BoxBrush : IVoxelBrush
    {
        public bool CullUnseenVoxels { get { return true; } }

        public IEnumerable<GlobalVoxelCoordinate> Select(BoundingBox Bounds, Vector3 Start, Vector3 End, bool Invert)
        {
            return VoxelHelpers.EnumerateCoordinatesInBoundingBox(Bounds);
        }
    }
}
