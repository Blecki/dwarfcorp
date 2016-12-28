// GroupBox.cs
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
    ///     This GUI component is a labeled panel which contains other components.
    /// </summary>
    public class GroupBox : GUIComponent
    {
        public GroupBox(DwarfGUI gui, GUIComponent parent, string title) :
            base(gui, parent)
        {
            Title = title;
            DrawBounds = true;
        }

        public string Title { get; set; }
        public bool DrawBounds { get; set; }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            if (!IsVisible)
            {
                return;
            }

            if (DrawBounds)
            {
                GUI.Skin.RenderGroup(GlobalBounds, batch);
            }
            Drawer2D.DrawAlignedText(batch, Title, GUI.DefaultFont, Color.Black,
                Drawer2D.Alignment.Top | Drawer2D.Alignment.Left, GlobalBounds);
            base.Render(time, batch);
        }
    }
}