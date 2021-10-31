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
    }
}
