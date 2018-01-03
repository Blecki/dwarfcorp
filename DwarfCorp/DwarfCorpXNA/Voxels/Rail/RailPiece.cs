using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Rail
{
    public class RailConversionEntry
    {
        public DecalOrientation ThisOrientation;
        public String OverlayDecal;
        public DecalOrientation OverlayOrientation;
        public String ResultDecal;
        public DecalOrientation ResultOrientation;
    }

    public class RailPiece
    {
        public String Name;
        public Point Tile;
        public List<RailConversionEntry> ConversionTable;

    }
}
