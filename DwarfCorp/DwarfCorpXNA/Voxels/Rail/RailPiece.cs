using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Rail
{
    public class RailCombination
    {
        public String Overlay;
        public Orientation OverlayRelativeOrientation;

        public String Result;
        public Orientation ResultRelativeOrientation;
    }

    public enum RailShape
    {
        Flat,
        TopHalfSlope,
        BottomHalfSlope
    }

    public class RailPiece
    {
        public String Name = "";
        public RailShape Shape = RailShape.Flat;
        public Point Tile = Point.Zero;
        public List<RailCombination> CombinationTable = new List<RailCombination>();
        public List<List<Vector3>> SplinePoints = new List<List<Vector3>>();
    }
}
