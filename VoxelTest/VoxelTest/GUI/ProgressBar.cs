using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    public class ProgressBar : SillyGUIComponent
    {
        public float Value { get; set; }

        public ProgressBar(SillyGUI gui, SillyGUIComponent parent, float v) :
            base(gui, parent)
        {
            Value = v;
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            Rectangle rectToDraw = new Rectangle(GlobalBounds.X, GlobalBounds.Y + GlobalBounds.Height / 2 - GUI.Skin.TileHeight / 2, GlobalBounds.Width, GUI.Skin.TileHeight);
            GUI.Skin.RenderProgressBar(rectToDraw, Value, batch);
            base.Render(time, batch);
        }
    }

}