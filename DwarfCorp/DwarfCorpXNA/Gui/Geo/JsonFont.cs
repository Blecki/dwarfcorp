using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.Gui
{
    public class JsonFont : ITileSheet
    {
        public class Glyph
        {
            public char Code;
            public int X;
            public int Y;
            public int Width;
            public int Height;
        }

        public struct _Rect
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
        }

        public class Atlas
        {
            public _Rect Dimensions;
            public List<Glyph> Glyphs;
        }

        private TileSheet Sheet;
        private Dictionary<char, Glyph> Glyphs = new Dictionary<char, Glyph>();

        public bool RepeatWhenUsedAsBorder { get { return false; } }

        public Vector4 MapRectangleToUVBounds(Rectangle R)
        {
            // This operation is pretty much nonsense for a font, but, whatever. Do it.
            return Sheet.MapRectangleToUVBounds(R);
        }

        public JsonFont(String AssetPath, Rectangle AtlasTexture, Rectangle Source)
        {
            Sheet = new TileSheet(AtlasTexture.Width, AtlasTexture.Height, Source, 1, 1, false);

            var atlas = FileUtils.LoadJsonFromResolvedPath<Atlas>(AssetPath + "_def.font");
            foreach (var glyph in atlas.Glyphs)
                Glyphs.Add(glyph.Code, glyph);
        }

        public Matrix TileMatrix(int TileID)
        {
            var entry = Glyphs[(char)(TileID + ' ')];
            return Sheet.TileMatrix(entry.X, entry.Y, entry.Width, entry.Height);
        }

        public int TileWidth
        {
            get { return Glyphs.First().Value.Width; }
        }

        public int TileHeight
        {
            get { return Glyphs.First().Value.Height; }
        }

        public Point GlyphSize(int Index)
        {
            return new Point(Glyphs[(char)(Index + ' ')].Width, Glyphs[(char)(Index + ' ')].Height);
        }

        public bool HasGlyph(int Index)
        {
            return Glyphs.ContainsKey((char)(Index + ' '));
        }

        public Point MeasureString(String S, float maxWidth)
        {
            return MeasureString(WordWrapString(S, 1.0f, maxWidth));
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
                    lineWidth += HasGlyph((int)(c - ' ')) ? GlyphSize((int)(c - ' ')).X : 0;
                    if (lineWidth > size.X) size.X = lineWidth;
                }
            }
            return size;
        }

        public String WordWrapString(String S, float GlyphWidthScale, float Width)
        {
            var r = new StringBuilder();
            var w = new StringBuilder();

            float lineLength = 0;
            float wordLength = 0;

            foreach(var c in S)
            {
                if (c == '\r' || c == '\t') continue;

                if (c == ' ' || c == '\n')
                {
                    if (w.Length == 0)
                    {
                        lineLength += Glyphs[' '].Width * GlyphWidthScale;
                    }
                    else
                    {
                        if (lineLength + wordLength > Width)
                        {
                            if (r.Length > 0)
                                r.Append("\n");
                            r.Append(w);
                            lineLength = wordLength + Glyphs[' '].Width * GlyphWidthScale;
                            wordLength = 0;
                            w.Clear();
                        }
                        else
                        {
                            r.Append(w);
                            lineLength += wordLength + Glyphs[' '].Width * GlyphWidthScale;
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
                    if (wordLength > Width)
                    {
                        r.Append("\n");
                        r.Append(w);
                        w.Clear();
                        wordLength = 0;
                    }
                }
            }

            if (w.Length != 0)
            {
                if (lineLength + wordLength > Width && r.Length > 0)
                    r.Append("\n");
                r.Append(w);
            }

            return r.ToString();
        }
    }
}
