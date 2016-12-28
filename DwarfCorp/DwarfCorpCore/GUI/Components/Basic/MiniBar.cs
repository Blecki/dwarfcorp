// MiniBar.cs
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class MiniBar : GUIComponent
    {
        public MiniBar(DwarfGUI gui, GUIComponent parent, float v, string label)
            : base(gui, parent)
        {
            Value = v;
            BackgroundColor = Color.Transparent;
            ForegroundColor = Color.Black;
            FillColor = new Color(10, 10, 10);
            Text = new Label(GUI, this, label, GUI.SmallFont)
            {
                LocalBounds = new Rectangle(0, 0, label.Length*8, 32)
            };
        }

        public float Value { get; set; }
        public Label Text { get; set; }

        public Color BackgroundColor { get; set; }
        public Color ForegroundColor { get; set; }
        public Color FillColor { get; set; }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            int width = GlobalBounds.Width;
            int height = Text.GlobalBounds.Height - 5;
            var renderBounds = new Rectangle(Text.GlobalBounds.X, GlobalBounds.Y + 32, (int) (width*Value), height);
            var maxBounds = new Rectangle(Text.GlobalBounds.X + 1, GlobalBounds.Y + 32 + 1, width - 1, height - 1);
            Drawer2D.FillRect(batch, renderBounds, FillColor);
            Drawer2D.DrawRect(batch, maxBounds, ForegroundColor, 1);
            base.Render(time, batch);
        }
    }
}