﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class LiquidCellHelpers
    {
        public static void FillVoxelWithLiquid(VoxelHandle V, byte Liquid)
        {
            foreach (var liquidCell in EnumerateCellsInVoxel(V))
            {
                var l = liquidCell;
                l.LiquidType = Liquid;
            }
        }


        public static void FillBottomOfVoxelWithLiquid(VoxelHandle V, byte Liquid)
        {
            foreach (var liquidCell in EnumerateCellsInBottomOfVoxel(V))
            {
                var l = liquidCell;
                l.LiquidType = Liquid;
            }
        }

        public static void FillVoxelWithLiquidAndWake(WorldManager World, VoxelHandle V, byte Liquid)
        {
            foreach (var liquidCell in EnumerateCellsInVoxel(V))
            {
                var l = liquidCell;
                l.LiquidType = Liquid;
                World.ChunkManager.Water.EnqueueDirtyCell(liquidCell);
            }
        }

        public static void ClearVoxelOfLiquid(VoxelHandle V)
        {
            foreach (var liquidCell in EnumerateCellsInVoxel(V))
            {
                var l = liquidCell;
                l.LiquidType = 0;
                l.OceanFlag = 0;
                
            }
        }

        public static void ClearVoxelOfLiquid(WorldManager World, VoxelHandle V)
        {
            foreach (var liquidCell in EnumerateCellsInVoxel(V))
            {
                var l = liquidCell;
                l.LiquidType = 0;
                l.OceanFlag = 0;
                World.ChunkManager.Water.EnqueueDirtyCell(liquidCell);
            }
        }
    }
}
