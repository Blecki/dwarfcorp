using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class CompanyLogo : Widget
    {
        public TileReference LogoBackground = null;
        public Vector4 LogoBackgroundColor = Vector4.One;
        public TileReference LogoSymbol = null;
        public Vector4 LogoSymbolColor = Vector4.One;

        public override Point GetBestSize()
        {
            if (LogoBackground != null)
            {
                var logoTileSheet = Root.GetTileSheet(LogoBackground.Sheet);
                return new Point(logoTileSheet.TileWidth, logoTileSheet.TileHeight);
            }

            return base.GetBestSize();
        }

        protected override Gum.Mesh Redraw()
        {
            var meshes = new List<Gum.Mesh>();
            meshes.Add(base.Redraw());

            if (LogoBackground != null)
            {
                var bgTileSet = Root.GetTileSheet(LogoBackground.Sheet);
                meshes.Add(Gum.Mesh.Quad()
                    .Scale(Rect.Width, Rect.Height)
                    .Texture(bgTileSet.TileMatrix(LogoBackground.Tile))
                    .Translate(Rect.X, Rect.Y)
                    .Colorize(LogoBackgroundColor));
            }

            if (LogoSymbol != null)
            {
                // Todo: Center symbol on logo.
                var symbol = Root.GetTileSheet(LogoSymbol.Sheet);
                meshes.Add(Gum.Mesh.Quad()
                    .Scale(Rect.Width, Rect.Height)
                    .Texture(symbol.TileMatrix(LogoSymbol.Tile))
                    .Translate(Rect.X, Rect.Y)
                    .Colorize(LogoSymbolColor));
            }

            return Gum.Mesh.Merge(meshes.ToArray());
        }
    }
}
