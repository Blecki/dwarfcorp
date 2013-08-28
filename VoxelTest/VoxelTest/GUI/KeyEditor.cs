using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class KeyEditor :  SillyGUIComponent
    {
        public KeyManager KeyManager { get; set; }
        public GridLayout Layout { get; set; }

        public void UpdateLayout()
        {
            Layout.UpdateSizes();
        }


        public KeyEditor(SillyGUI gui, SillyGUIComponent parent, KeyManager keyManager, int numRows, int numColumns) :
            base(gui, parent)
        {
            KeyManager = keyManager;

            Layout = new GridLayout(gui, this, numRows, numColumns * 2);

            int r = 0;
            int c = 0;

            foreach (KeyValuePair<string, Keys> button in KeyManager.Buttons)
            {
                if (r == numRows)
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

        void editor_OnKeyModified(string name, Keys arg)
        {
            KeyManager[name] = arg;
            KeyManager.SaveConfigSettings();
        }

        

    }
}
