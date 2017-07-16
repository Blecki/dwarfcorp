using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static void Reveal(
            ChunkData Data,
            TemporaryVoxelHandle Start)
        {
            var x = 0;
            if (!Start.IsEmpty)
                x = 5;

            Reveal(Data, new TemporaryVoxelHandle[] { Start });
        }

        public static void Reveal(
            ChunkData Data,
            IEnumerable<TemporaryVoxelHandle> voxels)
        {
            //if (!GameSettings.Default.FogofWar) return;

            var queue = new Queue<TemporaryVoxelHandle>(128);

            foreach (var voxel in voxels)
                if (voxel.IsValid)
                    queue.Enqueue(voxel);

            while (queue.Count > 0)
            {
                var v = queue.Dequeue();
                if (!v.IsValid) continue;

                foreach (var neighborCoordinate in Neighbors.EnumerateManhattanNeighbors(v.Coordinate))
                {
                    var neighbor = new TemporaryVoxelHandle(Data, neighborCoordinate);
                    if (!neighbor.IsValid) continue;
                    if (neighbor.IsExplored) continue;

                    neighbor.Chunk.NotifyExplored(neighbor.Coordinate.GetLocalVoxelCoordinate());
                    neighbor.IsExplored = true;

                    if (neighbor.IsEmpty)
                        queue.Enqueue(neighbor);

                    neighbor.Chunk.ShouldRebuild = true;
                    neighbor.Chunk.ShouldRebuildWater = true;
                    neighbor.Chunk.ShouldRecalculateLighting = true;
                }

                v.IsExplored = true;
            }
        }
    }
}
