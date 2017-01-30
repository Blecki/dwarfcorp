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

        public override void Construct()
        {
            Background = new TileReference("icon-frame", 0);
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
            meshes.Add(base.Redraw());

            if (Icon != null)
            {
                var iconSheet = Root.GetTileSheet(Icon.Sheet);
                meshes.Add(Gum.Mesh.Quad()
                    .Scale(iconSheet.TileWidth, iconSheet.TileHeight)
                    .Texture(iconSheet.TileMatrix(Icon.Tile))
                    .Translate(Rect.X + (Rect.Width / 2) - (iconSheet.TileWidth / 2), 
                        Rect.Y + (Rect.Height / 2) - (iconSheet.TileHeight / 2)));
            }
            
            return Gum.Mesh.Merge(meshes.ToArray());
        }
    }
}
