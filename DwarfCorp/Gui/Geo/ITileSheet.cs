using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui
{
    public interface ITileSheet
    {
        Matrix TileMatrix(int TileID);
        int TileWidth { get; }
        int TileHeight { get; }
        Point GlyphSize(int TileID);
        int GlyphAdvance(int TileID); // How far to move to the right after drawing a glyph.
        int GlyphLeftBearing(int TileID);
        int GlyphKerning(int FirstID, int SecondID);
        Point MeasureString(String S);
        bool RepeatWhenUsedAsBorder { get; }
        bool HasGlyph(int TileID);
        Vector4 MapRectangleToUVBounds(Rectangle R);
        String WordWrapString(String S, float GlyphWidthScale, float Width, bool wrapWithinWords);

        void ResetAtlasBounds(Rectangle MyBounds, Rectangle AtlasBounds);
    }
}
