using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    /// <summary>
    /// A properly framed Icon for use in an icon tray.
    /// </summary>
    public class FramedIcon : Widget
    {
        public Vector4 Tint = Vector4.One;
        
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

        protected override Gum.Mesh Redraw()
        {
            var meshes = new List<Gum.Mesh>();

            if (DrawFrame)
            {
                meshes.Add(Gum.Mesh.Quad()
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
                meshes.Add(Gum.Mesh.Quad()
                    .Scale(iconSheet.TileWidth, iconSheet.TileHeight)
                    .Texture(iconSheet.TileMatrix(Icon.Tile))
                    .Translate(Rect.X + (Rect.Width / 2) - (iconSheet.TileWidth / 2),
                        Rect.Y + (Rect.Height / 2) - (iconSheet.TileHeight / 2))
                    .Colorize(Enabled ? Tint : new Vector4(0.15f * Tint.X, 0.15f * Tint.Y, 0.15f * Tint.Z, 1 * Tint.W)));
            }

            if (!string.IsNullOrEmpty(Text))
            {
                base.GetTextMesh(meshes);
            }

            if (DrawIndicator && IndicatorValue != 0)
            {
                var indicatorTile = Root.GetTileSheet("indicator-circle");
                meshes.Add(Gum.Mesh.Quad()
                    .Scale(32, 32)
                    .Texture(indicatorTile.TileMatrix(0))
                    .Translate(Rect.Right - 32,
                        Rect.Bottom - 32));
                var numberSize = new Rectangle();
                var font = Root.GetTileSheet("font-hires");
                var stringMesh = Gum.Mesh.CreateStringMesh(
                    IndicatorValue.ToString(),
                    font,
                    new Vector2(1,1),
                    out numberSize)
                    .Colorize(new Vector4(1,1,1,1));
                meshes.Add(stringMesh.
                    Translate(Rect.Right - 16 - (numberSize.Width / 2),
                    Rect.Bottom - 16 - (numberSize.Height / 2)));
            }

            return Gum.Mesh.Merge(meshes.ToArray());
        }
    }
}
