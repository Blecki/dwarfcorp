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
        Point MeasureString(String S);
        bool RepeatWhenUsedAsBorder { get; }
        bool HasGlyph(int TileID);
        Vector4 MapRectangleToUVBounds(Rectangle R);
        String WordWrapString(String S, float GlyphWidthScale, float Width, bool wrapWithinWords);

        void ResetAtlasBounds(Rectangle MyBounds, Rectangle AtlasBounds);
    }
}
