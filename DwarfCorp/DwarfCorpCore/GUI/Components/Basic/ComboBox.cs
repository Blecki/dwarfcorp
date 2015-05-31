// ComboBox.cs
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

namespace DwarfCorp
{
    /// <summary>
    /// This GUI component has a list of items which can be selected on
    /// a drop down menu.
    /// </summary>
    public class ComboBox : GUIComponent
    {
        public List<string> Values { get; set; }
        public string CurrentValue { get; set; }
        public int CurrentIndex { get; set; }
        public bool IsOpen { get; set; }
        public ComboBoxSelector Selector { get; set; }
        public event ComboBoxSelector.Modified OnSelectionModified;

        public ComboBox(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
            Values = new List<string>();
            CurrentValue = "";
            OnLeftClicked += ComboBox_OnLeftPressed;
            Selector = null;
            OnSelectionModified += ComboBox_OnSelectionModified;
        }

        private void ComboBox_OnSelectionModified(string arg)
        {
            if(GUI.FocusComponent == Selector)
            {
                GUI.FocusComponent = null;
            }
        }

        public void InvokeSelectionModified()
        {
            OnSelectionModified.Invoke(CurrentValue);
        }

        private void ComboBox_OnLeftPressed()
        {
            if(Selector == null)
            {
                Rectangle fieldRect = new Rectangle(GlobalBounds.X, GlobalBounds.Y + GlobalBounds.Height / 2 - GUI.Skin.TileHeight / 2, GlobalBounds.Width, 32);
                if(fieldRect.Contains(Mouse.GetState().X, Mouse.GetState().Y))
                {
                    Selector = new ComboBoxSelector(GUI, this, Values, CurrentValue);
                    GUI.FocusComponent = Selector;
                    Selector.OnSelectionModified += Selector_OnSelectionModified;
                }
            }
        }

        private void Selector_OnSelectionModified(string value)
        {
            CurrentIndex = Selector.GetCurrentIndex();
            OnSelectionModified.Invoke(value);
        }

        public bool HasValue(string value)
        {
            return Values.Contains(value);
        }

        public void AddValue(string value)
        {
            Values.Add(value);
        }

        public void RemoveValue(string value)
        {
            Values.Remove(value);

            if(CurrentValue == value)
            {
                CurrentValue = "";
                CurrentIndex = 0;
            }
        }

        public override void Update(DwarfTime time)
        {
            if (string.IsNullOrEmpty(CurrentValue))
            {
                if(Selector != null)
                    Selector.CurrentValue = Values[CurrentIndex];
                CurrentValue = Values[CurrentIndex];
            }
            base.Update(time);
        }

        public override void Render(DwarfTime time, Microsoft.Xna.Framework.Graphics.SpriteBatch batch)
        {
            Rectangle fieldRect = new Rectangle(GlobalBounds.X, GlobalBounds.Y + GlobalBounds.Height / 2 - GUI.Skin.TileHeight / 2, GlobalBounds.Width - 37, 32);
            GUI.Skin.RenderField(fieldRect, batch);
            Drawer2D.DrawAlignedText(batch, CurrentValue, GUI.DefaultFont, Color.Black, Drawer2D.Alignment.Center, fieldRect);
            GUI.Skin.RenderDownArrow(new Rectangle(GlobalBounds.X + GlobalBounds.Width - 32, GlobalBounds.Y + GlobalBounds.Height / 2 - GUI.Skin.TileHeight / 2, 32, 32), batch);
            base.Render(time, batch);
        }

        public void ClearValues()
        {
            Values.Clear();
        }
    }

}