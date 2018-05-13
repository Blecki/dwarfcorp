using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class AutoGridPanel : Widget
    {
        public int Rows = 2;
        public int Columns = 4;

        public override void Layout()
        {
            Root.SafeCall(this.OnLayout, this);
            var area = GetDrawableInterior().Interior(InteriorMargin);
            var pos = new Point(area.X, area.Y);
            var itemSize = new Point(area.Width / Columns, area.Height / Rows);
            foreach (var child in EnumerateChildren())
            {
                child.Rect = new Rectangle(pos.X, pos.Y, itemSize.X, itemSize.Y);
                pos.X += itemSize.X;
                if (pos.X > area.Right - itemSize.X)
                {
                    var leftOver = area.Right - pos.X;
                    leftOver += itemSize.X;

                    pos.X = area.X;
                    pos.Y += itemSize.Y;
                }
                child.Layout();
            }
            Invalidate();
        }
    }
}
