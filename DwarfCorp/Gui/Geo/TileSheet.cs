using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui
{
    /// <summary>
    /// Calculates UV transformation matricies for tiles inside a tilesheet, that is itself a portion
    /// of a larger texture atlas.
    /// </summary>
    public class TileSheet : ITileSheet
    {
        public int TextureWidth { get; set; }
        public int TextureHeight { get; set; }
        public Rectangle SourceRect { get; set; }
        public int TileWidth { get; private set; }
        public int TileHeight { get; private set; }
        public bool RepeatWhenUsedAsBorder { get; private set; }

        public void ResetAtlasBounds(Rectangle MyBounds, Rectangle AtlasBounds)
        {
            TextureWidth = AtlasBounds.Width;
            TextureHeight = AtlasBounds.Height;
            SourceRect = MyBounds;
        }


        public float SourceURange { get { return (float)SourceRect.Width / (float)TextureWidth; } }
        public float SourceVRange { get { return (float)SourceRect.Height / (float)TextureHeight; } }
        public float SourceUOffset { get { return (float)SourceRect.X / (float)TextureWidth; } }
        public float SourceVOffset { get { return (float)SourceRect.Y / (float)TextureHeight; } }
        public int Columns { get { return SourceRect.Width / TileWidth; } }
        public int Rows { get { return SourceRect.Height / TileHeight; } }
        public int Row(int TileIndex) { return TileIndex / Columns; }
        public int Column(int TileIndex) { return TileIndex % Columns; }
        public float TileUStep { get { return SourceURange / Columns; } }
        public float TileVStep { get { return SourceVRange / Rows; } }
        public float ColumnU(int Column) { return SourceUOffset + (TileUStep * Column); }
        public float RowV(int Row) { return SourceVOffset + (TileVStep * Row); }
        public float TileU(int TileIndex) { return ColumnU(Column(TileIndex)); }
        public float TileV(int TileIndex) { return RowV(Row(TileIndex)); }

        public Vector4 MapRectangleToUVBounds(Rectangle R)
        {
            var x = (float)(R.X + SourceRect.X) / (float)TextureWidth;
            var y = (float)(R.Y + SourceRect.Y) / (float)TextureHeight;

            return new Vector4(x, y, x + ((float)R.Width / (float)TextureWidth), y + ((float)R.Height / (float)TextureHeight));
        }

        // Generate UV transform matricies that align the UV range 0..1 to a tile.
        public Matrix ScaleMatrix { get { return Matrix.CreateScale(TileUStep, TileVStep, 1.0f); } }
        public Matrix TranslationMatrix(int Column, int Row) { return Matrix.CreateTranslation(ColumnU(Column), RowV(Row), 0.0f); }
        public Matrix TileMatrix(int Column, int Row) { return ScaleMatrix * TranslationMatrix(Column % Columns, Row % Rows); }
        public Matrix TileMatrix(int TileIndex) { return TileMatrix(Column(TileIndex), Row(TileIndex)); }
        public Matrix TileMatrix(int TileIndex, int ColumnSpan, int RowSpan)
        {
            return Matrix.CreateScale(ColumnSpan, RowSpan, 1.0f) * TileMatrix(TileIndex);
        }

        public Matrix TileMatrix(int Column, int Row, int ColumnSpan, int RowSpan)
        {
            return Matrix.CreateScale(ColumnSpan, RowSpan, 1.0f) * TileMatrix(Column, Row);
        }

        public TileSheet(int TextureWidth, int TextureHeight, Rectangle Source, int TileWidth, int TileHeight, bool RepeatWhenUsedAsBorder)
        {
            this.TextureWidth = TextureWidth;
            this.TextureHeight = TextureHeight;
            this.TileWidth = TileWidth;
            this.TileHeight = TileHeight;
            this.SourceRect = Source;
            this.RepeatWhenUsedAsBorder = RepeatWhenUsedAsBorder;
        }

        public Point GlyphSize(int Index)
        {
            return new Point(TileWidth, TileHeight);
        }

        public int GlyphAdvance(int Index)
        {
            return TileWidth;
        }

        public int GlyphLeftBearing(int Index)
        {
            return 0;
        }

        public int GlyphKerning(int First, int Second)
        {
            return 0;
        }


        public Point MeasureString(String S)
        {
            return new Point(S.Length * TileWidth, TileHeight);
        }

        public bool HasGlyph(int Index)
        {
            return true;
        }

        public String WordWrapString(String S, float GlyphWidthScale, float Width, bool wrapWithinWords)
        {
            var r = new StringBuilder();
            var w = new StringBuilder();

            float lineLength = 0;
            float wordLength = 0;

            foreach (var c in S)
            {
                if (c == '\r' || c == '\t') continue;

                if (wrapWithinWords || ( c == ' ' || c == '\n'))
                {
                    if (w.Length == 0)
                    {
                        lineLength += TileWidth * GlyphWidthScale;
                    }
                    else
                    {
                        if (lineLength + wordLength > Width)
                        {
                            if (r.Length > 0)
                            {
                                if (c != ' ' && c != '\n' && c != '-')
                                {
                                    r.Append('-');
                                }
                                r.Append("\n");
                            }
                            r.Append(w);
                            lineLength = wordLength + TileWidth * GlyphWidthScale;
                            wordLength = 0;
                            w.Clear();
                        }
                        else
                        {
                            r.Append(w);
                            lineLength += wordLength + TileWidth * GlyphWidthScale;
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
                    wordLength += (HasGlyph(c) ? GlyphSize(c).X : 0) * GlyphWidthScale;
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
