using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// The classic A star planner, but singled out for use with voxels. It should
    /// probably be made generic.
    /// </summary>
    internal class AStarPlanner
    {
        public static VoxelRef GetVoxelWithMinimumFScore(PriorityQueue<VoxelRef> fScore, HashSet<VoxelRef> openSet)
        {
            return fScore.Dequeue();
        }


        public static List<VoxelRef> ReconstructPath(Dictionary<VoxelRef, VoxelRef> cameFrom, VoxelRef currentNode)
        {
            List<VoxelRef> toReturn = new List<VoxelRef>();
            if(cameFrom.ContainsKey(currentNode))
            {
                toReturn.AddRange(ReconstructPath(cameFrom, cameFrom[currentNode]));
                toReturn.Add(currentNode);
                return toReturn;
            }
            else
            {
                toReturn.Add(currentNode);
                return toReturn;
            }
        }


        private static bool Path(VoxelRef start, VoxelRef end, ChunkManager chunks, int maxExpansions, ref List<VoxelRef> toReturn, bool reverse)
        {
            VoxelChunk startChunk = chunks.ChunkData.ChunkMap[start.ChunkID];
            VoxelChunk endChunk = chunks.ChunkData.ChunkMap[end.ChunkID];

            if(startChunk.IsCompletelySurrounded(start) || endChunk.IsCompletelySurrounded(end))
            {
                toReturn = null;
                return false;
            }


            HashSet<VoxelRef> closedSet = new HashSet<VoxelRef>();

            HashSet<VoxelRef> openSet = new HashSet<VoxelRef>
            {
                start
            };

            Dictionary<VoxelRef, VoxelRef> cameFrom = new Dictionary<VoxelRef, VoxelRef>();
            Dictionary<VoxelRef, float> gScore = new Dictionary<VoxelRef, float>();
            PriorityQueue<VoxelRef> fScore = new PriorityQueue<VoxelRef>();
            gScore[start] = 0.0f;
            fScore.Enqueue(start, gScore[start] + Heuristic(start, end));

            int numExpansions = 0;
            while(openSet.Count > 0 && numExpansions < maxExpansions)
            {
                VoxelRef current = GetVoxelWithMinimumFScore(fScore, openSet);

                if(!current.IsValid)
                {
                    current = start;
                    numExpansions++;
                }

                //Drawer3D.DrawBox(current.GetBoundingBox(), Color.Red, 0.1f);

                numExpansions++;
                if((current.WorldPosition - end.WorldPosition).LengthSquared() < 0.5f)
                {
                    toReturn = ReconstructPath(cameFrom, current);
                    return true;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                VoxelChunk currentChunk = chunks.ChunkData.ChunkMap[current.ChunkID];

                List<VoxelRef> neighbors = null;

                neighbors = !reverse ? currentChunk.GetMovableNeighbors(current) : currentChunk.GetReverseMovableNeighbors(current);

                List<VoxelRef> manhattanNeighbors = currentChunk.GetNeighborsManhattan(current);

                if(manhattanNeighbors.Contains(end))
                {
                    List<VoxelRef> subPath = ReconstructPath(cameFrom, current);
                    subPath.Add(end);
                    toReturn = subPath;
                    return true;
                }

                foreach(VoxelRef n in neighbors)
                {
                    if(closedSet.Contains(n))
                    {
                        continue;
                    }

                    float tenativeGScore = gScore[current] + GetDistance(current, n, chunks);

                    if(openSet.Contains(n) && !(tenativeGScore < gScore[n]))
                    {
                        continue;
                    }

                    openSet.Add(n);
                    cameFrom[n] = current;
                    gScore[n] = tenativeGScore;
                    fScore.Enqueue(n, gScore[n] + Heuristic(n, end));
                }

                if(numExpansions >= maxExpansions)
                {
                    return false;
                }
            }
            toReturn = null;
            return false;
        }

        public static bool FindReversePath(VoxelRef start, VoxelRef end, ChunkManager chunks, int maxExpansions, ref List<VoxelRef> p)
        {
            bool success = Path(end, start, chunks, maxExpansions, ref p, true);

            if(p != null && success)
            {
                p.Reverse();
                return true;
            }
            else
            {
                return false;
            }
        }

        public static List<VoxelRef> FindPath(VoxelRef start, VoxelRef end, ChunkManager chunks, int maxExpansions)
        {
            List<VoxelRef> p = new List<VoxelRef>();
            bool success = Path(start, end, chunks, maxExpansions, ref p, false);

            if(success)
            {
                return p;
            }
            else
            {
                return null;
            }

            /*
            if(p == null || !success)
            {
                List<VoxelRef> rp = new List<VoxelRef>();
                if(FindReversePath(start, end, chunks, maxExpansions, ref rp))
                {
                    return rp;
                }
                else
                {
                    return p;
                }
            }
            else
            {
                return p;
            }
             */
        }

        public static float GetDistance(VoxelRef a, VoxelRef b, ChunkManager chunks)
        {
            if(b.TypeName != "empty")
            {
                return 100000;
            }
            else
            {
                float score = (a.WorldPosition - b.WorldPosition).LengthSquared() + (Math.Abs((b.WorldPosition - a.WorldPosition).Y)) * 10;

                if(b.GetWaterLevel(chunks) > 5)
                {
                    score += 5 + (Math.Abs((b.WorldPosition - a.WorldPosition).Y)) * 100;
                }

                return score;
            }
        }

        public static float Heuristic(VoxelRef a, VoxelRef b)
        {
            return (a.WorldPosition - b.WorldPosition).LengthSquared() + (Math.Abs((b.WorldPosition - a.WorldPosition).Y)) * 10;
        }
    }

}