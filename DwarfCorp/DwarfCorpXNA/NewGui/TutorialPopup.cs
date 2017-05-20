using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class TutorialPopup : Widget
    {
        public String Message = "";
        private Gum.Widgets.CheckBox DisableBox;
        public bool DisableChecked { get { return DisableBox.CheckState; } }

        public override void Construct()
        {
            //Set size and center on screen.
            Rect = new Rectangle(0, 0, 350, 150);

            Border = "border-fancy";

            Text = "Tutorial";
            Font = "font-hires";
            InteriorMargin = new Margin(20, 0, 0, 0);

            AddChild(new Widget
            {
                Text = "Dismiss",
                Font = "font",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) => this.Close(),
                AutoLayout = AutoLayout.FloatBottomRight
            });

            DisableBox = AddChild(new Gum.Widgets.CheckBox
            {
                Text = "Disable tutorial",
                Font = "font",
                AutoLayout = AutoLayout.FloatBottomLeft
            }) as Gum.Widgets.CheckBox;

            AddChild(new Widget
            {
                Text = Message,
                Font = "font",
                AutoLayout = AutoLayout.DockFill,
                OnLayout = (sender) => sender.Rect.Height -= 20
            });

            Layout();
        }
    }
}
