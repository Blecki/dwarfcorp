using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace DwarfCorp
{
    public class ColorKey : GUIComponent
    {

        public SpriteFont Font { get; set; }

        public Dictionary<string, Color> ColorEntries { get; set; }

        public Color BackgroundColor { get; set; }
        public Color TextColor { get; set; }
        public Color BorderColor { get; set; }

        public ColorKey(DwarfGUI gui, GUIComponent parent) 
            : base(gui, parent)
        {
            Font = gui.SmallFont;
            BackgroundColor = Color.White;
            TextColor = Color.Black;
            BorderColor = Color.Black;
            ColorEntries = new Dictionary<string, Color>();
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            AutoSize();
            Drawer2D.FillRect(batch, GlobalBounds, BackgroundColor);
            Drawer2D.DrawRect(batch, GlobalBounds, BorderColor, 2);


            int currHeight = 8;
            
            foreach(var entry in ColorEntries)
            {
                Vector2 measure = Font.MeasureString(entry.Key);
                Drawer2D.DrawAlignedText(batch, entry.Key, Font, TextColor, Drawer2D.Alignment.Right, new Rectangle(GlobalBounds.X + 18, GlobalBounds.Y + currHeight, LocalBounds.Width - 20, (int)measure.Y));
                Drawer2D.FillRect(batch, new Rectangle(GlobalBounds.X + 2, GlobalBounds.Y + currHeight, 15, 15), entry.Value);
                Drawer2D.DrawRect(batch, new Rectangle(GlobalBounds.X + 2, GlobalBounds.Y + currHeight, 15, 15), BorderColor, 1);
                currHeight += (int)(measure.Y + 1);
                
            }


            base.Render(time, batch);
        }

        public void AutoSize()
        {
            int sumHeight = 0;
            int maxWidth = 0;

            foreach(Vector2 measure in ColorEntries.Select(entry => Font.MeasureString(entry.Key)))
            {
                sumHeight += (int) measure.Y;
                maxWidth = (int)Math.Max(measure.X, maxWidth);
            }

            LocalBounds = new Rectangle(LocalBounds.X, LocalBounds.Y, maxWidth + 19, sumHeight + 10);
        }
    }
}
