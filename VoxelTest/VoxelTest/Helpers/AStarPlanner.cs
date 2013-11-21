using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    internal class AStarPlanner
    {
        public static VoxelRef GetVoxelWithMinimumFScore(PriorityQueue<VoxelRef> f_score, HashSet<VoxelRef> openSet)
        {
            return f_score.Dequeue();
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
            VoxelChunk startChunk = chunks.ChunkMap[start.ChunkID];
            VoxelChunk endChunk = chunks.ChunkMap[end.ChunkID];

            if(startChunk.IsCompletelySurrounded(start) || endChunk.IsCompletelySurrounded(end))
            {
                toReturn = null;
                return false;
            }


            HashSet<VoxelRef> closedSet = new HashSet<VoxelRef>();

            HashSet<VoxelRef> openSet = new HashSet<VoxelRef>();
            openSet.Add(start);

            Dictionary<VoxelRef, VoxelRef> cameFrom = new Dictionary<VoxelRef, VoxelRef>();
            Dictionary<VoxelRef, float> g_score = new Dictionary<VoxelRef, float>();
            PriorityQueue<VoxelRef> f_score = new PriorityQueue<VoxelRef>();
            g_score[start] = 0.0f;
            f_score.Enqueue(start, g_score[start] + Heuristic(start, end));

            VoxelRef current = start;

            int numExpansions = 0;
            while(openSet.Count > 0 && numExpansions < maxExpansions)
            {
                current = GetVoxelWithMinimumFScore(f_score, openSet);

                if(!current.isValid)
                {
                    current = start;
                    numExpansions++;
                }

                //SimpleDrawing.DrawBox(current.GetBoundingBox(), Color.Red, 0.1f);

                numExpansions++;
                if((current.WorldPosition - end.WorldPosition).LengthSquared() < 0.5f)
                {
                    toReturn = ReconstructPath(cameFrom, current);
                    return true;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                VoxelChunk currentChunk = chunks.ChunkMap[current.ChunkID];

                List<VoxelRef> neighbors = null;

                if(!reverse)
                {
                    neighbors = currentChunk.GetMovableNeighbors(current);
                }
                else
                {
                    neighbors = currentChunk.GetReverseMovableNeighbors(current);
                }

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

                    float tenative_g_score = g_score[current] + GetDistance(current, n, chunks);

                    if(!openSet.Contains(n) || tenative_g_score < g_score[n])
                    {
                        openSet.Add(n);
                        cameFrom[n] = current;
                        g_score[n] = tenative_g_score;
                        f_score.Enqueue(n, g_score[n] + Heuristic(n, end));
                    }
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
        }

        public static float GetDistance(VoxelRef A, VoxelRef B, ChunkManager chunks)
        {
            if(B.TypeName != "empty")
            {
                return 100000;
            }
            else
            {
                float score = (A.WorldPosition - B.WorldPosition).LengthSquared() + (Math.Abs((B.WorldPosition - A.WorldPosition).Y)) * 10;

                if(B.GetWaterLevel(chunks) > 5)
                {
                    score += 5 + (Math.Abs((B.WorldPosition - A.WorldPosition).Y)) * 100;
                }

                return score;
            }
        }

        public static float Heuristic(VoxelRef A, VoxelRef B)
        {
            return (A.WorldPosition - B.WorldPosition).LengthSquared() + (Math.Abs((B.WorldPosition - A.WorldPosition).Y)) * 10;
        }
    }

}