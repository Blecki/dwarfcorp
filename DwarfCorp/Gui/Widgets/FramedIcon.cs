using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    /// <summary>
    /// A properly framed Icon for use in an icon tray.
    /// </summary>
    public class FramedIcon : Widget
    {
        public Vector4 Tint = Vector4.One;
        public Vector4 EnabledTextColor = new Vector4(1, 1, 1, 1);
        public Vector4 DisabledTextColor = new Vector4(0.15f, 0.15f, 0.15f, 1);
        public bool ChangeTextColorOnEnable = true;
        public TileReference Icon = null;

        public ResourceType.GuiGraphic NewStyleIcon = null;
        private Gui.TextureAtlas.SpriteAtlasEntry CachedDynamicSheet = null;

        private bool _enabled = true;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                if (!_enabled && Root != null) Root.SafeCall(OnDisable, this);
                Invalidate();
            }
        }

        public Action<Widget> OnDisable;

        private bool _hilite = false;
        public bool Hilite
        {
            get { return _hilite; }
            set
            {
                _hilite = value;
                Invalidate();
            }
        }

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

        private bool _drawFrame = true;
        public bool DrawFrame
        {
            get { return _drawFrame; }
            set
            {
                _drawFrame = value;
                Invalidate();
            }
        }

        private bool _drawHotkey = false;
        public bool DrawHotkey
        {
            get { return _drawHotkey; }
            set
            {
                _drawHotkey = value;
                Invalidate();
            }
        }

        private Microsoft.Xna.Framework.Input.Keys _hotkeyValue = 0;
        public Microsoft.Xna.Framework.Input.Keys HotkeyValue
        {
            get { return _hotkeyValue; }
            set
            {
                _hotkeyValue = value;
                Invalidate();
            }
        }

        public override void Construct()
        {
            TextHorizontalAlign = HorizontalAlign.Center;
            Background = new TileReference("icon-frame", 0);

            if (OnClick != null)
            {
                var lambdaOnClick = this.OnClick;
                OnClick = (sender, args) => { if (Enabled) lambdaOnClick(sender, args); };
            }

            OnMouseEnter += (widget, action) =>
            {
                MouseIsOver = true;
            };

            OnMouseLeave += (widget, action) =>
            {
                MouseIsOver = false;
            };

            if (NewStyleIcon != null && CachedDynamicSheet == null)
                CachedDynamicSheet = ResourceGraphicsHelper.GetDynamicSheet(Root, NewStyleIcon);

            OnClose = (sender) =>
            {
                if (CachedDynamicSheet != null)
                    CachedDynamicSheet.Discard();
                CachedDynamicSheet = null;
            };
        }

        public override Point GetBestSize()
        {
            if (Background != null)
            {
                var backgroundSheet = Root.GetTileSheet(Background.Sheet);
                return new Point(backgroundSheet.TileWidth, backgroundSheet.TileHeight);
            }

            return base.GetBestSize();
        }

        protected override Gui.Mesh Redraw()
        {
            var mesh = Mesh.EmptyMesh();
            var interior = Rect;

            if (DrawFrame)
            {
                mesh.QuadPart()
                    .Scale(Rect.Width, Rect.Height)
                    .Translate(Rect.X, Rect.Y)
                    .Colorize(MouseIsOver
                        ? new Vector4(1, 0.5f, 0.5f, 1)
                        : (Hilite ? new Vector4(1, 0, 0, 1) : BackgroundColor))
                    .Texture(Root.GetTileSheet(Background.Sheet).TileMatrix(Background.Tile));
                interior = Rect.Interior(4, 4, 4, 4);
            }

            if (NewStyleIcon != null)
            {

                if (CachedDynamicSheet != null)
                    mesh.QuadPart()
                        .Scale(CachedDynamicSheet.TileSheet.TileWidth, CachedDynamicSheet.TileSheet.TileHeight)
                        .Translate(Rect.X + (Rect.Width / 2) - (CachedDynamicSheet.TileSheet.TileWidth / 2),
                            Rect.Y + (Rect.Height / 2) - (CachedDynamicSheet.TileSheet.TileHeight / 2))
                        .Colorize(BackgroundColor)
                        .Texture(CachedDynamicSheet.TileSheet.TileMatrix(0));
            }
            else if (Icon != null)
            {
                var iconSheet = Root.GetTileSheet(Icon.Sheet);

                if (interior.Width > iconSheet.TileWidth)
                    mesh.QuadPart()
                        .Scale(iconSheet.TileWidth, iconSheet.TileHeight)
                        .Texture(iconSheet.TileMatrix(Icon.Tile))
                        .Translate(Rect.X + (Rect.Width / 2) - (iconSheet.TileWidth / 2),
                            Rect.Y + (Rect.Height / 2) - (iconSheet.TileHeight / 2))
                        .Colorize(Enabled ? Tint : new Vector4(0.15f * Tint.X, 0.15f * Tint.Y, 0.15f * Tint.Z, 1 * Tint.W));
                else
                    mesh.QuadPart()
                        .Scale(interior.Width, interior.Height)
                        .Texture(iconSheet.TileMatrix(Icon.Tile))
                        .Translate(interior.X, interior.Y)
                        .Colorize(Enabled ? Tint : new Vector4(0.15f * Tint.X, 0.15f * Tint.Y, 0.15f * Tint.Z, 1 * Tint.W));
            }

            if (!string.IsNullOrEmpty(Text))
            {
                if (Enabled && ChangeTextColorOnEnable) TextColor = EnabledTextColor;
                else if (!Enabled && ChangeTextColorOnEnable) TextColor = DisabledTextColor;

                var prevRect = Rect;
                Rect = new Rectangle(prevRect.X - 4, prevRect.Y, prevRect.Width + 8, prevRect.Height);
                base.GetTextMeshPart(mesh);
                Rect = prevRect;
            }

            if (DrawHotkey)
            {
                var font = Root.GetTileSheet("font8");
                var numberSize = new Rectangle();
                InputManager.TryConvertKeyboardInput(HotkeyValue, false, out var hotkey);
                var stringMesh = mesh.StringPart(hotkey.ToString(), font, new Vector2(1, 1), out numberSize).Colorize(new Vector4(1, 1, 1, 0.8f));
                stringMesh.Translate(Rect.Left + 8 - (numberSize.Width / 2), Rect.Top + 8 - (numberSize.Height / 2));
            }

            return mesh;
        }
    }
}
