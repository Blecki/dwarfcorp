using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class GridPanel : Widget
    {
        public Point ItemSize = new Point(32, 32);
        public Point ItemSpacing = new Point(4, 4);
        public bool EnableScrolling = true;
        public bool OverflowBottom = false;

        private VerticalScrollBar ScrollBar = null;

        public override void Construct()
        {
            base.Construct();

            if (EnableScrolling)
                ScrollBar = AddChild(new VerticalScrollBar
                {
                    AutoLayout = AutoLayout.DockRight,
                    OnScrollValueChanged = (sender) =>
                    {
                        sender.Parent.Layout();
                    }
                }) as VerticalScrollBar;
        }

        private int VisibleItems(int Size, int Spacing, int AxisLength)
        {
            var r = AxisLength / (Size + Spacing);
            if ((r * (Size + Spacing)) + Size <= AxisLength)
                r += 1;
            return r;
        }

        public override void Layout()
        {
            Root.SafeCall(this.OnLayout, this); // ...What is it gonna do??

            var inside = this.GetDrawableInterior().Interior(InteriorMargin);
            if (EnableScrolling) inside = Widget.LayoutChild(inside, Margin.Zero, ScrollBar);

            // Find possible rows.
            var totalItems = EnableScrolling ? Children.Count - 1 : Children.Count;
            var itemsAcross = VisibleItems(ItemSize.X, ItemSpacing.X, inside.Width);
            var visibleRows = VisibleItems(ItemSize.Y, ItemSpacing.Y, inside.Height);

            if (itemsAcross <= 0) return;
            if (!OverflowBottom && visibleRows <= 0) return;

            var rows = totalItems / itemsAcross;
            if (totalItems % itemsAcross != 0)
                rows += 1;

            if (EnableScrolling)
            {
                ScrollBar.SupressOnScroll = true; // Prevents infinite loop.
                ScrollBar.ScrollArea = rows - visibleRows + 1;
            }

            var itemsThisRow = 0;
            var rowsAdded = 0;
            var currentItem = EnableScrolling ? (ScrollBar.ScrollPosition * itemsAcross) + 1 : 0;
            if (currentItem >= 2)
            {
                var x = 5;

            }

            var pos = new Point(inside.X, inside.Y);

            for (var i = EnableScrolling ? 1 : 0; i < Children.Count; ++i)
                GetChild(i).Hidden = true;

            for (; currentItem < Children.Count && (OverflowBottom || rowsAdded < visibleRows); ++currentItem)
            {
                var child = GetChild(currentItem);
                child.Hidden = false;
                child.Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                child.Layout();

                pos.X += ItemSize.X + ItemSpacing.X;
                itemsThisRow += 1;
                if (itemsThisRow >= itemsAcross)
                {
                    itemsThisRow = 0;
                    rowsAdded += 1;

                    pos.X = inside.X;
                    pos.Y += ItemSize.Y + ItemSpacing.Y;
                }
            }

            Invalidate();
        }
    }
}
