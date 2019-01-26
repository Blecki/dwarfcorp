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
using Microsoft.Xna.Framework;
using System.IO;

namespace DwarfCorp
{
    /// <summary>
    ///     The classic A star planner, but singled out for use with voxels. It should
    ///     probably be made generic. It is called in a thread so that it can return new paths at its
    ///     own leisurely pace. Plans are then returned via delegate.
    ///     The A* planner uses a graph where nodes are Voxels, and the edges between nodes are Movements.
    ///     A Movement could be something like "walk", "jump" or "fly". The planner finds the shortest list of
    ///     Movements that go from a start DestinationVoxel to a goal DestinationVoxel.
    /// </summary>
    public class AStarPlanner
    {
        /// <summary>
        ///     Gets the DestinationVoxel that has minimum expansion score. Expands this voxel.
        /// </summary>
        /// <param name="fScore">Queue of voxels by their expansion scores.</param>
        /// <returns>The DestinationVoxel with minimu expansion score.</returns>
        public static MoveState GetStateWithMinimumFScore(PriorityQueue<MoveState> fScore)
        {
            return fScore.Count == 0 ? MoveState.InvalidState : fScore.Dequeue();
        }


        /// <summary>
        ///     Creates a path of Movements starting from some end movement to the start.
        /// </summary>
        /// <param name="cameFrom">A dictionary of Voxels to the movement that was taken to get there.</param>
        /// <param name="currentNode">The very last movement in the path.</param>
        /// <param name="start"></param>
        /// <returns>The path of movements. from the start to the current node</returns>
        public static List<MoveAction> ReconstructPath(Dictionary<MoveState, MoveAction> cameFrom,
            MoveAction currentNode, MoveState start)
        {
            var toReturn = new List<MoveAction>() { currentNode };
            while (currentNode.SourceState != start && cameFrom.ContainsKey(currentNode.SourceState))
            {
                currentNode = cameFrom[currentNode.SourceState];
                toReturn.Add(currentNode);
            }
         
            // the path is reversed, and source/destination of edges need to be flipped
            // keeping in mind the "cameFrom" terminology from A*
            toReturn.Reverse();
            /*
            for (int i = 0; i < toReturn.Count; i++)
            {
                var a = toReturn[i];
                var temp = a.SourceVoxel;
                a.SourceVoxel = a.DestinationVoxel;
                a.DestinationVoxel = temp;
                toReturn[i] = a;
            }
            */
            return toReturn;
        }


        public static List<MoveAction> ReconstructInversePath(GoalRegion goal, 
            Dictionary<MoveState, MoveAction> cameFrom, MoveAction currentNode)
        {
            var toReturn = new List<MoveAction>() { currentNode };
            while (true)
            {
                if (!cameFrom.ContainsKey(currentNode.DestinationState))
                    break;
                currentNode = cameFrom[currentNode.DestinationState];
                toReturn.Add(currentNode);

                /*
                for (int frames = 0; frames < 6; frames++)
                {
                    var sourceColor = goal.IsInGoalRegion(currentNode.SourceVoxel) ? Color.Green : Color.Red;
                    Drawer3D.DrawLine(currentNode.SourceVoxel.WorldPosition + Vector3.One * 0.5f,
                           currentNode.DestinationVoxel.WorldPosition + Vector3.One * 0.5f, Color.Red, 0.1f);
                    Drawer3D.DrawBox(currentNode.SourceVoxel.GetBoundingBox(), sourceColor, 0.1f, true);
                    Drawer3D.DrawBox(currentNode.DestinationVoxel.GetBoundingBox(), Color.Yellow, 0.1f, true);
                    foreach (var pair in cameFrom)
                    {
                        var color = Color.White;
                        if (goal.IsInGoalRegion(pair.Value.SourceVoxel))
                            color = Color.Green;
                        Drawer3D.DrawLine(pair.Value.SourceVoxel.WorldPosition + Vector3.One * 0.5f,
                            pair.Value.DestinationVoxel.WorldPosition + Vector3.One * 0.5f, color, 0.05f);

                    }
                    System.Threading.Thread.Sleep(16);
                }
                */
                if (goal.IsInGoalRegion(currentNode.DestinationState.Voxel))
                    break;
            }
            return toReturn;
        }


