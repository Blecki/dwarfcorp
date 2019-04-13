using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        /// <summary>
        /// Run the reveal algorithm without invoking the invalidation mechanism.
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="voxel"></param>
        public static void InitialReveal(
            ChunkManager Manager,
            ChunkData Data,
            VoxelHandle voxel)
        {
            // Fog of war must be on for the initial reveal to avoid artifacts.
            bool fogOfWar = GameSettings.Default.FogofWar;
            GameSettings.Default.FogofWar = true;

            var queue = new Queue<VoxelHandle>(128);
            queue.Enqueue(voxel);

            while (queue.Count > 0)
            {
                var v = queue.Dequeue();
                if (!v.IsValid) continue;

                foreach (var neighborCoordinate in VoxelHelpers.EnumerateManhattanNeighbors(v.Coordinate))
                {
                    var neighbor = new VoxelHandle(Data, neighborCoordinate);
                    if (!neighbor.IsValid) continue;
                    if (neighbor.IsExplored) continue;

                    // We are skipping the invalidation mechanism but still need to trigger events.
                    // Because of the shear number of voxel revelations that get queued up - and the fact that this runs last while loading - we can go ahead and
                    //   trigger entities now. Hopefully.
                    //Manager.NotifyChangedVoxel(new VoxelChangeEvent
                    //{
                    //    Type = VoxelChangeEventType.Explored,
                    //    Voxel = neighbor
                    //});


                    neighbor.RawSetIsExplored();

                    var box = neighbor.GetBoundingBox();
                    var hashmap = Manager.World.EnumerateIntersectingObjects(box, CollisionType.Both);

                    foreach (var intersectingBody in hashmap)
                    {
                        var listener = intersectingBody as IVoxelListener;
                        if (listener != null)
                            listener.OnVoxelChanged(new VoxelChangeEvent
                            {
                                Type = VoxelChangeEventType.Explored,
                                Voxel = neighbor
                            });
                    }

                    Manager.World.Master.TaskManager.OnVoxelChanged(new VoxelChangeEvent
                    {
                        Type = VoxelChangeEventType.Explored,
                        Voxel = neighbor
                    });

                    if (neighbor.IsEmpty)
                        queue.Enqueue(neighbor);
                }

                v.RawSetIsExplored();
            }

            GameSettings.Default.FogofWar = fogOfWar;
        }

        private struct VisitedVoxel
        {
            public int Depth;
            public VoxelHandle Voxel;
        }

        public static void RadiusReveal(
            ChunkData Data,
            VoxelHandle Voxel,
            int Radius)
        {
            var queue = new Queue<VisitedVoxel>(128);
            var visited = new HashSet<ulong>();

            if (Voxel.IsValid)
            {
                queue.Enqueue(new VisitedVoxel { Depth = 1, Voxel = Voxel });
                Voxel.IsExplored = true;
            }

            while (queue.Count > 0)
            {
                var v = queue.Dequeue();
                if (v.Depth >= Radius) continue;
                
                foreach (var neighborCoordinate in VoxelHelpers.EnumerateManhattanNeighbors(v.Voxel.Coordinate))
                {
                    var neighbor = new VoxelHandle(Data, neighborCoordinate);
                    if (!neighbor.IsValid) continue;

                    var longHash = neighborCoordinate.GetLongHash();
                    if (visited.Contains(longHash)) continue;
                    visited.Add(longHash);

                    neighbor.IsExplored = true;
 
                    if (neighbor.IsEmpty)
                        queue.Enqueue(new VisitedVoxel
                        {
                            Depth = v.Depth + 1,
                            Voxel = neighbor,
                        });
                }
            }
        }
    }
}
