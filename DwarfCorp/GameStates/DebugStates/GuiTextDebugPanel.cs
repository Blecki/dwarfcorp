using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.GameStates.Debug
{
    public class GuiTextDebugPanel : Gui.Widget
    {
        public override void Construct()
        {
            AddChild(new Gui.Widgets.EditableTextField
            {
                MinimumSize = new Point(0, 24),
                AutoLayout = AutoLayout.DockTop,
                Font = "font16"
            });
        }
    }
}
