using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class LiquidCellHelpers
    {
        public static bool AnyLiquidInVoxel(VoxelHandle V)
        {
            foreach (var liquidCell in EnumerateCellsInVoxel(V))
                if (liquidCell.LiquidType != 0) return true;
            return false;
        }

        public static bool AnyLiquidInVoxel(VoxelHandle V, byte ltype)
        {
            foreach (var liquidCell in EnumerateCellsInVoxel(V))
                if (liquidCell.LiquidType == ltype) return true;
            return false;
        }

        public static bool AnyLiquidInTopOfVoxel(VoxelHandle V)
        {
            foreach (var liquidCell in EnumerateCellsInTopOfVoxel(V))
                if (liquidCell.LiquidType != 0) return true;
            return false;
        }

        public static int CountCellsWithWater(VoxelHandle V)
        {
            var c = 0;
            foreach (var liquidCell in EnumerateCellsInVoxel(V))
                if (liquidCell.LiquidType != 0) c += 1;
            return c;
        }

        private static byte FindMaxLiquid(Dictionary<byte, int> Counts)
        {
            var current = (byte)0;
            var currentCount = 0;
            foreach (var l in Counts)
                if (l.Value > currentCount)
                {
                    current = l.Key;
                    currentCount = l.Value;
                }
            return current;
        }

        public static byte MedianLiquidInVoxel(VoxelHandle V)
        {
            var LiquidCounts = new Dictionary<byte, int>();
            foreach (var liquidCell in EnumerateCellsInVoxel(V))
            {
                if (!LiquidCounts.ContainsKey(liquidCell.LiquidType))
                    LiquidCounts[liquidCell.LiquidType] = 1;
                else
                    LiquidCounts[liquidCell.LiquidType] += 1;
            }
            return (byte)FindMaxLiquid(LiquidCounts);
        }
    }
}
