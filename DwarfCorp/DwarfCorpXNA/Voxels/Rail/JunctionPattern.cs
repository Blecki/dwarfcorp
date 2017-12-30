using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Rail
{
    public class JunctionPattern
    {
        public String Name;
        public List<RailPiece> Pieces;

        public JunctionPattern Rotate()
        {
            return new JunctionPattern
            {
                Pieces = Pieces.Select(piece =>
                    new RailPiece
                    {
                        Offset = new Point(piece.Offset.Y, -piece.Offset.X),
                        Decal = piece.Decal,
                        Orientation = Orientation.Rotate(piece.Orientation, 1)
                    }
                ).ToList()
            };
        }
    }
}
