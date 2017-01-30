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
        public Point ItemSpacing = new Point(8, 8);

        private GridPanel Panel = null;
        
        public IEnumerable<Widget> ItemSource;

        public Scale9Corners Corners = Scale9Corners.All;

        public override void Construct()
        {
            Border = "tray-border";
            InteriorMargin = new Margin(-32, -32, -32, -32);
            if (Corners.HasFlag(Scale9Corners.Top)) InteriorMargin.Top = -16;
            if (Corners.HasFlag(Scale9Corners.Bottom)) InteriorMargin.Bottom = -16;
            if (Corners.HasFlag(Scale9Corners.Left)) InteriorMargin.Left = -16;
            if (Corners.HasFlag(Scale9Corners.Right)) InteriorMargin.Right = -16;
                       
            Panel = AddChild(new GridPanel
                {
                    AutoLayout = Gum.AutoLayout.DockFill,
                    ItemSize = ItemSize,
                    ItemSpacing = ItemSpacing
                }) as GridPanel;

            foreach (var item in ItemSource)
                Panel.AddChild(item);
            
            Layout();
        }

        protected override Gum.Mesh Redraw()
        {
            return Gum.Mesh.CreateScale9Background(Rect, Root.GetTileSheet(Border), Corners);
        }
    }
}
