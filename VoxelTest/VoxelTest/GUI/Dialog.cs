using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class Dialog : SillyGUIComponent
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

        public static Dialog Popup(SillyGUI gui, string title, string message, ButtonType buttons)
        {
            Dialog d = new Dialog(gui, gui.RootComponent, title, message, buttons);
            int w = message.Length * 8 + 150;
            int h = 150;
            d.LocalBounds = new Rectangle(gui.Graphics.Viewport.Width / 2 - w/2, gui.Graphics.Viewport.Height / 2 - h/2, w, h);
      
            return d;
        }

        public Dialog(SillyGUI gui, SillyGUIComponent parent, string title, string message, ButtonType buttons) :
            base(gui, parent)
        {
            IsModal = true;
            OnClicked += new ClickedDelegate(Dialog_OnClicked);
            OnClosed += new Closed(Dialog_OnClosed);

            GridLayout layout = new GridLayout(GUI, this, 4, 4);
            Title = new Label(GUI, layout, title, GUI.DefaultFont);
            layout.SetComponentPosition(Title, 0, 0, 1, 1);

            Message = new Label(GUI, layout, message, GUI.DefaultFont);
            layout.SetComponentPosition(Message, 0, 1, 4, 2);


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
                Button OKButton = new Button(GUI, layout, "OK", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
                layout.SetComponentPosition(OKButton, 3, 3, 1, 1);
                OKButton.OnClicked += new ClickedDelegate(OKButton_OnClicked);
            }

            if (createCancel)
            {
                Button cancelButton = new Button(GUI, layout, "Cancel", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
                layout.SetComponentPosition(cancelButton, 2, 3, 1, 1);
                cancelButton.OnClicked += new ClickedDelegate(cancelButton_OnClicked);
            }
        }

        void cancelButton_OnClicked()
        {
            Close(ReturnStatus.Canceled);
        }

        void OKButton_OnClicked()
        {
            Close(ReturnStatus.Ok);
        }

        void Dialog_OnClosed(Dialog.ReturnStatus status)
        {
            // nothing
        }

        void Dialog_OnClicked()
        {
            if (IsMouseOver)
            {
                GUI.FocusComponent = this;
            }
            else if(!IsModal)
            {
                if (GUI.FocusComponent == this)
                {
                    GUI.FocusComponent = null;
                }
            }
        }


        public virtual void Close(ReturnStatus status)
        {
            if (GUI.FocusComponent == this)
            {
                GUI.FocusComponent = null;
            }

            isClosed = true;
            IsVisible = false;

            OnClosed.Invoke(status);
            Parent.RemoveChild(this);
           
        }

        public override void Update(GameTime time)
        {
            if (IsModal && !isClosed && IsVisible)
            {
                GUI.FocusComponent = this;
            }
            else if (GUI.FocusComponent == this)
            {
                GUI.FocusComponent = null;
            }
            base.Update(time);
        }

        public override void Render(GameTime time, SpriteBatch batch)
        {
            if (!IsVisible)
            {
                return;
            }

            GUI.Skin.RenderPanel(GlobalBounds, batch);
            base.Render(time, batch);
        }


    }
}
