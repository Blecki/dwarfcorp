// Dialog.cs
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

namespace DwarfCorp
{
    /// <summary>
    /// This GUI component is a window which opens up on top
    /// of the GUI, and blocks the game until it gets user input.
    /// </summary>
    public class Dialog : Window
    {
        public enum ReturnStatus
        {
            None,
            Ok,
            Canceled
        }

        public enum ButtonType
        {
            None,
            OK,
            Cancel,
            OkAndCancel
        }

        public bool IsModal { get; set; }
        private bool isClosed = false;

        public delegate void Closed(ReturnStatus status);

        public event Closed OnClosed;

        public Label Title { get; set; }
        public Label Message { get; set; }

        public GridLayout Layout { get; set; }

        public static Dialog Popup(DwarfGUI gui, string title, string message, ButtonType buttons, int w, int h, GUIComponent parent, int x, int y)
        {
            Dialog d = new Dialog(gui, parent)
            {
                LocalBounds =
                    new Rectangle(x, y, w, h),
                MinWidth =  w - 150,
                MinHeight = h - 150
            };

            d.Initialize(buttons, title, message);

            return d;
        }

        public static Dialog Popup(DwarfGUI gui, string title, string message, ButtonType buttons)
        {
            int w = message.Length * 8 + 150;
            int h = 150;
            int x = gui.Graphics.Viewport.Width/2 - w/2;
            int y = gui.Graphics.Viewport.Height / 2 - h / 2;
            return Popup(gui, title, message, buttons, w, h, gui.RootComponent, x, y);
        }

        public 
            Dialog(DwarfGUI gui, GUIComponent parent) :
            base(gui, parent)
        {
           
        }


        public virtual void Initialize(ButtonType buttons, string title, string message)
        {
            IsModal = true;
            OnClicked += Dialog_OnClicked;
            OnClosed += Dialog_OnClosed;
            
            Layout = new GridLayout(GUI, this, 4, 4);
            Title = new Label(GUI, Layout, title, GUI.DefaultFont);
            Layout.SetComponentPosition(Title, 0, 0, 1, 1);

            Message = new Label(GUI, Layout, message, GUI.DefaultFont)
            {
                WordWrap = true
            };
            Layout.SetComponentPosition(Message, 0, 1, 4, 2);


            bool createOK = false;
            bool createCancel = false;

            switch (buttons)
            {
                case ButtonType.None:
                    break;
                case ButtonType.OkAndCancel:
                    createOK = true;
                    createCancel = true;
                    break;
                case ButtonType.OK:
                    createOK = true;
                    break;
                case ButtonType.Cancel:
                    createCancel = true;
                    break;
            }

            if (createOK)
            {
                Button okButton = new Button(GUI, Layout, "OK", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.Check));
                Layout.SetComponentPosition(okButton, 2, 3, 2, 1);
                okButton.OnClicked += OKButton_OnClicked;
            }

            if (createCancel)
            {
                Button cancelButton = new Button(GUI, Layout, "Cancel", GUI.DefaultFont, Button.ButtonMode.PushButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.Ex));
                Layout.SetComponentPosition(cancelButton, 0, 3, 2, 1);
                cancelButton.OnClicked += cancelButton_OnClicked;
            }
        }
      
        private void cancelButton_OnClicked()
        {
            Close(ReturnStatus.Canceled);
        }

        private void OKButton_OnClicked()
        {
            Close(ReturnStatus.Ok);
        }

        private void Dialog_OnClosed(Dialog.ReturnStatus status)
        {
            // nothing
        }

        private void Dialog_OnClicked()
        {
            if(IsMouseOver)
            {
                GUI.FocusComponent = this;
            }
            else if(!IsModal)
            {
                if(GUI.FocusComponent == this)
                {
                    GUI.FocusComponent = null;
                }
            }
        }


        public virtual void Close(ReturnStatus status)
        {
            if(GUI.FocusComponent == this)
            {
                GUI.FocusComponent = null;
            }

            isClosed = true;
            IsVisible = false;

            OnClosed.Invoke(status);
            Parent.RemoveChild(this);
        }

        public override void Update(DwarfTime time)
        {
            if(IsModal && !isClosed && IsVisible)
            {
                GUI.FocusComponent = this;
            }
            else if(GUI.FocusComponent == this)
            {
                GUI.FocusComponent = null;
            }
            base.Update(time);
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            if(!IsVisible)
            {
                return;
            }

            base.Render(time, batch);
        }
    }

}