        public enum PlanResultCode
        {
            Cancelled,
            Invalid,
            NoSolution,
            MaxExpansionsReached,
            Success
        }

        public struct PlanResult
        {
            public PlanResultCode Result;
            public int Expansions;
            public double TimeSeconds;
        }


        /// <summary>
        ///     Creates a path from the start voxel to some goal region, returning a list of movements that can
        ///     take a mover from the start voxel to the goal region.
        /// </summary>
        /// <param name="mover">The agent that we want to find a path for.</param>
        /// <param name="startVoxel"></param>
        /// <param name="goal">Goal conditions that the agent is trying to satisfy.</param>
        /// <param name="chunks">The voxels that the agent is moving through</param>
        /// <param name="maxExpansions">Maximum number of voxels to consider before giving up.</param>
        /// <param name="toReturn">The path of movements that the mover should take to reach the goal.</param>
        /// <param name="weight">
        ///     A heuristic weight to apply to the planner. If 1.0, the path is garunteed to be optimal. Higher values
        ///     usually result in faster plans that are suboptimal.
        /// </param>
        /// <param name="continueFunc"></param>
        /// <returns>True if a path could be found, or false otherwise.</returns>
        private static PlanResult Path(
            CreatureMovement mover, 
            VoxelHandle startVoxel, 
            GoalRegion goal, 
            ChunkManager chunks,
            int maxExpansions, 
            ref List<MoveAction> toReturn, 
            float weight, 
            Func<bool> continueFunc)
        {
            // Create a local clone of the octree, using only the objects belonging to the player.
            var octree = new OctTreeNode<Body>(mover.Creature.World.ChunkManager.Bounds.Min, mover.Creature.World.ChunkManager.Bounds.Max);
           
            List<Body> playerObjects = new List<Body>(mover.Creature.World.PlayerFaction.OwnedObjects);
            List<Body> teleportObjects = playerObjects.Where(o => o.Tags.Contains("Teleporter")).ToList();
            foreach (var obj in playerObjects)
                octree.Add(obj, obj.GetBoundingBox());

            var storage = new MoveActionTempStorage();

            var start = new MoveState()
            {
                Voxel = startVoxel
            };

            var startTime = DwarfTime.Tick();
            if (mover.IsSessile)
            {
                return new PlanResult()
                {
                    Expansions = 0,
                    Result = PlanResultCode.Invalid,
                    TimeSeconds = DwarfTime.Tock(startTime)
                };

            }

            // Sometimes a goal may not even be achievable a.priori. If this is true, we know there can't be a path 
            // which satisifies that goal.
            if (!goal.IsPossible())
            {
                toReturn = null;
                return new PlanResult()
                {
                    Expansions = 0,
                    Result = PlanResultCode.Invalid,
                    TimeSeconds = DwarfTime.Tock(startTime)
                };
            }

            // Voxels that have already been explored.
            var closedSet = new HashSet<MoveState>();

            // Voxels which should be explored.
            var openSet = new HashSet<MoveState>
            {
                start
            };

            // Dictionary of voxels to the optimal action that got the mover to that voxel.
            var cameFrom = new Dictionary<MoveState, MoveAction>();

            // Optimal score of a voxel based on the path it took to get there.
            var gScore = new Dictionary<MoveState, float>();

            // Expansion priority of voxels.
            var fScore = new PriorityQueue<MoveState>();

            // Starting conditions of the search.
            gScore[start] = 0.0f;
            fScore.Enqueue(start, gScore[start] + weight * goal.Heuristic(start.Voxel));

            // Keep count of the number of expansions we've taken to get to the goal.
            int numExpansions = 0;

            // Check the voxels adjacent to the current voxel as a quick test of adjacency to the goal.
            //var manhattanNeighbors = new List<VoxelHandle>(6);
            //for (int i = 0; i < 6; i++)
            //{
            //    manhattanNeighbors.Add(new VoxelHandle());
            //}

            // Loop until we've either checked every possible voxel, or we've exceeded the maximum number of
            // expansions.
            while (openSet.Count > 0 && numExpansions < maxExpansions)
            {

                if (numExpansions % 10 == 0 && !continueFunc())
                    return new PlanResult
                    {
                        Result = PlanResultCode.Cancelled,
                        Expansions = numExpansions,
                        TimeSeconds = DwarfTime.Tock(startTime)
                    };


                // Check the next voxel to explore.
                var current = GetStateWithMinimumFScore(fScore);
                if (!current.IsValid)
                {
                    // If there wasn't a voxel to explore, just try to expand from
                    // the start again.
                    numExpansions++;
                    continue;
                }

                numExpansions++;

                // If we've reached the goal already, reconstruct the path from the start to the 
                // goal.
       
                
                if (goal.IsInGoalRegion(current.Voxel) && cameFrom.ContainsKey(current))
                {
                    toReturn = ReconstructPath(cameFrom, cameFrom[current], start);
                    return new PlanResult()
                    {
                        Result = PlanResultCode.Success,
                        Expansions = numExpansions,
                        TimeSeconds = DwarfTime.Tock(startTime)
                    };
                }

                // We've already considered the voxel, so add it to the closed set.
                openSet.Remove(current);
                closedSet.Add(current);

                // Get the voxels that can be moved to from the current voxel.
                var neighbors = mover.GetMoveActions(current, octree, teleportObjects, storage).ToList();
                

                var foundGoalAdjacent = neighbors.FirstOrDefault(n => !n.DestinationState.VehicleState.IsRidingVehicle && goal.IsInGoalRegion(n.DestinationState.Voxel));

                // A quick test to see if we're already adjacent to the goal. If we are, assume
                // that we can just walk to it.
                if (foundGoalAdjacent.DestinationState.IsValid && foundGoalAdjacent.SourceState.IsValid)
                {
                    if (cameFrom.ContainsKey(current))
                    {
                        toReturn = ReconstructPath(cameFrom, foundGoalAdjacent, start);
                        return new PlanResult()
                        {
                            Result = PlanResultCode.Success,
                            Expansions = numExpansions,
                            TimeSeconds = DwarfTime.Tock(startTime)
                        };
                    }
                }

                // Otherwise, consider all of the neighbors of the current voxel that can be moved to,
                // and determine how to add them to the list of expansions.
                foreach (MoveAction n in neighbors)
                {
                    // If we've already explored that voxel, don't explore it again.
                    if (closedSet.Contains(n.DestinationState))
                    {
                        continue;
                    }

                    // Otherwise, consider the case of moving to that neighbor.
                    float tenativeGScore = gScore[current] + GetDistance(current.Voxel, n.DestinationState.Voxel, n.MoveType, mover);

                    // IF the neighbor can already be reached more efficiently, ignore it.
                    if (openSet.Contains(n.DestinationState) && !(tenativeGScore < gScore[n.DestinationState]))
                    {
                        continue;
                    }

                    // Otherwise, add it to the list of voxels for consideration.
                    openSet.Add(n.DestinationState);

                    // Add an edge to the voxel from the current voxel.
                    var cameAction = n;
                    cameFrom[n.DestinationState] = cameAction;

                    // Update the expansion scores for the next voxel.
                    gScore[n.DestinationState] = tenativeGScore;
                    fScore.Enqueue(n.DestinationState, gScore[n.DestinationState] + weight * (goal.Heuristic(n.DestinationState.Voxel) + (!n.DestinationState.VehicleState.IsRidingVehicle ? 100.0f : 0.0f)));
                }

                // If we've expanded too many voxels, just give up.
                if (numExpansions >= maxExpansions)
                {
                    return new PlanResult()
                    {
                        Expansions = numExpansions,
                        Result = PlanResultCode.MaxExpansionsReached,
                        TimeSeconds = DwarfTime.Tock(startTime)
                    };
                }
            }

            // Somehow we've reached this code without having found a path. Return false.
            toReturn = null;
            return new PlanResult()
            {
                Expansions = numExpansions,
                Result = PlanResultCode.NoSolution,
                TimeSeconds = DwarfTime.Tock(startTime)
            };

        }

