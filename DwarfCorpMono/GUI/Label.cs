using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class Label : SillyGUIComponent
    {
        public string Text { get; set; }
        public Color TextColor { get; set; }
        public Color StrokeColor { get; set;}
        public SpriteFont TextFont { get; set; }
        public Drawer2D.Alignment Alignment { get; set; }

        public Label(SillyGUI gui, SillyGUIComponent parent, string text, SpriteFont textFont) :
            base(gui, parent)
        {
            Text = text;
            TextColor = gui.DefaultTextColor;
            StrokeColor = gui.DefaultStrokeColor;
            TextFont = textFont;
            Alignment = Drawer2D.Alignment.Left;
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            Drawer2D.DrawAlignedStrokedText(batch, Text, TextFont, TextColor, StrokeColor, Alignment, GlobalBounds);
            base.Render(time, batch);
        }
    }
}
