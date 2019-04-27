using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static bool DoesVoxelHaveVisibleSurface(WorldManager World, VoxelHandle V)
        {
            if (!V.IsValid) return false;
            if (V.Coordinate.Y >= World.Master.MaxViewingLevel) return false;
            if (V.IsEmpty) return false;
            if (V.Coordinate.Y == World.Master.MaxViewingLevel - 1) return true;
            if (V.Coordinate.Y == World.WorldSizeInVoxels.Y - 1) return true;

            foreach (var neighborCoordinate in VoxelHelpers.EnumerateManhattanNeighbors(V.Coordinate))
            {
                var neighbor = new VoxelHandle(World.ChunkManager, neighborCoordinate);
                if (!neighbor.IsValid) return true;
                if (neighbor.IsEmpty && neighbor.IsExplored) return true;
            }

            return false;
        }
    }
}
