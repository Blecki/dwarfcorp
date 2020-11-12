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
            Rect = new Rectangle(0, 0, 450, 400);

            Border = "border-fancy";

            Text = Message == null || String.IsNullOrEmpty(Message.Title) ? "Tutorial" : Message.Title;
            Font = "font16";
            TextColor = new Vector4(0, 0, 0, 1);
            InteriorMargin = new Margin(16, 0, 0, 0);

            if (!String.IsNullOrEmpty(Message.Name) && AssetManager.DoesTextureExist("newgui\\tutorials\\" + Message.Name))
            {
                var asset = "newgui\\tutorials\\" + Message.Name;
                AddChild(new GameStates.TutorialIcon()
                {
                    ImageSource = asset,
                    MinimumSize = new Point(256, 128),
                    MaximumSize = new Point(256, 128),
                    AutoLayout = AutoLayout.DockTop
                });
                Rect = new Rectangle(0, 0, 450, 450);
                InteriorMargin = new Margin(32, 0, 0, 0);
            }
            else if (Message.Icon != null)
            {
                AddChild(new Play.ResourceGuiGraphicIcon()
                {
                    Resource = Message.Icon,
                    MinimumSize = new Point(128, 128),
                    MaximumSize = new Point(128, 128),
                    AutoLayout = AutoLayout.DockTop
                });
                Rect = new Rectangle(0, 0, 450, 450);
                InteriorMargin = new Margin(32, 0, 0, 0);
            }

            AddChild(new Button
            {
                Text = "Dismiss",
                Font = "font10",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) => { this.Close(); SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_window_close, 0.015f); },
                AutoLayout = AutoLayout.FloatBottomRight,
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1)
            });

            DisableBox = AddChild(new Gui.Widgets.CheckBox
            {
                Text = "Disable tutorial",
                ChangeColorOnHover = true,
                Font = "font10",
                AutoLayout = AutoLayout.FloatBottomLeft,
                TextColor = new Vector4(0,0,0,1)
            }) as Gui.Widgets.CheckBox;

            AddChild(new Widget
            {
                Text = Message == null ? "" : "\n" + Message.Text,
                Font = "aliasedFont20",
                AutoLayout = AutoLayout.DockTop,
                TextColor = new Vector4(0, 0, 0, 1)
            });

            Layout();
        }
    }
}
