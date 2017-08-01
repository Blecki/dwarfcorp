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

                foreach (var neighborCoordinate in VoxelHelpers.EnumerateManhattanNeighbors(v.Coordinate))
                {
                    var neighbor = new TemporaryVoxelHandle(Data, neighborCoordinate);
                    if (!neighbor.IsValid) continue;
                    if (neighbor.IsExplored) continue;

                    neighbor.Chunk.NotifyExplored(neighbor.Coordinate.GetLocalVoxelCoordinate());
                    neighbor.IsExplored = true;

                    if (neighbor.IsEmpty)
                        queue.Enqueue(neighbor);
                }

                v.IsExplored = true;
            }
        }

        /// <summary>
        /// Run the reveal algorithm without invoking the invalidation mechanism.
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="voxels"></param>
        public static void InitialReveal(
            ChunkData Data,
            TemporaryVoxelHandle voxel)
        {
            // Fog of war must be on for the initial reveal to avoid artifacts.
            bool fogOfWar = GameSettings.Default.FogofWar;
            GameSettings.Default.FogofWar = true;

            var queue = new Queue<TemporaryVoxelHandle>(128);
            queue.Enqueue(voxel);

            while (queue.Count > 0)
            {
                var v = queue.Dequeue();
                if (!v.IsValid) continue;

                foreach (var neighborCoordinate in VoxelHelpers.EnumerateManhattanNeighbors(v.Coordinate))
                {
                    var neighbor = new TemporaryVoxelHandle(Data, neighborCoordinate);
                    if (!neighbor.IsValid) continue;
                    if (neighbor.IsExplored) continue;

                    neighbor.Chunk.NotifyExplored(neighbor.Coordinate.GetLocalVoxelCoordinate());
                    neighbor.RawSetIsExplored(true);

                    if (neighbor.IsEmpty)
                        queue.Enqueue(neighbor);
                }

                v.RawSetIsExplored(true);
            }

            GameSettings.Default.FogofWar = fogOfWar;
        }
    }
}
