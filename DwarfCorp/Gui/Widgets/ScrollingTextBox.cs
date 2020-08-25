using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class ScrollingTextBox : Gui.Widget
    {
        private TextBoxEx TextBox;
        private VerticalScrollBar ScrollBar;
        public bool ScrollOnAppend = true;
        private bool TextAppendedThisFrame = false;

        public override void Construct()
        {
            ScrollBar = AddChild(new VerticalScrollBar
            {
                AutoLayout = AutoLayout.DockRight,
                OnScrollValueChanged = (sender) =>
                {
                    TextBox.OffsetLines = ScrollBar.ScrollPosition;
                }
            }) as VerticalScrollBar;

            TextBox = AddChild(new TextBoxEx
            {
                AutoLayout = AutoLayout.DockFill
            }) as TextBoxEx;
        }

        public void AppendText(String Message)
        {
            TextBox.AppendText(Message);
            TextAppendedThisFrame = true;
        }

        public void ClearText()
        {
            TextBox.ClearText();
        }

        protected override Mesh Redraw()
        {
            ScrollBar.ScrollArea = TextBox.ScrollSize;
            if (ScrollOnAppend && TextAppendedThisFrame)
            {
                TextAppendedThisFrame = false;
                ScrollBar.ScrollPosition = TextBox.ScrollSize;
            }
            return base.Redraw();
        }
    }
}
