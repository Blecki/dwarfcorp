using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class TrayBackground : Widget
    {
        public Scale9Corners Corners = Scale9Corners.All;

        public TrayBackground()
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
            InteriorMargin = new Margin(0, 0, 0, 0);
            if (Corners.HasFlag(Scale9Corners.Top)) InteriorMargin.Top = 12;
            if (Corners.HasFlag(Scale9Corners.Bottom)) InteriorMargin.Bottom = 12;
            if (Corners.HasFlag(Scale9Corners.Left)) InteriorMargin.Left = 16;
            if (Corners.HasFlag(Scale9Corners.Right)) InteriorMargin.Right = 16;

            Padding = new Margin(0, 0, 0, 0);
        }
        
        protected override Gui.Mesh Redraw()
        {
            if (Border != null)
            {
                return Gui.Mesh.CreateScale9Background(Rect, Root.GetTileSheet(Border), Corners);
            }
            else
            {
                return base.Redraw();
            }
        }
    }
}
