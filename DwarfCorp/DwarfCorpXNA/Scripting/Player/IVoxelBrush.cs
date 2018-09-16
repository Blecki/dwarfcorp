using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DwarfCorp
{
    public interface IVoxelBrush
    {
        IEnumerable<GlobalVoxelCoordinate> Select(BoundingBox Bounds, Vector3 Start, Vector3 End, bool Invert);
        bool CullUnseenVoxels { get; }
    }

    public class VoxelBrushes
    {
        public static StairBrush StairBrush = new StairBrush();
        public static ShellBrush ShellBrush = new ShellBrush();
        public static BoxBrush BoxBrush = new BoxBrush();
    }
}
