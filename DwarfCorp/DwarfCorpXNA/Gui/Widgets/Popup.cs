using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class Popup : Widget
    {
        public string OkayText = "OKAY";

        public override void Construct()
        {
            //Set size and center on screen.
            Rect = new Rectangle(0, 0, Math.Max(256, MinimumSize.X), Math.Max(128, MinimumSize.Y));
            Rect.X = (Root.RenderData.VirtualScreen.Width / 2) - Rect.Width / 2;
            Rect.Y = (Root.RenderData.VirtualScreen.Height / 2) - Rect.Height / 2;
            WrapText = true;

            Border = "border-fancy";

            AddChild(new Widget
            {
                Text = OkayText,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) => this.Close(),
                AutoLayout = AutoLayout.FloatBottomRight
            });

            Layout();
        }
    }
}
