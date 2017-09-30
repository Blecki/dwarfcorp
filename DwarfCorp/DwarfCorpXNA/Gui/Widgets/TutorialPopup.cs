using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Tutorial;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class TutorialPopup : Widget
    {
        public TutorialManager.TutorialEntry Message = null;
        private Gui.Widgets.CheckBox DisableBox;
        public bool DisableChecked { get { return DisableBox.CheckState; } }

        public override void Construct()
        {
            //Set size and center on screen.
            Rect = new Rectangle(0, 0, 450, 350);

            Border = "border-fancy";

            Text = Message == null || String.IsNullOrEmpty(Message.Title) ? "Tutorial" : Message.Title;
            Font = "font16";
            InteriorMargin = new Margin(20, 0, 0, 0);

            AddChild(new Button
            {
                Text = "Dismiss",
                Font = "font14",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) => { this.Close(); SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_window_close, 0.25f); },
                AutoLayout = AutoLayout.FloatBottomRight,
            });

            DisableBox = AddChild(new Gui.Widgets.CheckBox
            {
                Text = "Disable tutorial",
                Font = "font8",
                AutoLayout = AutoLayout.FloatBottomLeft
            }) as Gui.Widgets.CheckBox;

            AddChild(new Widget
            {
                Text = Message == null ? "" : Message.Text,
                Font = "font14",
                AutoLayout = AutoLayout.DockFill,
                OnLayout = (sender) => sender.Rect.Height -= 20
            });

            Layout();
        }
    }
}
