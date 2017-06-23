using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui
{
    public static class RectangleExtension
    {
        public static Rectangle Interior(this Rectangle r, int Left, int Top, int Right, int Bottom)
        {
            return new Microsoft.Xna.Framework.Rectangle(r.X + Left, r.Y + Top,
                r.Width - Left - Right, r.Height - Top - Bottom);
        }

        public static Point Atleast(this Point r, Point dim)
        {
            if (r.X < dim.X) r.X = dim.X;
            if (r.Y < dim.Y) r.Y = dim.Y;
            return r;
        }

        public static Rectangle Interior(this Rectangle R, Margin M)
        {
            return R.Interior(M.Left, M.Top, M.Right, M.Bottom);
        }

        public static Point Scale(this Point p, int S)
        {
            return new Point(p.X * S, p.Y * S);
        }
    }
}


