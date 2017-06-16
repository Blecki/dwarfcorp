using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui
{
    public partial class Mesh
    {
        public static Mesh CreateTabPanelBackground(Rectangle Rect, ITileSheet Tiles, int TabX, int TabWidth)
        {
            var result = new List<Mesh>();

            //Top-left corner
            if (TabX == Rect.X)
            {
                //Vertical edge segment
                result.Add(Quad()
                .Texture(Tiles.TileMatrix(3))
                .Scale(Tiles.TileWidth, Tiles.TileHeight)
                .Translate(Rect.X, Rect.Y));
            }
            else
            {
                //Corner
                result.Add(Quad()
                    .TileScaleAndTexture(Tiles, 0)
                    .Translate(Rect.X, Rect.Y));

                //Top edge
                result.Add(Quad()
                    .Texture(Tiles.TileMatrix(1))
                    .Scale(TabX - Rect.X - Tiles.TileWidth, Tiles.TileHeight)
                    .Translate(Rect.X + Tiles.TileWidth, Rect.Y));

                //Interior corner
                result.Add(Quad()
                    .Texture(Tiles.TileMatrix(9))
                    .Scale(Tiles.TileWidth, Tiles.TileHeight)
                    .Translate(TabX, Rect.Y));
            }

            //Top-right corner
            if (Rect.X + Rect.Width == TabX + TabWidth)
            {
                //Vertical edge segment
                result.Add(Quad()
                    .Texture(Tiles.TileMatrix(5))
                    .Scale(Tiles.TileWidth, Tiles.TileHeight)
                    .Translate(Rect.Right - Tiles.TileWidth, Rect.Y));
            }
            else
            {
                //Corner
                result.Add(Quad()
                    .TileScaleAndTexture(Tiles, 2)
                    .Translate(Rect.Right - Tiles.TileWidth, Rect.Y));

                //Top edge
                result.Add(Quad()
                    .Texture(Tiles.TileMatrix(1))
                    .Scale(Rect.Right - TabX - TabWidth - Tiles.TileWidth, Tiles.TileHeight)
                    .Translate(TabX + TabWidth, Rect.Y));

                //Interior corner
                result.Add(Quad()
                    .Texture(Tiles.TileMatrix(10))
                    .Scale(Tiles.TileWidth, Tiles.TileHeight)
                    .Translate(TabX + TabWidth - Tiles.TileWidth, Rect.Y));
            }
            
            //Top edge - bottom of tab
            result.Add(Quad()
                .Texture(Tiles.TileMatrix(4))
                .Scale(TabWidth - (2 * Tiles.TileWidth), Tiles.TileHeight)
                .Translate(TabX + Tiles.TileWidth, Rect.Y));


            //Bottom-left corner
            result.Add(Quad()
                .TileScaleAndTexture(Tiles, 6)
                .Translate(Rect.X, Rect.Bottom - Tiles.TileHeight));

            //Bottom-right corner
            result.Add(Quad()
                .TileScaleAndTexture(Tiles, 8)
                .Translate(Rect.Right - Tiles.TileWidth, Rect.Bottom - Tiles.TileHeight));

            //Bottom edge
            result.Add(Quad()
                .Texture(Tiles.TileMatrix(7))
                .Scale(Rect.Width - (2 * Tiles.TileWidth), Tiles.TileHeight)
                .Translate(Rect.X + Tiles.TileWidth, Rect.Bottom - Tiles.TileHeight));

            //Left edge
            result.Add(Quad()
                .Texture(Tiles.TileMatrix(3))
                .Scale(Tiles.TileWidth, Rect.Height - (2 * Tiles.TileHeight))
                .Translate(Rect.X, Rect.Y + Tiles.TileHeight));

            //Right edge
            result.Add(Quad()
                .Texture(Tiles.TileMatrix(5))
                .Scale(Tiles.TileWidth, Rect.Height - (2 * Tiles.TileHeight))
                .Translate(Rect.Right - Tiles.TileWidth, Rect.Y + Tiles.TileHeight));

            //Center
            result.Add(Quad()
                .Texture(Tiles.TileMatrix(4))
                .Scale(Rect.Width - (2 * Tiles.TileWidth), Rect.Height - (2 * Tiles.TileHeight))
                .Translate(Rect.X + Tiles.TileWidth, Rect.Y + Tiles.TileHeight));

            return Merge(result.ToArray());
        }
    }
}