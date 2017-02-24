using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Gum
{
    public interface ITileSheet
    {
        Matrix TileMatrix(int TileID);
        int TileWidth { get; }
        int TileHeight { get; }
        Point GlyphSize(int TileID);
        Point MeasureString(String S);
        bool RepeatWhenUsedAsBorder { get; }
    }
}
