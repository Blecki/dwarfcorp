// Checkbox.cs
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
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    /// <summary>
    /// This componenet, when clicked, toggles on and off. It also has a 
    /// label drawn next to it.
    /// </summary>
    public class Checkbox : GUIComponent
    {
        public string Text { get; set; }
        public Color TextColor { get; set; }
        public Color StrokeColor { get; set; }
        public Color HoverTextColor { get; set; }
        public SpriteFont TextFont { get; set; }
        public bool Checked { get; set; }

        public delegate void CheckModified(bool arg);

        public event CheckModified OnCheckModified;

        public Checkbox(DwarfGUI gui, GUIComponent parent, string text, SpriteFont textFont, bool check) :
            base(gui, parent)
        {
            Text = text;
            TextColor = gui.DefaultTextColor;
            TextFont = textFont;
            OnClicked += Clicked;
            StrokeColor = new Color(0, 0, 0, 0);
            Checked = check;
            HoverTextColor = Color.DarkRed;
            OnCheckModified += CheckBox_OnCheckModified;
        }

        private void CheckBox_OnCheckModified(bool arg)
        {
        }


        public void Clicked()
        {
            Checked = !Checked;
            OnCheckModified.Invoke(Checked);
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            Rectangle globalBounds = GlobalBounds;

            Color c = TextColor;

            if(IsMouseOver)
            {
                c = HoverTextColor;
            }

            Rectangle checkboxBounds = new Rectangle(GlobalBounds.Right - 32, GlobalBounds.Top + 1, 32, 32);

            GUI.Skin.RenderCheckbox(checkboxBounds, Checked, batch);
            Vector2 measure = Datastructures.SafeMeasure(GUI.DefaultFont, Text);


            Drawer2D.DrawStrokedText(batch, Text,
                GUI.DefaultFont,
                new Vector2(GlobalBounds.Right - measure.X - 32, GlobalBounds.Top + 5),
                c, StrokeColor);


            base.Render(time, batch);
        }
    }

}