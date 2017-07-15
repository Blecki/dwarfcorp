using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static IEnumerable<GlobalVoxelCoordinate> EnumerateCoordinatesInBoundingBox(BoundingBox Box)
        {
            for (var x = (int)Math.Floor(Box.Min.X); x < (int)Math.Ceiling(Box.Max.X); ++x)
                for (var y = (int)Math.Floor(Box.Min.Y); y < (int)Math.Ceiling(Box.Max.Y); ++y)
                    for (var z = (int)Math.Floor(Box.Min.Z); z < (int)Math.Ceiling(Box.Max.Z); ++z)
                        yield return new GlobalVoxelCoordinate(x, y, z);
        }
    }
}
