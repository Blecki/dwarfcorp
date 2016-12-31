// ColorKey.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// This is a legend for a map which maps names to colors on the map.
    /// </summary>
    /// <seealso cref="GUIComponent" />
    public class ColorKey : GUIComponent
    {
        public ColorKey(DwarfGUI gui, GUIComponent parent)
            : base(gui, parent)
        {
            Font = gui.SmallFont;
            BackgroundColor = Color.White;
            TextColor = Color.Black;
            BorderColor = Color.Black;
            ColorEntries = new Dictionary<string, Color>();
        }

        public SpriteFont Font { get; set; }

        public Dictionary<string, Color> ColorEntries { get; set; }

        public Color BackgroundColor { get; set; }
        public Color TextColor { get; set; }
        public Color BorderColor { get; set; }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            AutoSize();
            Drawer2D.FillRect(batch, GlobalBounds, BackgroundColor);
            Drawer2D.DrawRect(batch, GlobalBounds, BorderColor, 2);


            int currHeight = 8;

            foreach (var entry in ColorEntries)
            {
                Vector2 measure = Font.MeasureString(entry.Key);
                Drawer2D.DrawAlignedText(batch, entry.Key, Font, TextColor, Drawer2D.Alignment.Right,
                    new Rectangle(GlobalBounds.X + 18, GlobalBounds.Y + currHeight, LocalBounds.Width - 20,
                        (int) measure.Y));
                Drawer2D.FillRect(batch, new Rectangle(GlobalBounds.X + 2, GlobalBounds.Y + currHeight - 5, 10, 10),
                    entry.Value);
                Drawer2D.DrawRect(batch, new Rectangle(GlobalBounds.X + 2, GlobalBounds.Y + currHeight - 5, 10, 10),
                    BorderColor, 1);
                currHeight += (int) (measure.Y + 5);
            }


            base.Render(time, batch);
        }

        public void AutoSize()
        {
            int sumHeight = 0;
            int maxWidth = 0;

            foreach (Vector2 measure in ColorEntries.Select(entry => Font.MeasureString(entry.Key)))
            {
                sumHeight += (int) measure.Y + 5;
                maxWidth = (int) Math.Max(measure.X, maxWidth);
            }

            LocalBounds = new Rectangle(LocalBounds.X, LocalBounds.Y, maxWidth + 19, sumHeight + 10);
        }
    }
}