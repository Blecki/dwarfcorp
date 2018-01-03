using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Rail
{
    public class JunctionPiece
    {
        public Point Offset;
        public String RailPiece;
        public Orientations Orientation;
    }

    public class JunctionPattern
    {
        public String Name;
        public List<JunctionPiece> Pieces;

        public JunctionPattern Rotate()
        {
            return new JunctionPattern
            {
                Pieces = Pieces.Select(piece =>
                    new JunctionPiece
                    {
                        Offset = new Point(piece.Offset.Y, -piece.Offset.X),
                        RailPiece = piece.RailPiece,
                        Orientation = Orientation.Rotate(piece.Orientation, 1)
                    }
                ).ToList()
            };
        }

        public JunctionPattern Rotate(Orientations Orientation)
        {
            var orient = (byte)Orientation;
            var r = this;
            while (orient > 0)
            {
                r = r.Rotate();
                orient -= 1;
            }
            return r;
        }
    }
}
