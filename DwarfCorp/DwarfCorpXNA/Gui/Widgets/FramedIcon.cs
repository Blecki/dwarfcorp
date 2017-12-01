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
        
        public TileReference Icon = null;
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

        private bool _drawIndicator = false;
        public bool DrawIndicator
        {
            get { return _drawIndicator; }
            set
            {
                _drawIndicator = value;
                Invalidate();
            }
        }

        private int _indicatorValue = 0;
        public int IndicatorValue
        {
            get { return _indicatorValue; }
            set
            {
                _indicatorValue = value;
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
            var meshes = new List<Gui.Mesh>();

            if (DrawFrame)
            {
                meshes.Add(Gui.Mesh.Quad()
                    .Scale(Rect.Width, Rect.Height)
                    .Translate(Rect.X, Rect.Y)
                    .Colorize(MouseIsOver
                        ? new Vector4(1, 0.5f, 0.5f, 1)
                        : (Hilite ? new Vector4(1, 0, 0, 1) : BackgroundColor))
                    .Texture(Root.GetTileSheet(Background.Sheet).TileMatrix(Background.Tile)));
            }

            if (Icon != null)
            {
                var iconSheet = Root.GetTileSheet(Icon.Sheet);
                meshes.Add(Gui.Mesh.Quad()
                    .Scale(iconSheet.TileWidth, iconSheet.TileHeight)
                    .Texture(iconSheet.TileMatrix(Icon.Tile))
                    .Translate(Rect.X + (Rect.Width / 2) - (iconSheet.TileWidth / 2),
                        Rect.Y + (Rect.Height / 2) - (iconSheet.TileHeight / 2))
                    .Colorize(Enabled ? Tint : new Vector4(0.15f * Tint.X, 0.15f * Tint.Y, 0.15f * Tint.Z, 1 * Tint.W)));
            }

            if (!string.IsNullOrEmpty(Text))
            {
                if (Enabled) TextColor = EnabledTextColor;
                else TextColor = DisabledTextColor;
                base.GetTextMesh(meshes);
            }

            if (DrawIndicator && IndicatorValue != 0)
            {
                var indicatorTile = Root.GetTileSheet("indicator-circle");
                meshes.Add(Gui.Mesh.Quad()
                    .Scale(16, 16)
                    .Texture(indicatorTile.TileMatrix(0))
                    .Translate(Rect.Right - 16,
                        Rect.Bottom - 16).Colorize(Color.OrangeRed.ToVector4()));
                var numberSize = new Rectangle();
                var font = Root.GetTileSheet("font8");
                var stringMesh = Gui.Mesh.CreateStringMesh(
                    IndicatorValue.ToString(),
                    font,
                    new Vector2(1,1),
                    out numberSize)
                    .Colorize(new Vector4(1,1,1,1));
                meshes.Add(stringMesh.
                    Translate(Rect.Right - 8 - (numberSize.Width / 2),
                    Rect.Bottom - 8 - (numberSize.Height / 2)));
            }

            if (DrawHotkey)
            {
                var font = Root.GetTileSheet("font8");
                var numberSize = new Rectangle();
                char hotkey = '?';
                InputManager.TryConvertKeyboardInput(HotkeyValue, false, out hotkey);
                var stringMesh = Gui.Mesh.CreateStringMesh(
                    hotkey.ToString(),
                    font,
                    new Vector2(1, 1),
                    out numberSize)
                    .Colorize(new Vector4(1, 1, 1, 0.4f));
                meshes.Add(stringMesh.
                    Translate(Rect.Left + 8 - (numberSize.Width / 2),
                    Rect.Top + 8 - (numberSize.Height / 2)));
            }

            return Gui.Mesh.Merge(meshes.ToArray());
        }
    }
}
