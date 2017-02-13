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

        private GridPanel Panel = null;
        
        public IEnumerable<Widget> ItemSource;

        public Scale9Corners Corners = Scale9Corners.All;

        public override Rectangle GetDrawableInterior()
        {
            // Don't account for border.
            return Rect.Interior(InteriorMargin);
        }

        public override void Construct()
        {
            Border = "tray-border";
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

            Panel = AddChild(new GridPanel
                {
                    AutoLayout = Gum.AutoLayout.DockFill,
                    ItemSize = ItemSize,
                    ItemSpacing = ItemSpacing
                }) as GridPanel;

            foreach (var item in ItemSource)
                Panel.AddChild(item);
        }

        public override void Layout()
        {
            Root.SafeCall(OnLayout, this);
            Panel.Rect = GetDrawableInterior();
            Panel.Layout();
            Invalidate();   
        }

        protected override Gum.Mesh Redraw()
        {
            return Gum.Mesh.CreateScale9Background(Rect, Root.GetTileSheet(Border), Corners);
        }
    }
}
