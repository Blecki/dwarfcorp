﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.Gui
{
    public class Kerning
    {
        public String Key;
        public int Distance;
    }

    public class VariableWidthFont : ITileSheet
    {
        private TileSheet Sheet;
        private List<Rectangle> Glyphs;
        
        public bool RepeatWhenUsedAsBorder { get { return false; } }

        public void ResetAtlasBounds(Rectangle MyBounds, Rectangle AtlasBounds)
        {
            Sheet = new TileSheet(AtlasBounds.Width, AtlasBounds.Height, MyBounds, 1, 1, false);
        }

        public Vector4 MapRectangleToUVBounds(Rectangle R)
        {
            // This operation is pretty much nonsense for a font, but, whatever. Do it.
            return Sheet.MapRectangleToUVBounds(R);
        }

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
            bool allZeros = true;
            while (y < Data.Height)
            {
                int glyphHeight = 1;

                while (x < Data.Width)
                {
                    var pix = Data.GetPixel(x, y);
                    if (pix != new Color(0, 0, 0, 0))
                        allZeros = false;
                    if (pix == new Color(255,0,255,255))
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
            if (allZeros)
            {
                Console.Out.WriteLine("Failed to load font {0} something wrong?", Texture.Name);
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
            if (TileID < 0 || TileID >= Glyphs.Count)
                return Matrix.Identity;
            var entry = Glyphs[TileID];
            return Sheet.TileMatrix(entry.X, entry.Y, entry.Width, entry.Height);
        }

        public int TileWidth
        {
            get { return Glyphs.Count > 0 ? Glyphs[0].Width : 1; }
        }

        public int TileHeight
        {
            get { return Glyphs.Count > 0 ? Glyphs[0].Height : 1; }
        }

        public Point GlyphSize(int Index)
        {
            return Index >= 0 && Index < Glyphs.Count ? new Point(Glyphs[Index].Width, Glyphs[Index].Height) : new Point(1, 1);
        }

        public float GlyphAdvance(int Index)
        {
            return Index >= 0 && Index < Glyphs.Count ? Glyphs[Index].Width : 1;
        }

        public float GlyphLeftBearing(int Index)
        {
            return 0;
        }

        public int GlyphKerning(int First, int Second)
        {
            return 0;
        }
        
        public bool HasGlyph(int Index)
        {
            return Index >= 0 && Index < Glyphs.Count;
        }

        public Point MeasureString(String S, float maxWidth)
        {
            return MeasureString(WordWrapString(S, 1.0f, maxWidth, false));
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
                    lineWidth += HasGlyph(c - ' ') ? GlyphSize(c - ' ').X : 0;
                    if (lineWidth > size.X) size.X = lineWidth;
                }
            }
            return size;
        }

        public String WordWrapString(String S, float GlyphWidthScale, float Width, bool wrapWithinWords)
        {
            var r = new StringBuilder();
            var w = new StringBuilder();

            float lineLength = 0;
            float wordLength = 0;

            Func<bool> wrapWord = () =>
            {
                if (lineLength + wordLength > Width)
                {
                    if (!wrapWithinWords && r.Length > 0)
                    {
                        r.Append("\n");
                        r.Append(w);
                    }
                    else if (wrapWithinWords)
                    {
                        if (r.Length > 0)
                        {
                            r.Append("\n");
                            lineLength = 0;
                        }

                        wordLength = 0;
                        foreach (var letter in w.ToString())
                        {
                            lineLength += (HasGlyph(letter - ' ') ? GlyphSize(letter - ' ').X : 0) * GlyphWidthScale;
                            if (lineLength > Width)
                            {
                                r.Append("\n-");
                                r.Append(letter);
                                lineLength = 0;
                            }
                            else
                            {
                                r.Append(letter);
                            }
                        }
                    }
                    else
                    {
                        r.Append(w);
                    }
                }
                else
                {
                    r.Append(w);
                }
                return true;
            };

            foreach (var c in S)
            {
                if (c == '\r' || c == '\t') continue;

                if (c == ' ' || c == '\n' || c == '-')
                {
                    if (w.Length == 0)
                    {
                        lineLength += Glyphs[0].Width * GlyphWidthScale;
                    }
                    else
                    {
                        if (lineLength + wordLength > Width)
                        {
                            if (r.Length > 0)
                            {
                                r.Append("\n");
                                lineLength = 0;
                            }
                            wrapWord();
                            lineLength = wordLength + Glyphs[0].Width * GlyphWidthScale;
                            wordLength = 0;
                            w.Clear();
                        }
                        else
                        {
                            r.Append(w);
                            lineLength += wordLength + Glyphs[0].Width * GlyphWidthScale;
                            wordLength = 0;
                            w.Clear();
                        }
                    }

                    r.Append(c);
                    if (c == '\n')
                        lineLength = 0;
                }
                else
                {
                    w.Append(c);
                    wordLength += (HasGlyph(c - ' ') ? GlyphSize(c - ' ').X : 0) * GlyphWidthScale;
                }
            }

            if (w.Length != 0)
            {
                wrapWord();
            }

            return r.ToString();
        }
    }
}
