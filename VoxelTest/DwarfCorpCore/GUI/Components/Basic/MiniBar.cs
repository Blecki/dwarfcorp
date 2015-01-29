using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class MiniBar : GUIComponent
    {
        public float Value { get; set; }
        public Label Text { get; set; }
        
        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }
        public Color FillColor { get; set; }

        public MiniBar(DwarfGUI gui, GUIComponent parent, float v, string label) 
            : base(gui, parent)
        {
            Value = v;
            BackgroundColor = Color.Transparent;
            ForegroundColor = Color.Black;
            FillColor = new Color(10, 10, 10);
            Text = new Label(GUI, this, label, GUI.SmallFont)
            {
                LocalBounds = new Rectangle(0, 0, label.Length * 8, 32)
            };
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            int width = GlobalBounds.Width;
            int height = Text.GlobalBounds.Height - 5;
            Rectangle renderBounds = new Rectangle(Text.GlobalBounds.X, GlobalBounds.Y + 32, (int)(width * Value), height);
            Rectangle maxBounds = new Rectangle(Text.GlobalBounds.X + 1, GlobalBounds.Y + 32 + 1, width - 1, height - 1);
            Drawer2D.FillRect(batch, renderBounds, FillColor);
            Drawer2D.DrawRect(batch, maxBounds, ForegroundColor, 1);
            base.Render(time, batch);
        }
    }
}
