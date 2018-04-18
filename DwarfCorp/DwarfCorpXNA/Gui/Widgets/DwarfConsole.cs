using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class DwarfConsole : Gui.Widget
    {
        private List<String> Messages = new List<String>();
        private System.Threading.Mutex MessageLock = new System.Threading.Mutex();
        private bool NeedsInvalidated = false;

        public Vector4 TextBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.25f);
        private TextGrid TextGrid;

        public int VisibleLines
        {
            get
            {
                var font = Root.GetTileSheet(Font);
                return Rect.Height / (font.TileHeight * TextSize);
            }
        }

        public override void Construct()
        {
            Root.RegisterForUpdate(this);

            OnUpdate = (sender, time) =>
            {
                MessageLock.WaitOne();
                if (NeedsInvalidated)
                    this.Invalidate();
                NeedsInvalidated = false;
                MessageLock.ReleaseMutex();
            };

            Messages.Add("");

            TextGrid = AddChild(new TextGrid
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "monofont",
                TextSize = 2,
            }) as TextGrid;
        }

        public void AddMessage(String Message)
        {
            // AddMessage is called by another thread - need to protect the list.
            MessageLock.WaitOne();

            var last = Messages.Last();
            foreach (var c in Message)
            {
                if (c == '\n')
                {
                    Messages[Messages.Count - 1] = last;
                    Messages.Add("");
                    if (Messages.Count > TextGrid.TextHeight)
                        Messages.RemoveAt(0);
                    last = "";
                }
                else
                {
                    last += c;
                    if (last.Length >= TextGrid.TextWidth)
                    {
                        Messages[Messages.Count - 1] = last;
                        Messages.Add("");
                        if (Messages.Count > TextGrid.TextHeight)
                            Messages.RemoveAt(0);
                        last = "";
                    }
                }
            }
            Messages[Messages.Count - 1] = last;
                        
            // Need to invalidate inside the main GUI thread or else!
            NeedsInvalidated = true;
            MessageLock.ReleaseMutex();
        }

        protected override Gui.Mesh Redraw()
        {
            MessageLock.WaitOne();
            var i = 0;
            var y = 0;
            for (; y < Messages.Count; ++y)
            {
                var x = 0;
                for (; x < Messages[y].Length; ++x)
                {
                    TextGrid.SetCharacter(i, Messages[y][x]);
                    ++i;
                }
                for (; x < TextGrid.TextWidth; ++x)
                {
                    TextGrid.SetCharacter(i, ' ');
                    ++i;
                }
            }
            for (; y < TextGrid.TextHeight; ++y)
            {
                for (var x = 0; x < TextGrid.TextWidth; ++x)
                {
                    TextGrid.SetCharacter(i, ' ');
                    ++i;
                }
            }
            MessageLock.ReleaseMutex();

            TextGrid.Invalidate();

            return base.Redraw();
        }

        public bool HasMesssage(string loadingMessage)
        {
            return Messages.Contains(loadingMessage);
        }
    }
}
