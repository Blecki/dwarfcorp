using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class EditableTextField : Widget
    {
        private int CursorPosition = 0;
        public bool HiliteOnMouseOver = true;

        public Action<Widget> OnTextChange = null;

        public class BeforeTextChangeEventArgs
        {
            public String NewText;
            public bool Cancelled = false;
        }

        public Action<Widget, BeforeTextChangeEventArgs> BeforeTextChange = null;

        public override void Construct()
        {
            if (String.IsNullOrEmpty(Border)) Border = "border-thin";

            // Note: Cursor won't draw properly if these are changed. Click events may also break.
            // Widget should probably be able to handle different alignments.
            TextVerticalAlign = VerticalAlign.Center;
            TextHorizontalAlign = HorizontalAlign.Left;

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
                    }
                    else
                    {
                        // Take focus and move cursor to end of text.
                        Root.SetFocus(this);
                        CursorPosition = Text.Length;
                        Invalidate();
                    }
                };

            OnGainFocus += (sender) => this.Invalidate();
            OnLoseFocus += (sender) => this.Invalidate();
            OnUpdateWhileFocus += (sender) => this.Invalidate();

            OnKeyPress += (sender, args) =>
                {
                    // Actual logic of modifying the string is outsourced.
                    var beforeEventArgs = new BeforeTextChangeEventArgs
                        {
                            NewText = TextFieldLogic.Process(Text, CursorPosition, args.KeyValue, out CursorPosition),
                            Cancelled = false
                        };
                    Root.SafeCall(BeforeTextChange, this, beforeEventArgs);
                    if (beforeEventArgs.Cancelled == false)
                    {
                        Text = beforeEventArgs.NewText;
                        Root.SafeCall(OnTextChange, this);
                        Invalidate();
                    }
                };

            OnKeyDown += (sender, args) =>
                {
                    var beforeEventArgs = new BeforeTextChangeEventArgs
                        {
                            NewText = TextFieldLogic.HandleSpecialKeys(Text, CursorPosition, args.KeyValue, out CursorPosition),
                            Cancelled = false
                        };
                    Root.SafeCall(BeforeTextChange, this, beforeEventArgs);
                    if (beforeEventArgs.Cancelled == false)
                    {
                        Text = beforeEventArgs.NewText;
                        Root.SafeCall(OnTextChange, this);
                        Invalidate();
                    }
                    //Root.SafeCall(OnTextChange, this);
                    Invalidate();
                };

            if (HiliteOnMouseOver)
            {
                var color = TextColor;
                OnMouseEnter += (widget, action) =>
                {
                    widget.TextColor = new Vector4(0.5f, 0, 0, 1.0f);
                    widget.Invalidate();
                };

                OnMouseLeave += (widget, action) =>
                {
                    widget.TextColor = color;
                    widget.Invalidate();
                };
            }
        }

        protected override Mesh Redraw()
        {
            if (Object.ReferenceEquals(this, Root.FocusItem))
            {
                if (CursorPosition > Text.Length || CursorPosition < 0)
                {
                    CursorPosition = Text.Length;
                }

                var cursorTime = (int)(Math.Floor(Root.RunTime / Root.CursorBlinkTime));
                if ((cursorTime & 1) == 1)
                {
                    var font = Root.GetTileSheet(Font);
                    var drawableArea = this.GetDrawableInterior();

                    var pipeGlyph = font.GlyphSize('|' - ' ');
                    var cursorMesh = Mesh.Quad()
                        .Scale(pipeGlyph.X * TextSize, pipeGlyph.Y * TextSize)
                        .Translate(drawableArea.X 
                            + font.MeasureString(Text.Substring(0, CursorPosition)).X * TextSize 
                            - ((pipeGlyph.X * TextSize) / 2),
                            drawableArea.Y + ((drawableArea.Height - (pipeGlyph.Y * TextSize)) / 2))
                        .Texture(font.TileMatrix((int)('|' - ' ')))
                        .Colorize(new Vector4(1, 0, 0, 1));
                    return Mesh.Merge(base.Redraw(), cursorMesh);
                }
            }
            
            return base.Redraw();
        }
    }
}
