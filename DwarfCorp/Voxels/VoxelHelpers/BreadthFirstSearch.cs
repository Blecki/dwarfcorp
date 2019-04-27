using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {

        public static IEnumerable<GlobalVoxelCoordinate> BreadthFirstSearchNonBlocking(
            ChunkManager Data,
            GlobalVoxelCoordinate Start,
            float Radius,
            Func<GlobalVoxelCoordinate, bool> IsGoal)
        {
            var queue = new Queue<GlobalVoxelCoordinate>();
            var visited = new HashSet<GlobalVoxelCoordinate>();
            var radiusSquared = Radius * Radius;

            queue.Enqueue(Start);
            visited.Add(Start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (IsGoal(current))
                {
                    yield return current;
                    yield break;
                }

                var delta = current.ToVector3() - Start.ToVector3();
                if (delta.LengthSquared() < radiusSquared)
                {
                    foreach (var neighbor in VoxelHelpers.EnumerateManhattanNeighbors(current))
                    {
                        var v = new VoxelHandle(Data, neighbor);
                        if (!visited.Contains(neighbor) && v.IsValid && v.IsEmpty)
                        {
                            if (Debugger.Switches.DrawPaths)
                            {
                                Drawer3D.DrawBox(v.GetBoundingBox(), Color.Red, 0.1f, true);
                            }
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
                yield return new GlobalVoxelCoordinate(-9999, -9999, -9999);
            }
        }

        public static bool BreadthFirstSearch(
            ChunkManager Data,
            GlobalVoxelCoordinate Start,
            float Radius,
            Func<GlobalVoxelCoordinate, bool> IsGoal,
            out GlobalVoxelCoordinate Result)
        {
            var queue = new Queue<GlobalVoxelCoordinate>();
            var visited = new HashSet<GlobalVoxelCoordinate>();
            var radiusSquared = Radius * Radius;

            queue.Enqueue(Start);
            visited.Add(Start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (IsGoal(current))
                {
                    Result = current;
                    return true;
                }

                var delta = current.ToVector3() - Start.ToVector3();
                if (delta.LengthSquared() < radiusSquared)
                {
                    foreach (var neighbor in VoxelHelpers.EnumerateManhattanNeighbors(current))
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                }
            }

            Result = new GlobalVoxelCoordinate(0, 0, 0);
            return false;
        }
    }
}
