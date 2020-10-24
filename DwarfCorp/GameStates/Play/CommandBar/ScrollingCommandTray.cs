using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using DwarfCorp.Gui.Widgets;

namespace DwarfCorp.Play
{
    public class ScrollingCommandTray : Widget
    {
        public Point ItemSize = new Point(32, 32);
        public Point ItemSpacing = new Point(4, 4);
        public int ScrollPosition;

        public override void Construct()
        {
            base.Construct();
        }

        public void ClearContents()
        {
             Children.Clear();
        }

        private int VisibleItems(int Size, int Spacing, int AxisLength)
        {
            var r = AxisLength / (Size + Spacing);
            if ((r * (Size + Spacing)) + Size <= AxisLength)
                r += 1;
            return r;
        }

        public int GetItemsVisible()
        {
            var visibleColumns = VisibleItems(ItemSize.X, ItemSpacing.X, GetDrawableInterior().Interior(InteriorMargin).Width);
            if (visibleColumns <= 0) return 1;
            return visibleColumns;
        }

        public override void Layout()
        {
            Root.SafeCall(this.OnLayout, this); // ...What is it gonna do??

            var inside = this.GetDrawableInterior().Interior(InteriorMargin);

            // Find possible columns.
            var totalItems = Children.Count;
            var visibleColumns = VisibleItems(ItemSize.X, ItemSpacing.X, inside.Width);

            if (visibleColumns <= 0) visibleColumns = 1;

            var columnsAdded = 0;
            var currentItem = ScrollPosition;

            var pos = new Point(inside.X, inside.Y);

            for (var i = 0; i < Children.Count; ++i)
                GetChild(i).Hidden = true;

            for (; currentItem < Children.Count && (columnsAdded < visibleColumns); ++currentItem)
            {
                var child = GetChild(currentItem);
                child.Hidden = false;
                child.Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                child.Layout();

                columnsAdded += 1;
                pos.X += ItemSize.X + ItemSpacing.X;
            }

            Invalidate();
        }
    }
}
