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
        public static Voxel GetVoxelWithMinimumFScore(PriorityQueue<Voxel> fScore, HashSet<Voxel> openSet)
        {
            return fScore.Dequeue();
        }


        public static List<Voxel> ReconstructPath(Dictionary<Voxel, Voxel> cameFrom, Voxel currentNode)
        {
            List<Voxel> toReturn = new List<Voxel>();
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


        private static bool Path(Voxel start, Voxel end, ChunkManager chunks, int maxExpansions, ref List<Voxel> toReturn, bool reverse)
        {
            VoxelChunk startChunk = chunks.ChunkData.ChunkMap[start.ChunkID];
            VoxelChunk endChunk = chunks.ChunkData.ChunkMap[end.ChunkID];

            if(startChunk.IsCompletelySurrounded(start) || endChunk.IsCompletelySurrounded(end))
            {
                toReturn = null;
                return false;
            }


            HashSet<Voxel> closedSet = new HashSet<Voxel>();

            HashSet<Voxel> openSet = new HashSet<Voxel>
            {
                start
            };

            Dictionary<Voxel, Voxel> cameFrom = new Dictionary<Voxel, Voxel>();
            Dictionary<Voxel, float> gScore = new Dictionary<Voxel, float>();
            PriorityQueue<Voxel> fScore = new PriorityQueue<Voxel>();
            gScore[start] = 0.0f;
            fScore.Enqueue(start, gScore[start] + Heuristic(start, end));

            int numExpansions = 0;
            while(openSet.Count > 0 && numExpansions < maxExpansions)
            {
                Voxel current = GetVoxelWithMinimumFScore(fScore, openSet);

                if (current == null)
                {
                    current = start;
                    numExpansions++;
                }

                //Drawer3D.DrawBox(current.GetBoundingBox(), Color.Red, 0.1f);

                numExpansions++;
                if ((current.Position - end.Position).LengthSquared() < 0.5f)
                {
                    toReturn = ReconstructPath(cameFrom, current);
                    return true;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                VoxelChunk currentChunk = chunks.ChunkData.ChunkMap[current.ChunkID];

                List<Voxel> neighbors = null;

                neighbors = !reverse ? currentChunk.GetMovableNeighbors(current) : currentChunk.GetReverseMovableNeighbors(current);

                List<Voxel> manhattanNeighbors = currentChunk.AllocateVoxels(6);
                currentChunk.GetNeighborsManhattan(current, manhattanNeighbors);

                if(manhattanNeighbors.Contains(end))
                {
                    List<Voxel> subPath = ReconstructPath(cameFrom, current);
                    subPath.Add(end);
                    toReturn = subPath;
                    return true;
                }

                foreach(Voxel n in neighbors)
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

        public static bool FindReversePath(Voxel start, Voxel end, ChunkManager chunks, int maxExpansions, ref List<Voxel> p)
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

        public static List<Voxel> FindPath(Voxel start, Voxel end, ChunkManager chunks, int maxExpansions)
        {
            List<Voxel> p = new List<Voxel>();
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
                List<Voxel> rp = new List<Voxel>();
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

        public static float GetDistance(Voxel a, Voxel b, ChunkManager chunks)
        {
            if(b.TypeName != "empty")
            {
                return 100000;
            }
            else
            {
                float score = (a.Position - b.Position).LengthSquared() + (Math.Abs((b.Position - a.Position).Y)) * 10;

                if(b.WaterLevel > 5)
                {
                    score += 5 + (Math.Abs((b.Position - a.Position).Y)) * 100;
                }

                return score;
            }
        }

        public static float Heuristic(Voxel a, Voxel b)
        {
            return (a.Position - b.Position).LengthSquared() + (Math.Abs((b.Position - a.Position).Y)) * 10;
        }
    }

}