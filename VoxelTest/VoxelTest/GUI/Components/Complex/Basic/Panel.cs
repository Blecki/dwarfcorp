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
        public enum PanelMode
        {
            Fancy,
            Simple
        }

        public PanelMode Mode { get; set; }

        public Panel(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            Mode = PanelMode.Fancy;
            
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            if(!IsVisible)
            {
                return;
            }

            if(Mode == PanelMode.Fancy)
            {
                GUI.Skin.RenderPanel(GlobalBounds, batch);
            }
            else
            {
                GUI.Skin.RenderToolTip(GlobalBounds, batch, new Color(255, 255, 255, 150));
            }
            base.Render(time, batch);
        }
    }

}