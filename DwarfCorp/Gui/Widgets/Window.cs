using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Tutorial;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class Window : Widget
    {
        bool Dragging = false;
        Point MouseDownPos = Point.Zero;
        Point RectStartPos = Point.Zero;
        public Rectangle StartingSize = new Rectangle(0, 0, 450, 400);

        public override void Construct()
        {
            //Set size and center on screen.
            Rect = StartingSize;
            

            Border = "window";

            Font = "font16";
            TextColor = new Vector4(0, 0, 0, 1);
            //InteriorMargin = new Margin(16, 0, 0, 0);
            VerticalTextOffset = -18;

            Layout();

            OnMouseDown += (sender, args) =>
            {
                if (args.Y < Rect.Y + 32)
                {
                    if (args.X > (Rect.Right - 36))
                    {
                        this.Hidden = true;
                    }
                    else
                    {
                        Dragging = true;
                        MouseDownPos = new Point(args.X, args.Y);
                        RectStartPos = new Point(Rect.X, Rect.Y);
                    }
                }

                this.BringToFront();
            };

            OnMouseMove += (sender, args) =>
            {
                if (Dragging)
                {
                    var newMousePos = new Point(args.X, args.Y);
                    var delta = new Point(newMousePos.X - MouseDownPos.X, newMousePos.Y - MouseDownPos.Y);
                    var newRectPos = new Point(RectStartPos.X + delta.X, RectStartPos.Y + delta.Y);
                    this.Rect = new Rectangle(newRectPos.X, newRectPos.Y, Rect.Width, Rect.Height);

                    if (Rect.X < 0) Rect.X = 0;
                    if (Rect.Y < 0) Rect.Y = 0;
                    if (Rect.Right > Root.RenderData.VirtualScreen.Width) Rect.X = Root.RenderData.VirtualScreen.Width - Rect.Width;
                    if (Rect.Bottom > Root.RenderData.VirtualScreen.Height) Rect.Y = Root.RenderData.VirtualScreen.Height - Rect.Height;

                    this.Layout();
                    this.Invalidate();

                    args.Handled = true;
                }
            };

            OnMouseUp += (sender, args) =>
            {
                Dragging = false;
            };
        }

        protected override Mesh Redraw()
        {
            var r = base.Redraw();
            AddCloseButtonMesh(r);
            return r;
        }

        private void AddCloseButtonMesh(Mesh r)
        {
            var buttonSheet = Root.GetTileSheet("round-buttons");
            r.QuadPart().Scale(buttonSheet.TileWidth, buttonSheet.TileHeight)
                .Translate(Rect.Right - 36, Rect.Y + 30 - buttonSheet.TileHeight)
                .Texture(buttonSheet.TileMatrix(5));
        }
    }
}
