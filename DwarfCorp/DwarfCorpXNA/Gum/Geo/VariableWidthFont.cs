using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gum
{
    public class VariableWidthFont : ITileSheet
    {
        private TileSheet Sheet;
        private List<Rectangle> Glyphs;

        public bool RepeatWhenUsedAsBorder { get { return false; } }

        private class Texture
        {
            Color[] Data;
            public int Width {get; private set;}
            public int Height {get; private set;}

            public Color GetPixel(int X, int Y)
            {
                return Data[(Y * Width) + X];
            }

            public Texture(Texture2D Source)
            {
                Width = Source.Width;
                Height = Source.Height;
                Data = new Color[Width * Height];
                Source.GetData(Data);
            }
        }

        public VariableWidthFont(Texture2D Texture, int TextureWidth, int TextureHeight, Rectangle Source)
        {
            var Data = new Texture(Texture);
            Sheet = new TileSheet(TextureWidth, TextureHeight, Source, 1, 1, false);
            Glyphs = new List<Rectangle>();

            var x = 0;
            var y = 0;

            while (y < Data.Height)
            {
                int glyphHeight = 1;

                while (x < Data.Width)
                {
                    if (Data.GetPixel(x, y) == new Color(255,0,255,255))
                        x += 1;
                    else
                    {
                        var glyph = ExtractRect(Data, x, y);
                        Glyphs.Add(glyph);
                        x += glyph.Width;
                        glyphHeight = glyph.Height;
                    }
                }

                x = 0;
                y += glyphHeight;
            }
        }

        private Rectangle ExtractRect(Texture Data, int X, int Y)
        {
            var endX = X;
            var endY = Y;

            while (endX < Data.Width && Data.GetPixel(endX, Y) != new Color(255,0,255,255))
                endX += 1;

            while (endY < Data.Height && Data.GetPixel(X, endY) != new Color(255, 0, 255, 255))
                endY += 1;

            var rHeight = endY - Y;
            return new Rectangle(X, Y, endX - X, endY - Y);
        }

        public Matrix TileMatrix(int TileID)
        {
            var entry = Glyphs[TileID];
            return Sheet.TileMatrix(entry.X, entry.Y, entry.Width, entry.Height);
        }


        public int TileWidth
        {
            get { return Glyphs[0].Width; }
        }

        public int TileHeight
        {
            get { return Glyphs[0].Height; }
        }

        public Point GlyphSize(int Index)
        {
            return new Point(Glyphs[Index].Width, Glyphs[Index].Height);
        }

        public Point MeasureString(String S)
        {
            var size = new Point(0, TileHeight);
            var lineWidth = 0;
            foreach (var c in S)
            {
                if (c == '\n')
                {
                    size.Y += TileHeight;
                    lineWidth = 0;
                }
                else if (c < 32) continue;
                else
                {
                    lineWidth += GlyphSize(c - ' ').X;
                    if (lineWidth > size.X) size.X = lineWidth;
                }
            }
            return size;
        }
    }
}
