using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class CompanyLogo : Widget
    {
        public CompanyInformation CompanyInformation;

        public override Point GetBestSize()
        {
            if (CompanyInformation.LogoBackground != null)
            {
                var logoTileSheet = Root.GetTileSheet(CompanyInformation.LogoBackground.Sheet);
                return new Point(logoTileSheet.TileWidth, logoTileSheet.TileHeight);
            }

            return base.GetBestSize();
        }

        protected override Gui.Mesh Redraw()
        {
            var mesh = base.Redraw();

            if (CompanyInformation.LogoBackground != null)
            {
                var bgTileSet = Root.GetTileSheet(CompanyInformation.LogoBackground.Sheet);
                mesh.QuadPart()
                    .Scale(Rect.Width, Rect.Height)
                    .Texture(bgTileSet.TileMatrix(CompanyInformation.LogoBackground.Tile))
                    .Translate(Rect.X, Rect.Y)
                    .Colorize(CompanyInformation.LogoBackgroundColor);
            }

            if (CompanyInformation.LogoSymbol != null)
            {
                // Todo: Center symbol on logo.
                var symbol = Root.GetTileSheet(CompanyInformation.LogoSymbol.Sheet);
                mesh.QuadPart()
                    .Scale(Rect.Width, Rect.Height)
                    .Texture(symbol.TileMatrix(CompanyInformation.LogoSymbol.Tile))
                    .Translate(Rect.X, Rect.Y)
                    .Colorize(CompanyInformation.LogoSymbolColor);
            }

            return mesh;
        }
    }
}