        // Find a path from the start to the goal by computing an inverse path from goal to the start. Should only be used
        // if the forward path fails.
        private static PlanResult InversePath(CreatureMovement mover, VoxelHandle startVoxel, GoalRegion goal, ChunkManager chunks,
                int maxExpansions, ref List<MoveAction> toReturn, float weight, Func<bool> continueFunc)
        {
            // Create a local clone of the octree, using only the objects belonging to the player.
            var octree = new OctTreeNode<Body>(mover.Creature.World.ChunkManager.Bounds.Min, mover.Creature.World.ChunkManager.Bounds.Max);
            List<Body> playerObjects = new List<Body>(mover.Creature.World.PlayerFaction.OwnedObjects);
            List<Body> teleportObjects = mover.Creature.World.PlayerFaction.OwnedObjects.Where(o => o.Tags.Contains("Teleporter")).ToList();
            foreach (var obj in playerObjects)
                octree.Add(obj, obj.GetBoundingBox());

            MoveState start = new MoveState()
            {
                Voxel = startVoxel
            };

            var startTime = DwarfTime.Tick();
            if (mover.IsSessile)
            {
                return new PlanResult()
                {
                    Result = PlanResultCode.Invalid,
                    Expansions = 0,
                    TimeSeconds = 0
                };
            }

            if (!start.IsValid)
            {
                return new PlanResult()
                {
                    Result = PlanResultCode.Invalid,
                    Expansions = 0,
                    TimeSeconds = 0
                };
            }


            // Sometimes a goal may not even be achievable a.priori. If this is true, we know there can't be a path 
            // which satisifies that goal.
            // It only makes sense to do inverse plans for goals that have an associated voxel.
            if (!goal.IsPossible() || !goal.GetVoxel().IsValid)
            {
                toReturn = null;
                return new PlanResult()
                {
                    Result = PlanResultCode.Invalid,
                    Expansions = 0,
                    TimeSeconds = 0
                };
            }


            if (goal.IsInGoalRegion(start.Voxel))
            {
                toReturn = new List<MoveAction>();
                return new PlanResult()
                {
                    Result = PlanResultCode.Success,
                    Expansions = 0,
                    TimeSeconds = 0
                };
            }

            // Voxels that have already been explored.
            var closedSet = new HashSet<MoveState>();

            // Voxels which should be explored.
            var openSet = new HashSet<MoveState>();

            // Dictionary of voxels to the optimal action that got the mover to that voxel.
            var cameFrom = new Dictionary<MoveState, MoveAction>();

            // Optimal score of a voxel based on the path it took to get there.
            var gScore = new Dictionary<MoveState, float>();

            // Expansion priority of voxels.
            var fScore = new PriorityQueue<MoveState>();

            var goalVoxels = new List<VoxelHandle>();
            goalVoxels.Add(goal.GetVoxel());
            // Starting conditions of the search.

            foreach (var goalVoxel in VoxelHelpers.EnumerateCube(goal.GetVoxel().Coordinate)
                .Select(c => new VoxelHandle(start.Voxel.Chunk.Manager.ChunkData, c))) 
            {
                if (!goalVoxel.IsValid) continue;
                var goalState = new MoveState() { Voxel = goalVoxel };
                if (goal.IsInGoalRegion(goalVoxel))
                {
                    goalVoxels.Add(goalVoxel);
                    openSet.Add(goalState);
                    gScore[goalState] = 0.0f;
                    fScore.Enqueue(goalState,
                        gScore[goalState] + weight*(goalVoxel.WorldPosition - start.Voxel.WorldPosition).LengthSquared());
                }
            }

            // Keep count of the number of expansions we've taken to get to the goal.
            int numExpansions = 0;
    
            // Loop until we've either checked every possible voxel, or we've exceeded the maximum number of
            // expansions.
            while (openSet.Count > 0 && numExpansions < maxExpansions)
            {
                if (numExpansions % 10 == 0 && !continueFunc())
                    return new PlanResult
                    {
                        Result = PlanResultCode.Cancelled,
                        Expansions = numExpansions,
                        TimeSeconds = DwarfTime.Tock(startTime)
                    };

                // Check the next voxel to explore.
                var current = GetStateWithMinimumFScore(fScore);
                if (!current.IsValid)
                {
                    continue;
                }
                //Drawer3D.DrawBox(current.GetBoundingBox(), Color.Blue, 0.1f);
                numExpansions++;

                // If we've reached the goal already, reconstruct the path from the start to the 
                // goal.
                if (current.Equals(start))
                {
                    toReturn = ReconstructInversePath(goal, cameFrom, cameFrom[start]);
                    return new PlanResult()
                    {
                        Result = PlanResultCode.Success,
                        Expansions = numExpansions,
                        TimeSeconds = DwarfTime.Tock(startTime)
                    };
                }

                // We've already considered the voxel, so add it to the closed set.
                openSet.Remove(current);
                closedSet.Add(current);

                IEnumerable<MoveAction> neighbors = null;

                // Get the voxels that can be moved to from the current voxel.
                neighbors = mover.GetInverseMoveActions(current, octree, teleportObjects);

                // Otherwise, consider all of the neighbors of the current voxel that can be moved to,
                // and determine how to add them to the list of expansions.
                foreach (MoveAction n in neighbors)
                {
                    //Drawer3D.DrawBox(n.SourceVoxel.GetBoundingBox(), Color.Red, 0.1f);
                    // If we've already explored that voxel, don't explore it again.
                    if (closedSet.Contains(n.SourceState))
                    {
                        continue;
                    }

                    // Otherwise, consider the case of moving to that neighbor.
                    float tenativeGScore = gScore[current] + GetDistance(current.Voxel, n.SourceState.Voxel, n.MoveType, mover);

                    // IF the neighbor can already be reached more efficiently, ignore it.
                    if (openSet.Contains(n.SourceState) && gScore.ContainsKey(n.SourceState) && !(tenativeGScore < gScore[n.SourceState]))
                    {
                        continue;
                    }

                    // Otherwise, add it to the list of voxels for consideration.
                    openSet.Add(n.SourceState);

                    // Add an edge to the voxel from the current voxel.
                    var cameAction = n;
                    cameFrom[n.SourceState] = cameAction;

                    // Update the expansion scores for the next voxel.
                    gScore[n.SourceState] = tenativeGScore;
                    fScore.Enqueue(n.SourceState, gScore[n.SourceState] + weight * ((n.SourceState.Voxel.WorldPosition - start.Voxel.WorldPosition).LengthSquared() + (!n.SourceState.VehicleState.IsRidingVehicle ? 100.0f : 0.0f)));
                }

                // If we've expanded too many voxels, just give up.
                if (numExpansions >= maxExpansions)
                {
                    return new PlanResult()
                    {
                        Result = PlanResultCode.MaxExpansionsReached,
                        Expansions = numExpansions,
                        TimeSeconds = DwarfTime.Tock(startTime)
                    };
                }
            }

            // Somehow we've reached this code without having found a path. Return false.
            toReturn = null;
            return new PlanResult()
            {
                Result = PlanResultCode.NoSolution,
                Expansions = numExpansions,
                TimeSeconds = DwarfTime.Tock(startTime)
            };
        }

