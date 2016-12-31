// ColorPanel.cs
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
    /// <summary>
    /// This is a panel which merely displays a color.
    /// </summary>
    /// <seealso cref="GUIComponent" />
    public class ColorPanel : GUIComponent
    {
        public Color BorderColor = Color.Black;

        public int BorderWidth = 1;
        public Color CurrentColor = Color.White;

        public ColorPanel(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            Drawer2D.FillRect(batch, GlobalBounds, CurrentColor);
            if (BorderWidth > 0)
            {
                Drawer2D.DrawRect(batch, GlobalBounds, BorderColor, BorderWidth);
            }

            if (IsMouseOver)
            {
                var highlightColor = new Color(255 - CurrentColor.R, 255 - CurrentColor.G, 255 - CurrentColor.B);
                Drawer2D.DrawRect(batch, GlobalBounds, highlightColor, BorderWidth*2 + 1);
            }

            base.Render(time, batch);
        }
    }
}