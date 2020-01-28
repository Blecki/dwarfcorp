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
        public MeshPart CreateTabPanelBackgroundPart(Rectangle Rect, ITileSheet Tiles, int TabX, int TabWidth)
        {
            var r = new MeshPart { Mesh = this, VertexOffset = this.VertexCount };

            //Top-left corner
            if (TabX == Rect.X)
            {
                //Vertical edge segment
                QuadPart()
                .Texture(Tiles.TileMatrix(3))
                .Scale(Tiles.TileWidth, Tiles.TileHeight)
                .Translate(Rect.X, Rect.Y);
            }
            else
            {
                //Corner
                QuadPart()
                    .TileScaleAndTexture(Tiles, 0)
                    .Translate(Rect.X, Rect.Y);

                //Top edge
                QuadPart()
                    .Texture(Tiles.TileMatrix(1))
                    .Scale(TabX - Rect.X - Tiles.TileWidth, Tiles.TileHeight)
                    .Translate(Rect.X + Tiles.TileWidth, Rect.Y);

                //Interior corner
                QuadPart()
                    .Texture(Tiles.TileMatrix(9))
                    .Scale(Tiles.TileWidth, Tiles.TileHeight)
                    .Translate(TabX, Rect.Y);
            }

            //Top-right corner
            if (Rect.X + Rect.Width == TabX + TabWidth)
            {
                //Vertical edge segment
                QuadPart()
                    .Texture(Tiles.TileMatrix(5))
                    .Scale(Tiles.TileWidth, Tiles.TileHeight)
                    .Translate(Rect.Right - Tiles.TileWidth, Rect.Y);
            }
            else
            {
                //Corner
                QuadPart()
                    .TileScaleAndTexture(Tiles, 2)
                    .Translate(Rect.Right - Tiles.TileWidth, Rect.Y);

                //Top edge
                QuadPart()
                    .Texture(Tiles.TileMatrix(1))
                    .Scale(Rect.Right - TabX - TabWidth - Tiles.TileWidth, Tiles.TileHeight)
                    .Translate(TabX + TabWidth, Rect.Y);

                //Interior corner
                QuadPart()
                    .Texture(Tiles.TileMatrix(10))
                    .Scale(Tiles.TileWidth, Tiles.TileHeight)
                    .Translate(TabX + TabWidth - Tiles.TileWidth, Rect.Y);
            }

            //Top edge - bottom of tab
            QuadPart()
                .Texture(Tiles.TileMatrix(4))
                .Scale(TabWidth - (2 * Tiles.TileWidth), Tiles.TileHeight)
                .Translate(TabX + Tiles.TileWidth, Rect.Y);


            //Bottom-left corner
            QuadPart()
                .TileScaleAndTexture(Tiles, 6)
                .Translate(Rect.X, Rect.Bottom - Tiles.TileHeight);

            //Bottom-right corner
            QuadPart()
                .TileScaleAndTexture(Tiles, 8)
                .Translate(Rect.Right - Tiles.TileWidth, Rect.Bottom - Tiles.TileHeight);

            //Bottom edge
            QuadPart()
                .Texture(Tiles.TileMatrix(7))
                .Scale(Rect.Width - (2 * Tiles.TileWidth), Tiles.TileHeight)
                .Translate(Rect.X + Tiles.TileWidth, Rect.Bottom - Tiles.TileHeight);

            //Left edge
            QuadPart()
                .Texture(Tiles.TileMatrix(3))
                .Scale(Tiles.TileWidth, Rect.Height - (2 * Tiles.TileHeight))
                .Translate(Rect.X, Rect.Y + Tiles.TileHeight);

            //Right edge
            QuadPart()
                .Texture(Tiles.TileMatrix(5))
                .Scale(Tiles.TileWidth, Rect.Height - (2 * Tiles.TileHeight))
                .Translate(Rect.Right - Tiles.TileWidth, Rect.Y + Tiles.TileHeight);

            //Center
            QuadPart()
                .Texture(Tiles.TileMatrix(4))
                .Scale(Rect.Width - (2 * Tiles.TileWidth), Rect.Height - (2 * Tiles.TileHeight))
                .Translate(Rect.X + Tiles.TileWidth, Rect.Y + Tiles.TileHeight);

            r.VertexCount = this.VertexCount - r.VertexOffset;
            return r;
        }
    }
}