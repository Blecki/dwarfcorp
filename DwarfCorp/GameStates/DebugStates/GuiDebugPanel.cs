
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.GameStates.Debug
{
    public class GuiDebugPanel : Gui.Widget
    {
        public override void Construct()
        {
            Background = new TileReference("basic", 1);
            BackgroundColor = new Vector4(0, 0, 0, 1);
        }

        protected override Mesh Redraw()
        {
            return Mesh.Merge(base.Redraw(), Mesh.Quad().Scale(Rect.Width, Rect.Height).Translate(Rect.X, Rect.Y));
        }
    }
}
