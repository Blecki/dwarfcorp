// KeyEditor.cs
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

using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    ///     This GUI component allows the player to edit the keyboard settings.
    /// </summary>
    public class KeyEditor : GUIComponent
    {
        public KeyEditor(DwarfGUI gui, GUIComponent parent, KeyManager keyManager, int numRows, int numColumns) :
            base(gui, parent)
        {
            Keys[] reserved =
            {
                Keys.Up,
                Keys.Left,
                Keys.Right,
                Keys.Down,
                Keys.LeftControl,
                Keys.LeftShift,
                Keys.RightShift,
                Keys.LeftAlt,
                Keys.RightAlt,
                Keys.RightControl,
                Keys.Escape
            };
            ReservedKeys = new List<Keys>();
            ReservedKeys.AddRange(reserved);

            KeyManager = keyManager;

            Layout = new GridLayout(gui, this, numRows, numColumns*2);

            int r = 0;
            int c = 0;

            foreach (var button in KeyManager.Buttons)
            {
                if (r == numRows)
                {
                    r = 0;
                    c++;
                }

                var keyLabel = new Label(gui, Layout, button.Key, gui.DefaultFont);
                var editor = new KeyEdit(gui, Layout, button.Value);
                Layout.SetComponentPosition(keyLabel, c*2, r, 1, 1);
                Layout.SetComponentPosition(editor, c*2 + 1, r, 1, 1);


                string name = button.Key;

                editor.OnKeyModified += (prevKey, arg, keyedit) => editor_OnKeyModified(name, prevKey, arg, keyedit);

                r++;
            }
        }

        public KeyManager KeyManager { get; set; }
        public GridLayout Layout { get; set; }
        public List<Keys> ReservedKeys { get; set; }

        public bool IsReserved(Keys key)
        {
            return ReservedKeys.Contains(key);
        }

        public void UpdateLayout()
        {
            Layout.UpdateSizes();
        }


        private void editor_OnKeyModified(string name, Keys prevKey, Keys arg, KeyEdit editor)
        {
            if (!KeyManager.IsMapped(arg))
            {
                KeyManager[name] = arg;
                KeyManager.SaveConfigSettings();
            }
            else
            {
                editor.Key = prevKey;
                editor.Text = prevKey.ToString();
                Dialog.Popup(GUI, "Key assigned!", "Key " + arg + " already assigned.", Dialog.ButtonType.OK);
            }
        }
    }
}