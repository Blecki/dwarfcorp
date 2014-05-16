using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class Panel : SillyGUIComponent
    {
        public Panel(SillyGUI gui, SillyGUIComponent parent) :
            base(gui, parent)
        {

        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            if (!IsVisible)
            {
                return;
            }

            GUI.Skin.RenderPanel(GlobalBounds, batch);
            base.Render(time, batch);
        }

        
    }
}
