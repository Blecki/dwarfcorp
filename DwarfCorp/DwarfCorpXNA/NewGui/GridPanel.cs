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
            var area = GetDrawableInterior();
            var pos = new Point(area.X, area.Y);
            foreach (var child in EnumerateChildren())
            {
                child.Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                pos.X += ItemSize.X + ItemSpacing.X;
                if (pos.X > area.Right - ItemSize.X)
                {
                    pos.X = area.X;
                    pos.Y += ItemSize.Y + ItemSpacing.Y;
                }
                child.Layout();
            }
            Invalidate();
        }

        public Point ItemsThatFit
        {
            get
            {
                return new Point(GetDrawableInterior().Width / (ItemSize.X + ItemSpacing.X),
                    GetDrawableInterior().Height / (ItemSize.Y + ItemSpacing.Y));
            }
        }
    }
}
