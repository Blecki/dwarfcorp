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
        public TileReference Icon = null;
        private bool _enabled = true;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                if (!_enabled) Root.SafeCall(OnDisable, this);
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

        public override void Construct()
        {
            Background = new TileReference("icon-frame", 0);

            if (OnClick != null)
            {
                var lambdaOnClick = this.OnClick;
                OnClick = (sender, args) => { if (Enabled) lambdaOnClick(sender, args); };
            }
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

            meshes.Add(Gum.Mesh.Quad()
                    .Scale(Rect.Width, Rect.Height)
                    .Translate(Rect.X, Rect.Y)
                    .Colorize(Hilite ? new Vector4(1,0,0,1) : BackgroundColor)
                    .Texture(Root.GetTileSheet(Background.Sheet).TileMatrix(Background.Tile)));

            if (Icon != null)
            {
                var iconSheet = Root.GetTileSheet(Icon.Sheet);
                meshes.Add(Gum.Mesh.Quad()
                    .Scale(iconSheet.TileWidth, iconSheet.TileHeight)
                    .Texture(iconSheet.TileMatrix(Icon.Tile))
                    .Translate(Rect.X + (Rect.Width / 2) - (iconSheet.TileWidth / 2),
                        Rect.Y + (Rect.Height / 2) - (iconSheet.TileHeight / 2))
                    .Colorize(Enabled ? new Vector4(1, 1, 1, 1) : new Vector4(0.15f, 0.15f, 0.15f, 1)));
            }
            
            return Gum.Mesh.Merge(meshes.ToArray());
        }
    }
}