        public static float OpennessHeuristic(VoxelHandle vox)
        {
            if (!vox.IsValid)
            {
                return 0.0f;
            }

            var p = vox.Coordinate;
            var data = vox.Chunk.Manager.ChunkData;

            float sumLength = 0;
            for (int x = -10; x < 1; x++)
            {
                var n = p + new GlobalVoxelOffset(x, 0, 0);
                var voxAt = new VoxelHandle(data, n);
                if (!voxAt.IsValid)
                    continue;
                if (voxAt.IsEmpty)
                    sumLength++;
                else
                    break;
            }

            for (int x = 0; x < 11; x++)
            {
                var n = p + new GlobalVoxelOffset(x, 0, 0);
                var voxAt = new VoxelHandle(data, n);
                if (!voxAt.IsValid)
                    continue;
                if (voxAt.IsEmpty)
                    sumLength++;
                else
                    break;
            }

            for (int x = -10; x < 1; x++)
            {
                var n = p + new GlobalVoxelOffset(0, x, 0);
                var voxAt = new VoxelHandle(data, n);
                if (!voxAt.IsValid)
                    continue;
                if (voxAt.IsEmpty)
                    sumLength++;
                else
                    break;
            }

            for (int x = 0; x < 11; x++)
            {
                var n = p + new GlobalVoxelOffset(0, x, 0);
                var voxAt = new VoxelHandle(data, n);
                if (!voxAt.IsValid)
                    continue;
                if (voxAt.IsEmpty)
                    sumLength++;
                else
                    break;
            }

            for (int x = -10; x < 1; x++)
            {
                var n = p + new GlobalVoxelOffset(0, 0, x);
                var voxAt = new VoxelHandle(data, n);
                if (!voxAt.IsValid)
                    continue;
                if (voxAt.IsEmpty)
                    sumLength++;
                else
                    break;
            }

            for (int x = 0; x < 11; x++)
            {
                var n = p + new GlobalVoxelOffset(0, 0, x);
                var voxAt = new VoxelHandle(data, n);
                if (!voxAt.IsValid)
                    continue;
                if (voxAt.IsEmpty)
                    sumLength++;
                else
                    break;
            }

            return sumLength;
        }


