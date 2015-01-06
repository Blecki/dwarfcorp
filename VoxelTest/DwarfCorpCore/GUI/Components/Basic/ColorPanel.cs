using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class ColorPanel : GUIComponent
    {
        public Color CurrentColor = Microsoft.Xna.Framework.Color.White;
        public Color BorderColor = Microsoft.Xna.Framework.Color.Black;
        
        public int BorderWidth = 1;

        public ColorPanel(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            
        }

        public override void Render(DwarfTime time, Microsoft.Xna.Framework.Graphics.SpriteBatch batch)
        {
            Drawer2D.FillRect(batch, GlobalBounds, CurrentColor);
            if (BorderWidth > 0)
            {
                Drawer2D.DrawRect(batch, GlobalBounds, BorderColor, BorderWidth);
            }

            if (IsMouseOver)
            {
                Color highlightColor = new Color(255 - CurrentColor.R, 255 - CurrentColor.G, 255 - CurrentColor.B);
                Drawer2D.DrawRect(batch, GlobalBounds, highlightColor, BorderWidth * 2 + 1);
            }

            base.Render(time, batch);
        }
    }
}
