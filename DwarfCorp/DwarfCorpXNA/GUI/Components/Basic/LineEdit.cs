// LineEdit.cs
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
    /// This GUI component allows simple user text input on a single line.
    /// </summary>
    public class LineEdit : GUIComponent
    {
        public delegate void Modified(string arg);

        public event Modified OnTextModified;
        public string Prompt { get; set; }
        public string Text { get; set; }
        public bool HasKeyboardFocus { get; set; }
        public int Carat { get; set; }
        public bool IsEditable { get; set; }

        public Mode TextMode { get; set; }
        public string Prefix { get; set; }

        public enum Mode
        {
            Text,
            Numeric
        }

        public LineEdit(DwarfGUI gui, GUIComponent parent, string text) :
            base(gui, parent)
        {
            HasKeyboardFocus = false;
            Text = text;
            InputManager.MouseClickedCallback += InputManager_MouseClickedCallback;
            InputManager.KeyPressedCallback += InputManager_KeyPressedCallback;
            Carat = text.Length;
            OnTextModified += LineEdit_OnTextModified;
            IsEditable = true;
            Prompt = "";
            TextMode = Mode.Text;
            Prefix = "";
        }

        private void LineEdit_OnTextModified(string arg)
        {
        }

        private void InputManager_KeyPressedCallback(Microsoft.Xna.Framework.Input.Keys key)
        {
            if(HasKeyboardFocus && IsEditable)
            {
                if(key == Keys.Back || key == Keys.Delete)
                {
                    if(Text.Length > 0 && Carat > 0)
                    {
                        Carat = MathFunctions.Clamp(Carat - 1, 0, Text.Length);
                        Text = Text.Remove(Carat, 1);
                    }
                }
                else if (key == Keys.Left)
                {
                    Carat = MathFunctions.Clamp(Carat - 1, 0, Text.Length);
                }
                else if (key == Keys.Right)
                {
                    Carat = MathFunctions.Clamp(Carat + 1, 0, Text.Length);
                }
                else if (TextMode == Mode.Numeric && (key == Keys.Up || key == Keys.OemPlus))
                {
                    int value;
                    if (int.TryParse(Text, out value))
                    {
                        value++;
                        Text = value.ToString("D");
                    }
                }
                else if (TextMode == Mode.Numeric && (key == Keys.Down || key == Keys.OemMinus))
                {
                    int value;
                    if (int.TryParse(Text, out value))
                    {
                        value--;
                        Text = value.ToString("D");
                    }
                }
                else
                {
                    char k = ' ';
                    if(InputManager.TryConvertKeyboardInput(key, Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift), out k))
                    {
                        if (TextMode != Mode.Text && !char.IsDigit(k)) return;

                        if (TextMode == Mode.Numeric && Text == "0")
                        {
                            Text = "";
                            Carat = 0;
                        }

                        Carat = MathFunctions.Clamp(Carat, 0, Text.Length);
                        Text = Text.Insert(Carat, k.ToString());
                        Carat = MathFunctions.Clamp(Carat + 1, 0, Text.Length);
                    }
                }

                if (TextMode == Mode.Numeric && string.IsNullOrEmpty(Text))
                {
                    Text = "0";
                    Carat = Text.Length;
                }

                OnTextModified.Invoke(Text);
            }
        }

        private void InputManager_MouseClickedCallback(InputManager.MouseButton button)
        {
            if(IsMouseOver && IsEditable)
            {
                HasKeyboardFocus = true;

                if (!HasKeyboardFocus)
                {
                    Carat = Text.Length;
                }
            }
            else
            {
                HasKeyboardFocus = false;
            }
        }

        public override void Update(DwarfTime time)
        {
            if (!HasKeyboardFocus)
            {
                Carat = Text.Length;
            }
            base.Update(time);
        }

        private string GetSubstringToShow()
        {
            Vector2 measure = Datastructures.SafeMeasure(GUI.DefaultFont, Text);

            if(measure.X < GlobalBounds.Width - 15)
            {
                return Text;
            }
            else
            {
                for(int i = 0; i < Text.Length; i++)
                {
                    string subtext = Text.Substring(i);
                    measure = Datastructures.SafeMeasure(GUI.DefaultFont, subtext);

                    if(measure.X < GlobalBounds.Width - 15)
                    {
                        return "..." + subtext;
                    }
                }
            }

            return Text;
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            Rectangle fieldRect = new Rectangle(GlobalBounds.X, GlobalBounds.Y + GlobalBounds.Height / 2 - GUI.Skin.TileHeight / 2, GlobalBounds.Width, GUI.Skin.TileHeight);
            Rectangle textRect = new Rectangle(GlobalBounds.X + 5, GlobalBounds.Y + GlobalBounds.Height / 2 - GUI.Skin.TileHeight / 2, GlobalBounds.Width, GUI.Skin.TileHeight);
            GUI.Skin.RenderField(fieldRect, batch);


            if (string.IsNullOrEmpty(Text) && !HasKeyboardFocus)
            {
                Drawer2D.DrawAlignedText(batch, " " + Prompt, GUI.DefaultFont, Color.Brown, Drawer2D.Alignment.Left, textRect);
            }
            else
            {
                string toShow = GetSubstringToShow();
                Carat = MathFunctions.Clamp(Carat, 0, toShow.Length);
                string first = toShow.Substring(0, Carat);
                string last = toShow.Substring(Carat, toShow.Length - Carat);

                if (!HasKeyboardFocus)
                {
                    Drawer2D.DrawAlignedText(batch, " " + Prefix + toShow, GUI.DefaultFont, GUI.DefaultTextColor, Drawer2D.Alignment.Left, textRect);
                }
                else
                {
                    if (time.TotalRealTime.TotalMilliseconds % 1000 < 500)
                    {
                        Drawer2D.DrawAlignedText(batch, " " + Prefix + first + "|" + last, GUI.DefaultFont, GUI.DefaultTextColor, Drawer2D.Alignment.Left, textRect);
                    }
                    else
                    {
                        Drawer2D.DrawAlignedText(batch, " " + Prefix + first + " " + last, GUI.DefaultFont, GUI.DefaultTextColor, Drawer2D.Alignment.Left, textRect);
                    }
                }   
            }

            base.Render(time, batch);
        }
    }

}