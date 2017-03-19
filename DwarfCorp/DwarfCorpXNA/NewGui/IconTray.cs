using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class IconTray : Widget
    {
        public Point ItemSize = new Point(40, 40);
        public Point ItemSpacing = new Point(2, 2);
        public Point SizeToGrid = new Point(1, 1);

        public IEnumerable<Widget> ItemSource;

        public Scale9Corners Corners = Scale9Corners.All;

        public IconTray()
        {
            Border = "tray-border";
        }

        public override Rectangle GetDrawableInterior()
        {
            // Don't account for border.
            return Rect.Interior(InteriorMargin);
        }

        public override void Construct()
        {
            if (ItemSource == null)
            {
                ItemSource = new List<Widget>();
            }
            InteriorMargin = new Margin(0,0,0,0);
            if (Corners.HasFlag(Scale9Corners.Top)) InteriorMargin.Top = 12;
            if (Corners.HasFlag(Scale9Corners.Bottom)) InteriorMargin.Bottom = 12;
            if (Corners.HasFlag(Scale9Corners.Left)) InteriorMargin.Left = 16;
            if (Corners.HasFlag(Scale9Corners.Right)) InteriorMargin.Right = 16;

            Padding = new Margin(0, 0, 0, 0);

            // Calculate perfect size. Margins + item sizes + padding.
            MaximumSize.X = InteriorMargin.Left + InteriorMargin.Right + (SizeToGrid.X * ItemSize.X) + ((SizeToGrid.X - 1) * ItemSpacing.X);
            MaximumSize.Y = InteriorMargin.Top + InteriorMargin.Bottom + (SizeToGrid.Y * ItemSize.Y) + ((SizeToGrid.Y - 1) * ItemSpacing.Y);
            MinimumSize = MaximumSize;

            Rect.Width = MinimumSize.X;
            Rect.Height = MinimumSize.Y;

            foreach (var item in ItemSource)
                AddChild(item);
        }

        public override void Layout()
        {
            Root.SafeCall(OnLayout, this);
            var rect = GetDrawableInterior();

            var pos = new Point(rect.X, rect.Y);
            foreach (var child in EnumerateChildren())
            {
                child.Rect = new Rectangle(pos.X, pos.Y, ItemSize.X, ItemSize.Y);
                pos.X += ItemSize.X + ItemSpacing.X;
                if (pos.X > rect.Right - ItemSize.X)
                {
                    pos.X = rect.X;
                    pos.Y += ItemSize.Y + ItemSpacing.Y;
                }
                child.Layout();
            }

            Invalidate();   
        }

        protected override Gum.Mesh Redraw()
        {
            if (Border != null)
            {
                return Gum.Mesh.CreateScale9Background(Rect, Root.GetTileSheet(Border), Corners);
            }
            else
            {
                return base.Redraw();
            }
        }
    }
}
