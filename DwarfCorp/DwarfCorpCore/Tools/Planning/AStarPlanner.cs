// AStarPlanner.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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


        private static bool Path(CreatureMovement mover, Voxel start, GoalRegion goal, ChunkManager chunks, int maxExpansions, ref List<Creature.MoveAction> toReturn, bool reverse)
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

            List<Voxel> manhattanNeighbors = new List<Voxel>(6);
            for (int i = 0; i < 6; i++)
            {
                manhattanNeighbors.Add(new Voxel());
            }
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

                neighbors = mover.GetMoveActions(current);
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

                    float tenativeGScore = gScore[current] + GetDistance(current, n.Voxel, n.MoveType, chunks);

                    if(openSet.Contains(n.Voxel) && !(tenativeGScore < gScore[n.Voxel]))
                    {
                        continue;
                    }

                    openSet.Add(n.Voxel);
                    Creature.MoveAction cameAction = n;
                    cameAction.Voxel = current;

                    cameFrom[n.Voxel] = cameAction;
                    gScore[n.Voxel] = tenativeGScore;
                    fScore.Enqueue(n.Voxel, gScore[n.Voxel] + Heuristic(n.Voxel, goal.GetVoxel()));
                }

                if(numExpansions >= maxExpansions)
                {
                    return false;
                }
            }
            toReturn = null;
            return false;
        }



        public static List<Creature.MoveAction> FindPath(CreatureMovement mover, Voxel start, GoalRegion goal, ChunkManager chunks, int maxExpansions)
        {
            List<Creature.MoveAction> p = new List<Creature.MoveAction>();
            bool success = Path(mover, start, goal, chunks, maxExpansions, ref p, false);

            if(success)
            {
                return p;
            }
            else
            {
                return null;
            }
        }

        public static float GetDistance(Voxel a, Voxel b, Creature.MoveType action, ChunkManager chunks)
        {
            if(!b.IsEmpty)
            {
                return 100000;
            }
            else
            {
                float score = (a.Position - b.Position).LengthSquared() * ActionCost(action);

                return score;
            }
        }

        private static float ActionCost(Creature.MoveType action)
        {
            switch (action)
            {
                case Creature.MoveType.Walk:
                    return 1.0f;
                case Creature.MoveType.Jump:
                    return 10.0f;
                case Creature.MoveType.Climb:
                    return 2.0f;
                case Creature.MoveType.Swim:
                    return 20.0f;
                case Creature.MoveType.Fall:
                    return 10.0f;
                default:
                    return 1.0f;
            }
        }

        public static float Heuristic(Voxel a, Voxel b)
        {
            return (a.Position - b.Position).LengthSquared();
        }
    }

}