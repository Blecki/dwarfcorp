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
            public float AdvanceWidth;
            public float LeftBearing;
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
            public Dictionary<String, int> Kernings;
        }

        private TileSheet Sheet;
        private Dictionary<char, Glyph> Glyphs = new Dictionary<char, Glyph>();
        private Dictionary<String, int> Kernings;

        public void ResetAtlasBounds(Rectangle MyBounds, Rectangle AtlasBounds)
        {
            Sheet = new TileSheet(AtlasBounds.Width, AtlasBounds.Height, MyBounds, 1, 1, false);
        }

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
            {
                if (glyph.AdvanceWidth == 0)
                    glyph.AdvanceWidth = glyph.Width; // Fix fonts generated without full metadata
                Glyphs.Add(glyph.Code, glyph);
            }
            Kernings = atlas.Kernings;
            if (Kernings == null)
                Kernings = new Dictionary<string, int>();
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
            get { return Glyphs['I'].Height; }
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
            return MeasureString(WordWrapString(S, 1.0f, maxWidth, false));
        }

        public Point MeasureString(String S)
        {
            var size = new Point(0, TileHeight);
            float lineWidth = 0;
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
                    lineWidth += HasGlyph((int)(c - ' ')) ? GlyphAdvance((int)(c - ' ')) : 0;
                    if (lineWidth > size.X) size.X = (int)Math.Ceiling(lineWidth);
                }
            }
            return size;
        }

        public float GlyphAdvance(int Index)
        {
            return Glyphs[(char)(Index + ' ')].AdvanceWidth;
        }

        public float GlyphLeftBearing(int Index)
        {
            return Glyphs[(char)(Index + ' ')].LeftBearing;
        }

        public int GlyphKerning(int First, int Second)
        {
            var s = new String((char)First, (char)Second);
            if (Kernings.TryGetValue(s, out var k))
                return k;
            return 0;
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
                            lineLength += (HasGlyph(letter - ' ') ? GlyphAdvance(letter - ' ') : 0) * GlyphWidthScale;
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

            foreach(var c in S)
            {
                if (c == '\r' || c == '\t') continue;

                if (c == ' ' || c == '\n' || c == '-')
                {
                    if (w.Length == 0)
                    {
                        lineLength += Glyphs[' '].AdvanceWidth * GlyphWidthScale;
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
                            lineLength = wordLength + Glyphs[' '].AdvanceWidth * GlyphWidthScale;
                            wordLength = 0;
                            w.Clear();
                        }
                        else
                        {
                            r.Append(w);
                            lineLength += wordLength + Glyphs[' '].AdvanceWidth * GlyphWidthScale;
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
                    wordLength += (HasGlyph(c - ' ') ? GlyphAdvance(c - ' ') : 0) * GlyphWidthScale;
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
