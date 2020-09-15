using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class StaticGridPanel : Widget
    {
        public int Rows = 2;
        public int Columns = 4;

        public class Panel
        {
            public String Name;
            public int X;
            public int Y;
            public int ColSpan = 1;
            public int RowSpan = 1;
        }

        public List<Panel> Panels = new List<Panel>();

        public override void Layout()
        {
            Root.SafeCall(this.OnLayout, this);
            var area = GetDrawableInterior().Interior(InteriorMargin);
            var itemSize = new Point(area.Width / Columns, area.Height / Rows);
            var itemIndex = 0;
            foreach (var panel in Panels)
            {
                var child = Children.FirstOrDefault(c => c.Tag is String str && str == panel.Name);
                if (child == null) continue;

                var rect = new Rectangle(panel.X * itemSize.X, panel.Y * itemSize.Y, panel.ColSpan * itemSize.X, panel.RowSpan * itemSize.Y);
                child.Rect = rect;
                child.Layout();

                itemIndex += 1;
            }
        }
    }
}
