using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static bool BreadthFirstSearch(
            ChunkData Data,
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
                    foreach (var neighbor in Neighbors.EnumerateManhattanNeighbors(current))
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
