using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class TextBoxEx : Gui.Widget
    {
        private List<String> Lines = new List<string>();

        public int ScrollSize => Lines.Count - VisibleLines;

        private int _offsetLines = 0;
        public int OffsetLines
        {
            set
            {
                _offsetLines = value;
                if (_offsetLines < 0) _offsetLines = 0;
                if (_offsetLines > Lines.Count - VisibleLines)
                    _offsetLines = Lines.Count - VisibleLines;
                this.Invalidate();
            }

            get
            {
                return _offsetLines;
            }
        }

        public int VisibleLines
        {
            get
            {
                var font = Root.GetTileSheet(Font);
                return GetDrawableInterior().Height / (font.TileHeight * TextSize);
            }
        }

        public override void Construct()
        {
            Lines.Add("");
        }

        public void AppendText(String Message)
        {
            if (Lines.Count == 0) Lines.Add("");

            var newLines = Lines[Lines.Count - 1] + Message;
            Lines[Lines.Count - 1] = "";
            var font = Root.GetTileSheet(Font);
            var text = WrapText ? font.WordWrapString(newLines, TextSize, GetDrawableInterior().Width, WrapWithinWords) : newLines;
            foreach (var c in text)
            {
                if (c == '\n')
                    Lines.Add("");
                else
                    Lines[Lines.Count - 1] += c;
            }

            OffsetLines = Lines.Count - VisibleLines;

            this.Invalidate();
        }

        public void ClearText()
        {
            Lines.Clear();
            Lines.Add("");
            this.Invalidate();
        }

        protected override Mesh Redraw()
        {
            Text = String.Join("\n", Lines.Skip(Math.Max(0, OffsetLines)).Take(VisibleLines));
            return base.Redraw();
        }
    }
}
