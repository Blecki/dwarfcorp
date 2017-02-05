using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public static class TileSheetExtension
    {
        public static int ConvertRectToIndex(this Gum.TileSheet Sheet, Rectangle Rect)
        {
            var x = Rect.X / Sheet.TileWidth;
            var y = Rect.Y / Sheet.TileHeight;
            return (Sheet.Columns * y) + x;
        }
    }
}
