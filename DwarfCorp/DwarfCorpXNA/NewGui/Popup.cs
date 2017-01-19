using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class Popup : Widget
    {
        public string OkayText = "OKAY";

        public override void Construct()
        {
            //Set size and center on screen.
            Rect = new Rectangle(0, 0, 256, 128);
            Rect.X = (Root.VirtualScreen.Width / 2) - 128;
            Rect.Y = (Root.VirtualScreen.Height / 2) - 32;

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