        /// <summary>
        ///     Finds the path from the start to the goal region of move actions that can be performed by the creature.
        /// </summary>
        /// <param name="mover">The creature following the path.</param>
        /// <param name="start">The voxel the path starts with.</param>
        /// <param name="goal">Goal conditions that must be satisfied by the path.</param>
        /// <param name="chunks">The chunks the creature is moving through.</param>
        /// <param name="maxExpansions">Maximum number of voxels to explore before giving up.</param>
        /// <param name="weight">
        ///     The heuristic weight of the planner. If 1.0, the path returned is optimal.
        ///     Higher values result in suboptimal paths, but the search may be faster.
        /// </param>
        /// <param name="numPlans"></param>
        /// <param name="continueFunc"></param>
        /// <returns>The path of movements the creature must take to reach the goal. Returns null if no such path exists.</returns>
        public static List<MoveAction> FindPath(CreatureMovement mover, VoxelHandle start, GoalRegion goal,
            ChunkManager chunks, int maxExpansions, float weight, int numPlans, Func<bool> continueFunc, out PlanResultCode resultCode)
        {
            var p = new List<MoveAction>();
            //bool use_inverse = goal.IsReversible() && OpennessHeuristic(goal.GetVoxel()) < OpennessHeuristic(start);
            bool use_inverse = false;
            var result = use_inverse ? InversePath(mover, start, goal, chunks, maxExpansions, ref p, weight, continueFunc)
                : Path(mover, start, goal, chunks, maxExpansions, ref p, weight, continueFunc);

            resultCode = result.Result;
            var length = (start.WorldPosition - goal.GetVoxel().WorldPosition).Length();
            if (result.Result == PlanResultCode.Success)
            {
                return p;
            }

            if (result.Result == PlanResultCode.Invalid || 
                result.Result == PlanResultCode.NoSolution || 
                result.Result == PlanResultCode.Cancelled)
            {
                return null;
            }

            if (!goal.IsReversible())
            {
                return null;
            }

            result = use_inverse ? Path(mover, start, goal, chunks, maxExpansions, ref p, weight, continueFunc)
                : InversePath(mover, start, goal, chunks, maxExpansions, ref p, weight, continueFunc);
            resultCode = result.Result;
            return result.Result == PlanResultCode.Success ? p : null;
        }

        /// <summary>
        ///     Given two voxels, and an action taken between the voxels, returns the cost of moving
        ///     between the two voxels given that action.
        /// </summary>
        /// <param name="a">The source voxel of the action.</param>
        /// <param name="b">The destination voxel of the action.</param>
        /// <param name="action">The action taken to get between voxels.</param>
        /// <param name="movement">The creature making the movement.</param>
        /// <returns>The cost of going from a to b using the given action.</returns>
        public static float GetDistance(VoxelHandle a, VoxelHandle b, MoveType action, CreatureMovement movement)
        {
            // Otherwise, the cost is the distance between the voxels multiplied by the intrinsic cost
            // of an action.
            var actionCost = ActionCost(movement, action);
            float score = (a.WorldPosition - b.WorldPosition).LengthSquared() * actionCost + actionCost;
            return score;
        }

        /// <summary>
        ///     Returns the intrinsic cost of an action.
        /// </summary>
        private static float ActionCost(CreatureMovement movement, MoveType action)
        {
            return movement.Cost(action);
        }
    }
}