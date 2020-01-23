using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui
{
    public enum Scale9Corners
    {
        Top = 1,
        Right = 2,
        Bottom = 4,
        Left = 8,
        All = Top | Right | Bottom | Left
    }

    public partial class Mesh
    {
        public static Mesh FittedSprite(Rectangle Rect, ITileSheet Tiles, int Tile)
        {
            return Quad()
                .Scale(Rect.Width, Rect.Height)
                .Translate(Rect.X, Rect.Y)
                .Texture(Tiles.TileMatrix(Tile));
        }

        public static MeshPart FittedSprite(Mesh Into, Rectangle Rect, ITileSheet Tiles, int Tile)
        {
            return Into.QuadPart()
                .Scale(Rect.Width, Rect.Height)
                .Translate(Rect.X, Rect.Y)
                .Texture(Tiles.TileMatrix(Tile));
        }

        // Tiles a sprite across a rect. Expensive!
        public static Mesh TiledSprite(Rectangle Rect, ITileSheet Tiles, int Tile)
        {
            var r = Mesh.EmptyMesh();
            TiledSprite(r, Rect, Tiles, Tile);
            return r;
        }

        // Tiles a sprite across a rect. Expensive!
        public static MeshPart TiledSprite(Mesh Into, Rectangle Rect, ITileSheet Tiles, int Tile)
        {
            var pos = new Point(Rect.X, Rect.Y);
            var r = new MeshPart { VertexOffset = Into.VertexCount, Mesh = Into };

            while (pos.X < Rect.Right)
            {
                while (pos.Y < Rect.Bottom)
                {
                    var quad = Into.QuadPart();
                    var size = new Point(Tiles.TileWidth, Tiles.TileHeight);

                    // Adjust texture coordinates if needed.
                    if (pos.Y + Tiles.TileHeight > Rect.Bottom)
                    {
                        size.Y = Rect.Bottom - pos.Y;
                        var ratio = (float)(size.Y) / (float)Tiles.TileHeight;
                        quad.MorphEx(v => { v.TextureCoordinate.Y *= ratio; return v; });
                    }

                    if (pos.X + Tiles.TileWidth > Rect.Right)
                    {
                        size.X = Rect.Right - pos.X;
                        var ratio = (float)(size.X) / (float)Tiles.TileWidth;
                        quad.MorphEx(v => { v.TextureCoordinate.X *= ratio; return v; });
                    }

                    quad.Scale(size.X, size.Y)
                        .Translate(pos.X, pos.Y)
                        .Texture(Tiles.TileMatrix(Tile));

                    pos.Y += Tiles.TileHeight;
                }
                pos.Y = Rect.Y;
                pos.X += Tiles.TileWidth;
            }

            r.VertexCount = Into.VertexCount - r.VertexOffset;
            return r;
        }

        /// <summary>
        /// Create a mesh for a scale9 background. This assumed the tilesheet is 3*3 and positions the 
        /// corners without scalling, scales the edges on one axis only, and fills the middle with the 
        /// center tile.
        /// </summary>
        /// <param name="Rect"></param>
        /// <param name="Tiles"></param>
        /// <param name="Corners"></param>
        /// <returns></returns>
        public static Mesh CreateScale9Background(
            Rectangle Rect,
            ITileSheet Tiles,
            Scale9Corners Corners = Scale9Corners.All)
        {
            var result = Mesh.EmptyMesh();
            CreateScale9Background(result, Rect, Tiles, Corners);
            return result;
        }

        /// <summary>
        /// Create a mesh for a scale9 background. This assumed the tilesheet is 3*3 and positions the 
        /// corners without scalling, scales the edges on one axis only, and fills the middle with the 
        /// center tile.
        /// </summary>
        /// <param name="Rect"></param>
        /// <param name="Tiles"></param>
        /// <param name="Corners"></param>
        /// <returns></returns>
        public static MeshPart CreateScale9Background(
            Mesh Into,
            Rectangle Rect,
            ITileSheet Tiles,
            Scale9Corners Corners = Scale9Corners.All)
        {
            var rects = new Rectangle[9];
            var margin = new Margin(0, 0, 0, 0);

            if (Corners.HasFlag(Scale9Corners.Left)) margin.Left = Tiles.TileWidth;
            if (Corners.HasFlag(Scale9Corners.Right)) margin.Right = Tiles.TileWidth;
            if (Corners.HasFlag(Scale9Corners.Top)) margin.Top = Tiles.TileHeight;
            if (Corners.HasFlag(Scale9Corners.Bottom)) margin.Bottom = Tiles.TileHeight;

            rects[0] = new Rectangle(Rect.Left, Rect.Top, margin.Left, margin.Top);
            rects[1] = new Rectangle(Rect.Left + margin.Left, Rect.Top, Rect.Width - margin.Left - margin.Right, margin.Top);
            rects[2] = new Rectangle(Rect.Right - margin.Right, Rect.Top, margin.Right, margin.Top);
            rects[3] = new Rectangle(Rect.Left, Rect.Top + margin.Top, margin.Left, Rect.Height - margin.Top - margin.Bottom);
            rects[4] = new Rectangle(Rect.Left + margin.Left, Rect.Top + margin.Top, Rect.Width - margin.Left - margin.Right, Rect.Height - margin.Top - margin.Bottom);
            rects[5] = new Rectangle(Rect.Right - margin.Right, Rect.Top + margin.Top, margin.Right, Rect.Height - margin.Top - margin.Bottom);
            rects[6] = new Rectangle(Rect.Left, Rect.Bottom - margin.Bottom, margin.Left, margin.Bottom);
            rects[7] = new Rectangle(Rect.Left + margin.Left, Rect.Bottom - margin.Bottom, Rect.Width - margin.Left - margin.Right, margin.Bottom);
            rects[8] = new Rectangle(Rect.Right - margin.Right, Rect.Bottom - margin.Bottom, margin.Right, margin.Bottom);

            var result = new MeshPart { VertexOffset = Into.VertexCount, Mesh = Into };

            for (var i = 0; i < 9; ++i)
                if (rects[i].Width != 0 && rects[i].Height != 0)
                {
                    if (Tiles.RepeatWhenUsedAsBorder)
                        TiledSprite(Into, rects[i], Tiles, i);
                    else
                        FittedSprite(Into, rects[i], Tiles, i);
                }

            result.VertexCount = Into.VertexCount - result.VertexOffset;
            return result;
        }

    }
}