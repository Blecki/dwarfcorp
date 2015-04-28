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


        public static List<Creature.MoveAction> ReconstructPath(Dictionary<Voxel, Creature.MoveAction> cameFrom, Creature.MoveAction currentNode)
        {
            List<Creature.MoveAction> toReturn = new List<Creature.MoveAction>();
            if(cameFrom.ContainsKey(currentNode.Voxel))
            {
                toReturn.AddRange(ReconstructPath(cameFrom, cameFrom[currentNode.Voxel]));
                toReturn.Add(currentNode);
                return toReturn;
            }
            else
            {
                toReturn.Add(currentNode);
                return toReturn;
            }
        }


        private static bool Path(Voxel start, GoalRegion goal, ChunkManager chunks, int maxExpansions, ref List<Creature.MoveAction> toReturn, bool reverse)
        {
            VoxelChunk startChunk = chunks.ChunkData.ChunkMap[start.ChunkID];
            VoxelChunk endChunk = chunks.ChunkData.ChunkMap[goal.GetVoxel().ChunkID];

            if(startChunk.IsCompletelySurrounded(start) || endChunk.IsCompletelySurrounded(goal.GetVoxel()))
            {
                toReturn = null;
                return false;
            }


            HashSet<Voxel> closedSet = new HashSet<Voxel>();

            HashSet<Voxel> openSet = new HashSet<Voxel>
            {
                start
            };

            Dictionary<Voxel, Creature.MoveAction> cameFrom = new Dictionary<Voxel, Creature.MoveAction>();
            Dictionary<Voxel, float> gScore = new Dictionary<Voxel, float>();
            PriorityQueue<Voxel> fScore = new PriorityQueue<Voxel>();
            gScore[start] = 0.0f;
            fScore.Enqueue(start, gScore[start] + Heuristic(start, goal.GetVoxel()));

            int numExpansions = 0;
            while(openSet.Count > 0 && numExpansions < maxExpansions)
            {
                Voxel current = GetVoxelWithMinimumFScore(fScore, openSet);

                if (current == null)
                {
                    current = start;
                    numExpansions++;
                }
                numExpansions++;
                if (goal.IsInGoalRegion(current))
                {
                    Creature.MoveAction first = new Creature.MoveAction()
                    {
                        Voxel = current,
                        MoveType = Creature.MoveType.Walk
                    };
                    toReturn = ReconstructPath(cameFrom, first);
                    return true;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                VoxelChunk currentChunk = chunks.ChunkData.ChunkMap[current.ChunkID];

                List<Creature.MoveAction> neighbors = null;

                neighbors = currentChunk.GetMovableNeighbors(current);

                List<Voxel> manhattanNeighbors = currentChunk.AllocateVoxels(6);
                currentChunk.GetNeighborsManhattan(current, manhattanNeighbors);

                if(manhattanNeighbors.Contains(goal.GetVoxel()))
                {
                    Creature.MoveAction first = new Creature.MoveAction()
                    {
                        Voxel = current,
                        MoveType = Creature.MoveType.Walk
                    };
                    Creature.MoveAction last = new Creature.MoveAction()
                    {
                        Voxel = goal.GetVoxel(),
                        MoveType = Creature.MoveType.Walk
                    };
                    List<Creature.MoveAction> subPath = ReconstructPath(cameFrom, first);
                    subPath.Add(last);
                    toReturn = subPath;
                    return true;
                }

                foreach(Creature.MoveAction n in neighbors)
                {
                    if(closedSet.Contains(n.Voxel))
                    {
                        continue;
                    }

                    float tenativeGScore = gScore[current] + GetDistance(current, n.Voxel, chunks);

                    if(openSet.Contains(n.Voxel) && !(tenativeGScore < gScore[n.Voxel]))
                    {
                        continue;
                    }

                    openSet.Add(n.Voxel);
                    Creature.MoveAction cameAction = n;
                    cameAction.Voxel = current;

                    cameFrom[n.Voxel] = cameAction;
                    gScore[n.Voxel] = tenativeGScore;
                    fScore.Enqueue(n.Voxel, gScore[n.Voxel] + Heuristic(n.Voxel, goal.GetVoxel()) + (n.MoveType == Creature.MoveType.Climb ? 5 : 0));
                }

                if(numExpansions >= maxExpansions)
                {
                    return false;
                }
            }
            toReturn = null;
            return false;
        }



        public static List<Creature.MoveAction> FindPath(Voxel start, GoalRegion goal, ChunkManager chunks, int maxExpansions)
        {
            List<Creature.MoveAction> p = new List<Creature.MoveAction>();
            bool success = Path(start, goal, chunks, maxExpansions, ref p, false);

            if(success)
            {
                return p;
            }
            else
            {
                return null;
            }
        }

        public static float GetDistance(Voxel a, Voxel b, ChunkManager chunks)
        {
            if(!b.IsEmpty)
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