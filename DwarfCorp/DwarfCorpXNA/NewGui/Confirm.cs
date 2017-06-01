using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class Confirm : Widget
    {
        public enum Result
        {
            OKAY,
            CANCEL
        }

        public Result DialogResult = Result.CANCEL;
        public string OkayText = "Okay";
        public string CancelText = "Cancel";

        public override void Construct()
        {
            //Set size and center on screen.
            Rect = new Rectangle(0, 0, 256, 128);
            Rect.X = (Root.RenderData.VirtualScreen.Width / 2) - 128;
            Rect.Y = (Root.RenderData.VirtualScreen.Height / 2) - 32;

            Border = "border-fancy";

            if (!String.IsNullOrEmpty(OkayText))
            {
                AddChild(new Gum.Widgets.Button
                {
                    Text = OkayText,
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Center,
                    Border = "border-button",
                    OnClick = (sender, args) =>
                    {
                        DialogResult = Result.OKAY;
                        this.Close();
                    },
                    AutoLayout = AutoLayout.FloatBottomRight
                });
            }

            if (!String.IsNullOrEmpty(CancelText))
            {
                AddChild(new Gum.Widgets.Button
                {
                    Text = CancelText,
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Center,
                    Border = "border-button",
                    OnClick = (sender, args) =>
                    {
                        DialogResult = Result.CANCEL;
                        this.Close();
                    },
                    AutoLayout = AutoLayout.FloatBottomLeft
                });
            }

            Layout();
        }
    }
}
