using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.GameStates.Debug
{
    public class GuiDebugPanel : Gui.Widget
    {
        public bool IncludeCloseButton = false;

        public override void Construct()
        {
            var tabs = AddChild(new Gui.Widgets.TabPanel
            {
                AutoLayout = AutoLayout.DockFill,
                ButtonFont = "font8"
            }) as TabPanel;

            tabs.AddTab("Texture", new GuiTextureDebugPanel
            {
            });

            tabs.AddTab("TEXT", new GuiTextDebugPanel { });

            if (IncludeCloseButton)
                AddChild(new Widget
                {
                    Text = "Exit",
                    Border = "border-button",
                    AutoLayout = AutoLayout.FloatBottomRight,
                    OnClick = (sender, args) => this.Close(),
                    OnLayout = (sender) =>
                    {
                        sender.Rect = new Rectangle(tabs.Rect.Right - 128, tabs.Rect.Bottom - 64, 128, 64);                  
                    },
                    TextSize = 2,
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Center
                });
        }
    }
}
