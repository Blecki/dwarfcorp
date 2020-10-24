using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using DwarfCorp.Gui.Widgets;

namespace DwarfCorp.Play
{
    public class HorizontalGridPanel : Widget
    {
        public Point ItemSize = new Point(32, 32);
        public Point ItemSpacing = new Point(4, 4);
        public bool EnableScrolling = true;

        private HorizontalScrollBar ScrollBar = null;

        public override void Construct()
        {
            base.Construct();

            if (EnableScrolling)
                ScrollBar = AddChild(new HorizontalScrollBar
                {
                    AutoLayout = AutoLayout.DockBottom,
                    OnScrollValueChanged = (sender) =>
                    {
                        sender.Parent.Layout();
                    }
                }) as HorizontalScrollBar;
        }

        public void ClearContents()
        {
            if (EnableScrolling)
            {
                var scrollBar = Children[0];
                Children.Clear();
                Children.Add(scrollBar);
            }
            else
                Children.Clear();
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

            // Find possible columns.
            var totalItems = EnableScrolling ? Children.Count - 1 : Children.Count;
            var itemsDown = VisibleItems(ItemSize.Y, ItemSpacing.Y, inside.Height);
            var visibleColumns = VisibleItems(ItemSize.X, ItemSpacing.X, inside.Width);

            if (itemsDown <= 0) itemsDown = 1;
            if (visibleColumns <= 0) visibleColumns = 1;

            var columns = totalItems / itemsDown;
            if (totalItems % itemsDown != 0)
                columns += 1;

            if (EnableScrolling)
            {
                ScrollBar.SupressOnScroll = true; // Prevents infinite loop.
                ScrollBar.ScrollArea = columns - visibleColumns + 1;
            }

            var itemsThisColumn = 0;
            var columnsAdded = 0;
            var currentItem = EnableScrolling ? (ScrollBar.ScrollPosition * itemsDown) + 1 : 0;
            if (currentItem >= 2)
            {
                var x = 5;

            }

            var pos = new Point(inside.X, inside.Y);

            for (var i = EnableScrolling ? 1 : 0; i < Children.Count; ++i)
                GetChild(i).Hidden = true;

            for (; currentItem < Children.Count && (columnsAdded < visibleColumns); ++currentItem)
            {
                var child = GetChild(currentItem);
                child.Hidden = false;
                child.Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                child.Layout();

                pos.Y += ItemSize.Y + ItemSpacing.Y;
                itemsThisColumn += 1;
                if (itemsThisColumn >= itemsDown)
                {
                    itemsThisColumn = 0;
                    columnsAdded += 1;

                    pos.Y = inside.Y;
                    pos.X += ItemSize.X + ItemSpacing.X;
                }
            }

            Invalidate();
        }
    }
}
