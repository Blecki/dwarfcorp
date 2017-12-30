using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Rail
{
    public class RailPiece
    {
        /// <summary>
        /// Offset from the origin of the junction pattern.
        /// </summary>
        public Point Offset;

        public String Decal;

        /// <summary>
        /// Orientation of this piece in junction pattern.
        /// </summary>
        public Orientations Orientation;
    }
}
