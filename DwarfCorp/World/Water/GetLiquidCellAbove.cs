using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public partial class LiquidCellHelpers
    {
        public static LiquidCellHandle GetLiquidCellAbove(LiquidCellHandle V)
        {
            if (!V.IsValid) return LiquidCellHandle.InvalidHandle;
            return new LiquidCellHandle(V.Chunk.Manager, new GlobalLiquidCoordinate(V.Coordinate.X, V.Coordinate.Y + 1, V.Coordinate.Z));
        }
    }
}
