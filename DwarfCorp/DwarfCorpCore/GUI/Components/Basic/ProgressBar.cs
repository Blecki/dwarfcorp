// ProgressBar.cs
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
    ///     This GUI component draws a filled bar specifying a certain value.
    /// </summary>
    public class ProgressBar : GUIComponent
    {
        public ProgressBar(DwarfGUI gui, GUIComponent parent, float v) :
            base(gui, parent)
        {
            Value = v;
            Tint = Color.Lime;
            Message = "";
            MessageColor = Color.White;
            MessageStroke = Color.Black;
        }

        public float Value { get; set; }
        public Color Tint { get; set; }
        public string Message { get; set; }
        public Color MessageColor { get; set; }
        public Color MessageStroke { get; set; }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            if (!IsVisible) return;
            var rectToDraw = new Rectangle(GlobalBounds.X,
                GlobalBounds.Y + GlobalBounds.Height/2 - GUI.Skin.TileHeight/2, GlobalBounds.Width, GUI.Skin.TileHeight);
            GUI.Skin.RenderProgressBar(rectToDraw, Value, Tint, batch);
            Drawer2D.DrawAlignedStrokedText(batch, Message, GUI.DefaultFont, MessageColor, MessageStroke,
                Drawer2D.Alignment.Center, GlobalBounds);
            base.Render(time, batch);
        }

        public override bool IsMouseOverRecursive()
        {
            if (!IsVisible) return false;
            return base.IsMouseOverRecursive();
        }
    }
}