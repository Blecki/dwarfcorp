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
    /// This GUI component takes input and displays a specific keyboard Key.
    /// </summary>
    public class KeyEdit : GUIComponent
    {
        public delegate void Modified(string arg);

        public event Modified OnTextModified;

        public delegate void KeyModified(Keys prevKey, Keys arg, KeyEdit editor);

        public event KeyModified OnKeyModified;

        public string Text { get; set; }
        public Keys Key { get; set; }
        public bool HasKeyboardFocus { get; set; }
        public int Carat { get; set; }
        public bool IsEditable { get; set; }

        public KeyEdit(DwarfGUI gui, GUIComponent parent, Keys key) :
            base(gui, parent)
        {
            Key = key;
            HasKeyboardFocus = false;
            Text = key.ToString();
            InputManager.MouseClickedCallback += InputManager_MouseClickedCallback;
            InputManager.KeyPressedCallback += InputManager_KeyPressedCallback;
            Carat = 0;
            OnTextModified += LineEdit_OnTextModified;
            OnKeyModified += KeyEdit_OnKeyModified;
            IsEditable = true;
        }

        private void KeyEdit_OnKeyModified(Keys prevKey, Keys arg, KeyEdit editor)
        {
        }

        private void LineEdit_OnTextModified(string arg)
        {
        }

        private void InputManager_KeyPressedCallback(Microsoft.Xna.Framework.Input.Keys key)
        {
            if(!HasKeyboardFocus || !IsEditable)
            {
                return;
            }
            Keys prevKey = Key;
            Key = key;
            Text = key.ToString();
            OnTextModified.Invoke(Text);
            OnKeyModified.Invoke(prevKey, Key, this);
        }

        private void InputManager_MouseClickedCallback(InputManager.MouseButton button)
        {
            if(IsMouseOver && IsEditable)
            {
                HasKeyboardFocus = true;
            }
            else
            {
                HasKeyboardFocus = false;
            }
        }

        public override void Update(DwarfTime time)
        {
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

            string substring = GetSubstringToShow();

            if(!HasKeyboardFocus)
            {
                Drawer2D.DrawAlignedText(batch, substring, GUI.DefaultFont, GUI.DefaultTextColor, Drawer2D.Alignment.Left, textRect);
            }
            else
            {
                if(time.TotalGameTime.TotalMilliseconds % 1000 < 500)
                {
                    Drawer2D.DrawAlignedText(batch, substring + "|", GUI.DefaultFont, GUI.DefaultTextColor, Drawer2D.Alignment.Left, textRect);
                }
                else
                {
                    Drawer2D.DrawAlignedText(batch, substring, GUI.DefaultFont, GUI.DefaultTextColor, Drawer2D.Alignment.Left, textRect);
                }
            }

            base.Render(time, batch);
        }
    }

}