using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class LiquidCellHelpers
    {
        public static IEnumerable<LiquidCellHandle> EnumerateCellsInVoxel(VoxelHandle V)
        {
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(0, 0, 0));
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(1, 0, 0));
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(0, 1, 0));
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(1, 1, 0));
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(0, 0, 1));
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(1, 0, 1));
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(0, 1, 1));
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(1, 1, 1));
        }

        public static IEnumerable<LiquidCellHandle> EnumerateCellsInTopOfVoxel(VoxelHandle V)
        {
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(0, 1, 0));
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(1, 1, 0));
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(0, 1, 1));
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(1, 1, 1));
        }

        public static IEnumerable<LiquidCellHandle> EnumerateCellsInBottomOfVoxel(VoxelHandle V)
        {
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(0, 0, 0));
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(1, 0, 0));
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(0, 0, 1));
            yield return LiquidCellHandle.UnsafeCreateLocalHandle(V.Chunk, V.Coordinate.GetLocalVoxelCoordinate().ToLocalLiquidCoordinate().Offset(1, 0, 1));
        }

        public static IEnumerable<LiquidCellHandle> EnumerateEmptyCellsInVoxel(VoxelHandle V)
        {
            foreach (var liquidCell in EnumerateCellsInVoxel(V))
                if (liquidCell.LiquidType == 0)
                    yield return liquidCell;
        }

        public static IEnumerable<LiquidCellHandle> EnumerateFilledCellsInVovel(VoxelHandle V)
        {
            foreach (var liquidCell in EnumerateCellsInVoxel(V))
                if (liquidCell.LiquidType != 0)
                    yield return liquidCell;
        }
    }
}
