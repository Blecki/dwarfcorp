using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Play
{
    // Needs the frame and caption
    public class CommandMenuItemIcon : Widget
    {
        private Gui.TextureAtlas.SpriteAtlasEntry CachedDynamicSheet = null;
        public CommandMenuItem Command;
        public bool Enabled = true;

        private bool _mouseisOver = false;
        public bool MouseIsOver
        {
            get { return _mouseisOver; }
            set
            {
                _mouseisOver = value;
                Invalidate();
            }
        }

        public void UpdateAvailability()
        {
            if (Command.IsAvailable != null && !Command.IsAvailable())
                Enabled = false;
            else
                Enabled = true;
            Invalidate();
        }

        public override void Construct()
        {
            Tooltip = Command.Tooltip;
            MinimumSize = new Point(40, 40);
            Background = new TileReference("icon-frame", 0);
            BackgroundColor = new Vector4(0, 0, 0, 1.0f);
            TextVerticalAlign = VerticalAlign.Below;
            TextHorizontalAlign = HorizontalAlign.Center;
            TextColor = new Vector4(1, 1, 1, 1);
            Text = Command.DisplayName;

            if (Command != null && Command.Icon != null && CachedDynamicSheet == null)
            {
                CachedDynamicSheet = ResourceGraphicsHelper.GetDynamicSheet(Root, Command.Icon);
            }


            OnClose = (sender) =>
            {
                if (CachedDynamicSheet != null)
                    CachedDynamicSheet.Discard();
                CachedDynamicSheet = null;
            };

            OnMouseEnter += (widget, action) =>
            {
                MouseIsOver = true;
            };

            OnMouseLeave += (widget, action) =>
            {
                MouseIsOver = false;
            };

            base.Construct();
        }

        protected override Mesh Redraw()
        {
            var r = base.Redraw();

            if (Command != null)
            {
                var interior = Rect;

                r.QuadPart()
                        .Scale(Rect.Width, Rect.Height)
                        .Translate(Rect.X, Rect.Y)
                        .Colorize(MouseIsOver
                            ? new Vector4(1, 0.5f, 0.5f, 1)
                            : new Vector4(1, 1, 1, 1))
                        .Texture(Root.GetTileSheet(Background.Sheet).TileMatrix(Background.Tile));
                    interior = Rect.Interior(4, 4, 4, 4);
                
                if (Command.Icon != null)
                {
                    if (CachedDynamicSheet != null)
                        r.QuadPart()
                            .Scale(32, 32)
                            .Translate(interior.X + (interior.Width - 32) / 2, interior.Y + (interior.Height - 32) / 2)
                            .Texture(CachedDynamicSheet.TileSheet.TileMatrix(0));
                }
                else
                {
                    var tile = Root.GetTileSheet(Command.OldStyleIcon.Sheet);
                    r.QuadPart()
                        .Scale(32, 32)
                            .Translate(interior.X + (interior.Width - 32) / 2, interior.Y + (interior.Height - 32) / 2)
                        .Texture(Root.GetTileSheet(Command.OldStyleIcon.Sheet).TileMatrix(Command.OldStyleIcon.Tile));
                }

                if (!Enabled)
                    r.EntireMeshAsPart().Colorize(new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
            }

            return r;
        }
    }
}
