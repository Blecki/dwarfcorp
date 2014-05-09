using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// This is a simple GUI component which draws a fancy rectangle thing.
    /// </summary>
    public class Panel : GUIComponent
    {
        public Panel(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            if(!IsVisible)
            {
                return;
            }

            GUI.Skin.RenderPanel(GlobalBounds, batch);
            base.Render(time, batch);
        }
    }

}