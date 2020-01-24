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
        }
    }
}
