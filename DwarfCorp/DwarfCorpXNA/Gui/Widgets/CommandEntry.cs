using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class CommandEntry : Widget
    {
        private int CursorPosition = 0;

        private enum State
        {
            AcceptingInput,
            Paginating
        }

        private State CurrentState = State.AcceptingInput;
        private List<String> PendingOutput = new List<string>();

        public override void Construct()
        {
            OnClick += (sender, args) =>
                {
                    if (Object.ReferenceEquals(this, Root.FocusItem))
                    {
                        // This widget already has focus - move cursor to click position.

                        var clickIndex = 0;
                        var clickX = args.X - this.GetDrawableInterior().X;
                        var searchIndex = 0;
                        var font = Root.GetTileSheet(Font);
                        
                        while (true)
                        {
                            if (searchIndex == Text.Length)
                            {
                                clickIndex = Text.Length;
                                break;
                            }

                            var glyphSize = font.GlyphSize(Text[searchIndex] - ' ');
                            if (clickX < glyphSize.X)
                            {
                                clickIndex = searchIndex;
                                if (clickX > (glyphSize.X / 2)) clickIndex += 1;
                                break;
                            }

                            clickX -= glyphSize.X;
                            searchIndex += 1;
                        }

                        CursorPosition = clickIndex;
                        Invalidate();
                        args.Handled = true;
                    }
                    else
                    {
                        // Take focus and move cursor to end of text.
                        Root.SetFocus(this);
                        CursorPosition = Text.Length;
                        Invalidate();
                        args.Handled = true;
                    }
                };

            OnGainFocus += (sender) => this.Invalidate();
            OnLoseFocus += (sender) => this.Invalidate();
            OnUpdateWhileFocus += (sender) => this.Invalidate();

            OnKeyUp += (sender, args) =>
            {
                args.Handled = true;
            };

            OnKeyPress += (sender, args) =>
                {
                    if (args.KeyValue == '`') return;

                    if (CurrentState == State.Paginating)
                    {
                        ShowNextPage(Parent as DwarfConsole);

                        if (PendingOutput.Count > 0)
                            CurrentState = State.Paginating;
                        else
                            CurrentState = State.AcceptingInput;
                    }
                    else
                    {
                        if (args.KeyValue == '\r')
                        {
                            var console = Parent as DwarfConsole;
                            var output = Debugger.HandleConsoleCommand(Text);
                            var lines = output.Split('\n');
                            PendingOutput.Add("> " + Text);
                            PendingOutput.AddRange(lines);
                            ShowNextPage(console);

                            if (PendingOutput.Count > 0)
                                CurrentState = State.Paginating;
                            else
                                CurrentState = State.AcceptingInput;

                            Text = "";
                            Invalidate();
                            args.Handled = true;
                        }
                        else
                        {
                            Text = TextFieldLogic.Process(Text, CursorPosition, args.KeyValue, out CursorPosition);
                            Invalidate();
                            args.Handled = true;
                        }
                    }
                };

            OnKeyDown += (sender, args) =>
                {
                    if (IsAnyParentHidden() || IsAnyParentTransparent())
                    {
                        return;
                    }

                    Invalidate();
                    args.Handled = true;
                };
        }

        private void ShowNextPage(DwarfConsole Console)
        {
            int linesShown = 0;
            while (linesShown < Console.VisibleLines - 2 && PendingOutput.Count > 0)
            {
                Console.AddMessage(PendingOutput[0] + "\n");
                PendingOutput.RemoveAt(0);
                linesShown += 1;
            }

            if (PendingOutput.Count > 0)
                Console.AddMessage("** Press any key for more\n");
        }

        protected override Mesh Redraw()
        {
            var result = new List<Mesh>();

            if (Object.ReferenceEquals(this, Root.FocusItem))
            {
                if (CursorPosition > Text.Length || CursorPosition < 0)
                    CursorPosition = Text.Length;

                var cursorTime = (int)(Math.Floor(Root.RunTime / Root.CursorBlinkTime));
                if ((cursorTime % 2) == 0)
                {
                    var font = Root.GetTileSheet(Font);
                    var drawableArea = this.GetDrawableInterior();

                    var pipeGlyph = font.GlyphSize('|');
                    var cursorMesh = Mesh.Quad()
                        .Scale(pipeGlyph.X * TextSize, pipeGlyph.Y * TextSize)
                        .Translate(drawableArea.X 
                            + font.MeasureString(Text.Substring(0, CursorPosition)).X * TextSize 
                            - ((pipeGlyph.X * TextSize) / 2),
                            drawableArea.Y + ((drawableArea.Height - (pipeGlyph.Y * TextSize)) / 2))
                        .Texture(font.TileMatrix((int)('|' )))
                        .Colorize(new Vector4(1, 0, 0, 1));
                    result.Add(cursorMesh);
                }
            }

            // Add text label
            if (!String.IsNullOrEmpty(Text))
            {
                var _text = ModString(Text);
                var drawableArea = GetDrawableInterior();
                var stringMeshSize = new Rectangle();
                var font = Root.GetTileSheet(Font);
                var text = (WrapText)
                    ? font.WordWrapString(_text, TextSize, drawableArea.Width)
                    : _text;
                var stringMesh = Mesh.CreateStringMesh(
                    text,
                    font,
                    new Vector2(TextSize, TextSize),
                    out stringMeshSize)
                    .Colorize(TextColor);


                var textDrawPos = Vector2.Zero;
                textDrawPos.X = drawableArea.X;
                textDrawPos.Y = drawableArea.Y + ((drawableArea.Height - stringMeshSize.Height) / 2);

                stringMesh.Translate(textDrawPos.X, textDrawPos.Y);
                result.Add(stringMesh);
            }

            return Mesh.Merge(result.ToArray());
        }

        private String ModString(String S)
        {
            var r = new StringBuilder();
            foreach (var C in S)
                r.Append((char)(C + ' '));
            return r.ToString();
        }
    }
}
