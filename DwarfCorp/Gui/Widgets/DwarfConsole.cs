using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class DwarfConsole : Gui.Widget
    {
        public List<String> Lines = new List<String>();
        private global::System.Threading.Mutex MessageLock = new global::System.Threading.Mutex();
        private bool NeedsInvalidated = false;

        public Vector4 TextBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.25f);
        private TextGrid TextGrid;

        public int VisibleLines
        {
            get
            {
                return TextGrid.TextHeight;
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

            Lines.Add("");

            TextGrid = AddChild(new TextGrid
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "monofont",
                TextSize = GameSettings.Default.ConsoleTextSize,
            }) as TextGrid;
        }

        public void Append(char C)
        {
            MessageLock.WaitOne();
            if (C == '\n')
            {
                Lines.Add("");
                if (Lines.Count > TextGrid.TextHeight)
                    Lines.RemoveAt(0);
            }
            else
            {
                Lines[Lines.Count - 1] += C;
                if (Lines[Lines.Count - 1].Length >= TextGrid.TextWidth)
                {
                    Lines.Add("");
                    if (Lines.Count > TextGrid.TextHeight)
                        Lines.RemoveAt(0);
                }
            }

            // Need to invalidate inside the main GUI thread or else!
            NeedsInvalidated = true;
            MessageLock.ReleaseMutex();
        }

        public void AddMessage(String Message)
        {
            foreach (var c in Message)
                Append(c);
        }

        protected override Gui.Mesh Redraw()
        {
            if (TextGrid == null)
                return base.Redraw();

            MessageLock.WaitOne();
            var i = 0;
            var y = 0;
            for (; y < Lines.Count; ++y)
            {
                var x = 0;
                for (; x < Lines[y].Length; ++x)
                {
                    TextGrid.SetCharacter(i, Lines[y][x]);
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

        public void AddCommandEntry()
        {
            if (Children.Count == 1)
            {
                var input = AddChild(new Gui.Widgets.CommandEntry
                {
                    AutoLayout = AutoLayout.DockBottom,
                    MinimumSize = new Point(0, 32),
                    Text = "",
                    Border = "",
                    TextSize = GameSettings.Default.ConsoleTextSize,
                    Font = "monofont"
                });
                SendToBack(input);
                Layout();
            }
        }
    }
}
