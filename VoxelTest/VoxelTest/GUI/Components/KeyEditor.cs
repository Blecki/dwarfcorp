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
    /// This GUI component allows the player to edit the keyboard settings.
    /// </summary>
    public class KeyEditor : GUIComponent
    {
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

            Layout = new GridLayout(gui, this, numRows, numColumns * 2);

            int r = 0;
            int c = 0;

            foreach(KeyValuePair<string, Keys> button in KeyManager.Buttons)
            {
                if(r == numRows)
                {
                    r = 0;
                    c++;
                }

                Label keyLabel = new Label(gui, Layout, button.Key, gui.DefaultFont);
                KeyEdit editor = new KeyEdit(gui, Layout, button.Value);
                Layout.SetComponentPosition(keyLabel, c * 2, r, 1, 1);
                Layout.SetComponentPosition(editor, c * 2 + 1, r, 1, 1);


                string name = button.Key;

                editor.OnKeyModified += delegate(Keys arg) { editor_OnKeyModified(name, arg); };

                r++;
            }
        }

        private void editor_OnKeyModified(string name, Keys arg)
        {

            KeyManager[name] = arg;
            KeyManager.SaveConfigSettings();

            /*
            if(!IsReserved(arg))
            {
                KeyManager[name] = arg;
                KeyManager.SaveConfigSettings();
            }
             */

        }
    }

}