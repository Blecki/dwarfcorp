using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FontBuilder
{ 
    public class VariableWidthBitmapFont
    {
        private static System.Drawing.Color Magenta = System.Drawing.Color.FromArgb(255, 255, 0, 255);
        private static System.Drawing.Color Black = System.Drawing.Color.FromArgb(255, 0, 0, 0);
        private static System.Drawing.Color Transparent = System.Drawing.Color.FromArgb(0, 0, 0, 0);

        public static Dictionary<char, System.Drawing.Bitmap> DecodeVariableWidthBitmapFont(System.Drawing.Bitmap SourceBitmap)
        {
            var glyphs = new List<Rectangle>();

            var x = 0;
            var y = 0;

            while (y < SourceBitmap.Height)
            {
                int glyphHeight = 1;

                while (x < SourceBitmap.Width)
                {
                    var pix = SourceBitmap.GetPixel(x, y);
                    if (pix == Magenta)
                        x += 1;
                    else
                    {
                        var glyph = ExtractRect(SourceBitmap, x, y);
                        glyphs.Add(glyph);
                        x += glyph.Width;
                        glyphHeight = glyph.Height;
                    }
                }

                x = 0;
                y += glyphHeight;
            }

            var r = new Dictionary<char, System.Drawing.Bitmap>();
            var c = ' ';

            foreach (var glyph in glyphs)
            {
                if (!IsEmptyGlyph(SourceBitmap, glyph))
                    r.Add(c, ExtractGlyph(SourceBitmap, glyph));
                c = (char)(c + 1);
            }

            return r;
        }

        /// <summary>
        /// Find the rectangle with origin at X, Y that is surrounded by magenta pixels
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns></returns>
        private static Rectangle ExtractRect(System.Drawing.Bitmap Data, int X, int Y)
        {
            var endX = X;
            var endY = Y;

            while (endX < Data.Width && Data.GetPixel(endX, Y) != Magenta)
                endX += 1;

            while (endY < Data.Height && Data.GetPixel(X, endY) != Magenta)
                endY += 1;

            var rHeight = endY - Y;
            return new Rectangle(X, Y, endX - X, endY - Y);
        }

        /// <summary>
        /// Returns false if any pixel in the rectangle is not black.
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Rect"></param>
        /// <returns></returns>
        private static bool IsEmptyGlyph(System.Drawing.Bitmap Data, Rectangle Rect)
        {
            for (var x = Rect.X; x < Rect.X + Rect.Width && x < Data.Width; ++x)
                for (var y = Rect.Y; y < Rect.Y + Rect.Height && y < Data.Height; ++y)
                    if (Data.GetPixel(x, y) != Black)
                        return false;
            return true;
        }

        private static System.Drawing.Bitmap ExtractGlyph(System.Drawing.Bitmap Source, Rectangle Rect)
        {
            var r = new System.Drawing.Bitmap(Rect.Width, Rect.Height);

            for (var x = 0; x < Rect.Width && (x + Rect.X) < Source.Width; ++x)
                for (var y = 0; y < Rect.Height && (y + Rect.Y) < Source.Height; ++y)
                {
                    var c = Source.GetPixel(x + Rect.X, y + Rect.Y);
                    if (c == Black)
                        r.SetPixel(x, y, Transparent);
                    else
                        r.SetPixel(x, y, c);
                }
            return r;
        }
    }
}
