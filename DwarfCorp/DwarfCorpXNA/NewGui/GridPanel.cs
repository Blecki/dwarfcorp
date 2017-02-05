using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class GridPanel : Widget
    {
        public Point ItemSize = new Point(32, 32);
        public Point ItemSpacing = new Point(4, 4);

        public override void Layout()
        {
            Root.SafeCall(this.OnLayout, this);
            var pos = new Point(Rect.X, Rect.Y);
            foreach (var child in EnumerateChildren())
            {
                child.Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                pos.X += ItemSize.X + ItemSpacing.X;
                if (pos.X > Rect.Right - ItemSize.X)
                {
                    pos.X = Rect.X;
                    pos.Y += ItemSize.Y + ItemSpacing.Y;
                }
                child.Layout();
            }
            Invalidate();
        }
    }
}
