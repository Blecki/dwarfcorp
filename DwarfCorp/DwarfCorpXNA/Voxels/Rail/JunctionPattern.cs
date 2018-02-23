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
        public Orientation Orientation;
    }

    public enum JunctionPaintMode
    {
        SingleInstance,
        Path
    }

    public class JunctionPortal
    {
        public Point Offset;
        public Orientation Direction;
    }

    public class JunctionPattern
    {
        public String Name;
        public List<JunctionPiece> Pieces;
        public int Icon;
        public JunctionPaintMode PaintMode = JunctionPaintMode.SingleInstance;
        public JunctionPortal Entrance = null;
        public JunctionPortal Exit = null;

        public JunctionPattern Rotate()
        {
            return new JunctionPattern
            {
                Pieces = Pieces.Select(piece =>
                    new JunctionPiece
                    {
                        Offset = new Point(piece.Offset.Y, -piece.Offset.X),
                        RailPiece = piece.RailPiece,
                        Orientation = OrientationHelper.Rotate(piece.Orientation, 1)
                    }
                ).ToList(),
                Entrance = Entrance == null ? null :
                    new JunctionPortal
                    {
                        Offset = new Point(Entrance.Offset.Y, -Entrance.Offset.X),
                        Direction = OrientationHelper.Rotate(Entrance.Direction, 1)
                    },
                Exit = Exit == null ? null :
                    new JunctionPortal
                    {
                        Offset = new Point(Exit.Offset.Y, -Exit.Offset.X),
                        Direction = OrientationHelper.Rotate(Exit.Direction, 1)
                    },
                PaintMode = PaintMode
            };
        }

        public JunctionPattern Rotate(Orientation Orientation)
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
