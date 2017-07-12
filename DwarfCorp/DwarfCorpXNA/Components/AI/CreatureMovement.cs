// CreatureAI.cs
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
using System.Runtime.Serialization;
//using System.Windows.Forms;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class CreatureMovement
    {
        public CreatureMovement(CreatureAI Parent)
        {
            this.Parent = Parent;
            Actions = new Dictionary<MoveType, ActionStats>
            {
                {
                    MoveType.Climb,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 2.0f,
                        Speed = 0.5f
                    }
                },
                {
                    MoveType.Walk,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 1.0f,
                        Speed = 1.0f
                    }
                },
                {
                    MoveType.Swim,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 2.0f,
                        Speed = 0.5f
                    }
                },
                {
                    MoveType.Jump,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 1.0f,
                        Speed = 1.0f
                    }
                },
                {
                    MoveType.Fly,
                    new ActionStats
                    {
                        CanMove = false,
                        Cost = 1.0f,
                        Speed = 1.0f
                    }
                },
                {
                    MoveType.Fall,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 30.0f,
                        Speed = 1.0f
                    }
                },
                {
                    MoveType.DestroyObject,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 30.0f,
                        Speed = 1.0f
                    }
                },
                {
                    MoveType.ClimbWalls,
                    new ActionStats
                    {
                        CanMove = false,
                        Cost = 30.0f,
                        Speed = 0.5f
                    }
                },
            };
        }

        public CreatureAI Parent;
        /// <summary> The creature associated with this AI </summary>
        public Creature Creature { get { return Parent.Creature; } }

        /// <summary> Wrapper around the creature's fly movement </summary>
        [JsonIgnore]
        public bool CanFly
        {
            get { return Can(MoveType.Fly); }
            set { SetCan(MoveType.Fly, value); }
        }

        /// <summary> Wrapper aroound the creature's swim movement </summary>
        [JsonIgnore]
        public bool CanSwim
        {
            get { return Can(MoveType.Swim); }
            set { SetCan(MoveType.Swim, value); }
        }

        /// <summary> wrapper around creature's climb movement </summary>
        [JsonIgnore]
        public bool CanClimb
        {
            get { return Can(MoveType.Climb); }
            set { SetCan(MoveType.Climb, value); }
        }

        /// <summary> wrapper around creature's climb walls movement </summary>
        [JsonIgnore]
        public bool CanClimbWalls
        {
            get { return Can(MoveType.ClimbWalls); }
            set { SetCan(MoveType.ClimbWalls, value); }
        }

        /// <summary> wrapper around creature's walk movement </summary>
        [JsonIgnore]
        public bool CanWalk
        {
            get { return Can(MoveType.Walk); }
            set { SetCan(MoveType.Walk, value); }
        }

        /// <summary> List of move actions that the creature can take </summary>
        public Dictionary<MoveType, ActionStats> Actions { get; set; }

        /// <summary> determines whether the creature can move using the given move type. </summary>
        public bool Can(MoveType type)
        {
            return Actions[type].CanMove;
        }

        /// <summary> gets the cost of a creature's movement for a particular type </summary>
        public float Cost(MoveType type)
        {
            return Actions[type].Cost;
        }

        /// <summary> gets the speed multiplier of a creature's movement for a particular type </summary>
        public float Speed(MoveType type)
        {
            return Actions[type].Speed;
        }

        /// <summary> Sets whether the creature can move using the given type </summary>
        public void SetCan(MoveType type, bool value)
        {
            Actions[type].CanMove = value;
        }

        /// <summary> sets the cost of moving using a given movement type </summary>
        public void SetCost(MoveType type, float value)
        {
            Actions[type].Cost = value;
        }

        /// <summary> Sets the movement speed of a particular move type </summary>
        public void SetSpeed(MoveType type, float value)
        {
            Actions[type].Speed = value;
        }

        public bool IsSessile = false;

        /// <summary> 
        /// Returns a 3 x 3 x 3 voxel grid corresponding to the immediate neighborhood
        /// around the given voxel..
        /// </summary>
        private VoxelHandle[, ,] GetNeighborhood(VoxelHandle voxel)
        {
            var neighborHood = new VoxelHandle[3, 3, 3];
            CollisionManager objectHash = Creature.Manager.World.CollisionManager;

            VoxelChunk startChunk = voxel.Chunk;
            var x = (int)voxel.GridPosition.X;
            var y = (int)voxel.GridPosition.Y;
            var z = (int)voxel.GridPosition.Z;
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    for (int dz = -1; dz < 2; dz++)
                    {
                        neighborHood[dx + 1, dy + 1, dz + 1] = new VoxelHandle();
                        int nx = dx + x;
                        int ny = dy + y;
                        int nz = dz + z;
                        if (
                            !Creature.Manager.World.ChunkManager.ChunkData.GetVoxel(startChunk,
                                startChunk.ID + new LocalVoxelCoordinate(nx, ny, nz),
                                ref neighborHood[dx + 1, dy + 1, dz + 1]))
                        {
                            neighborHood[dx + 1, dy + 1, dz + 1] = null;
                        }
                    }
                }
            }
            return neighborHood;
        }

        /// <summary> Determines whether the voxel has any neighbors in X or Z directions </summary>
        private bool HasNeighbors(VoxelHandle[, ,] neighborHood)
        {
            bool hasNeighbors = false;
            for (int dx = 0; dx < 3; dx++)
            {
                for (int dz = 0; dz < 3; dz++)
                {
                    if (dx == 1 && dz == 1)
                    {
                        continue;
                    }

                    hasNeighbors = hasNeighbors ||
                                   (neighborHood[dx, 1, dz] != null && (!neighborHood[dx, 1, dz].IsEmpty));
                }
            }


            return hasNeighbors;
        }

        /// <summary> Determines whether the given voxel is null or empty </summary>
        private bool IsEmpty(VoxelHandle v)
        {
            return v == null || v.IsEmpty;
        }

        /// <summary> gets a list of actions that the creature can take from the given position </summary>
        public IEnumerable<MoveAction> GetMoveActions(Vector3 pos)
        {
            var vox = new VoxelHandle();
            Creature.Manager.World.ChunkManager.ChunkData.GetVoxel(pos, ref vox);
            return GetMoveActions(vox);
        }

        // Todo: Convert to temporary voxel handles?
        /// <summary> gets the list of actions that the creature can take from a given voxel. </summary>
        public IEnumerable<MoveAction> GetMoveActions(VoxelHandle voxel)
        {
            if (!voxel.IsEmpty)
            {
                yield break;
            }

            CollisionManager objectHash = Creature.Manager.World.CollisionManager;

            VoxelHandle[, ,] neighborHood = GetNeighborhood(voxel);
            var x = (int)voxel.GridPosition.X;
            var y = (int)voxel.GridPosition.Y;
            var z = (int)voxel.GridPosition.Z;
            bool inWater = (neighborHood[1, 1, 1] != null && neighborHood[1, 1, 1].WaterLevel > WaterManager.inWaterThreshold);
            bool standingOnGround = (neighborHood[1, 0, 1] != null && !neighborHood[1, 0, 1].IsEmpty);
            bool topCovered = (neighborHood[1, 2, 1] != null && !neighborHood[1, 2, 1].IsEmpty);
            bool hasNeighbors = HasNeighbors(neighborHood);
            bool isClimbing = false;

            var successors = new List<MoveAction>();

            //Climbing ladders.
            IEnumerable<IBoundedObject> objectsInside = objectHash.GetObjectsAt(voxel,
                CollisionManager.CollisionType.Static);
            if (objectsInside != null)
            {
                IEnumerable<GameComponent> bodies = objectsInside.OfType<GameComponent>();
                IList<GameComponent> enumerable = bodies as IList<GameComponent> ?? bodies.ToList();
                if (CanClimb)
                {
                    bool hasLadder = enumerable.Any(component => component.Tags.Contains("Climbable"));
                    // if the creature can climb objects and a ladder is in this voxel,
                    // then add a climb action.
                    if (hasLadder)
                    {
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(1, 2, 1),
                            MoveType = MoveType.Climb,
                            InteractObject = enumerable.FirstOrDefault(component => component.Tags.Contains("Climbable"))
                        });

                        isClimbing = true;

                        if (!standingOnGround)
                        {
                            successors.Add(new MoveAction
                            {
                                Diff = new Vector3(1, 0, 1),
                                MoveType = MoveType.Climb,
                                InteractObject = enumerable.FirstOrDefault(component => component.Tags.Contains("Climbable"))
                            });
                        }

                        standingOnGround = true;
                    }
                }
            }

            // If the creature can climb walls and is not blocked by a voxl above.
            if (CanClimbWalls && !topCovered)
            {
                VoxelHandle[] walls =
                {
                    neighborHood[2, 1, 1], neighborHood[0, 1, 1], neighborHood[1, 1, 2],
                    neighborHood[1, 1, 0]
                };
                // Determine if the creature is adjacent to a wall.
                bool nearWall = (neighborHood[2, 1, 1] != null && !neighborHood[2, 1, 1].IsEmpty) ||
                                (neighborHood[0, 1, 1] != null && !neighborHood[0, 1, 1].IsEmpty) ||
                                (neighborHood[1, 1, 2] != null && !neighborHood[1, 1, 2].IsEmpty) ||
                                (neighborHood[1, 1, 0] != null && !neighborHood[1, 1, 0].IsEmpty);

                // If we're near a wall, we can climb upwards.
                if (nearWall)
                {
                    isClimbing = true;
                    successors.Add(new MoveAction
                    {
                        Diff = new Vector3(1, 2, 1),
                        MoveType = MoveType.ClimbWalls,
                        ActionVoxel = walls.FirstOrDefault(v => v != null && !v.IsEmpty)
                    });
                }
                // If we're near a wall and not blocked from below, we can climb downward.
                if (nearWall && !standingOnGround)
                {
                    successors.Add(new MoveAction
                    {
                        Diff = new Vector3(1, 0, 1),
                        MoveType = MoveType.ClimbWalls,
                        ActionVoxel = walls.FirstOrDefault(v => v != null && !v.IsEmpty)
                    });
                }
            }

            // If the creature either can walk or is in water, add the 
            // eight-connected free neighbors around the voxel.
            if ((CanWalk && standingOnGround) || (CanSwim && inWater))
            {
                // If the creature is in water, it can swim. Otherwise, it will walk.
                var moveType = inWater ? MoveType.Swim : MoveType.Walk;
                if (IsEmpty(neighborHood[0, 1, 1]))
                    // +- x
                    successors.Add(new MoveAction
                    {
                        Diff = new Vector3(0, 1, 1),
                        MoveType = moveType
                    });

                if (IsEmpty(neighborHood[2, 1, 1]))
                    successors.Add(new MoveAction
                    {
                        Diff = new Vector3(2, 1, 1),
                        MoveType = moveType
                    });

                if (IsEmpty(neighborHood[1, 1, 0]))
                    // +- z
                    successors.Add(new MoveAction
                    {
                        Diff = new Vector3(1, 1, 0),
                        MoveType = moveType
                    });

                if (IsEmpty(neighborHood[1, 1, 2]))
                    successors.Add(new MoveAction
                    {
                        Diff = new Vector3(1, 1, 2),
                        MoveType = moveType
                    });

                // Only bother worrying about 8-connected movement if there are
                // no full neighbors around the voxel.
                if (!hasNeighbors)
                {
                    if (IsEmpty(neighborHood[2, 1, 2]))
                        // +x + z
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(2, 1, 2),
                            MoveType = moveType
                        });

                    if (IsEmpty(neighborHood[2, 1, 0]))
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(2, 1, 0),
                            MoveType = moveType
                        });

                    if (IsEmpty(neighborHood[0, 1, 2]))
                        // -x -z
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(0, 1, 2),
                            MoveType = moveType
                        });

                    if (IsEmpty(neighborHood[0, 1, 0]))
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(0, 1, 0),
                            MoveType = moveType
                        });
                }
            }

            // If the creature's head is free, and it is standing on ground,
            // or if it is in water, or if it is climbing, it can also jump
            // to voxels that are 1 cell away and 1 cell up.
            if (!topCovered && (standingOnGround || (CanSwim && inWater) || isClimbing))
            {
                for (int dx = 0; dx <= 2; dx++)
                {
                    for (int dz = 0; dz <= 2; dz++)
                    {
                        if (dx == 1 && dz == 1) continue;

                        if (!IsEmpty(neighborHood[dx, 1, dz]))
                        {
                            successors.Add(new MoveAction
                            {
                                Diff = new Vector3(dx, 2, dz),
                                MoveType = MoveType.Jump
                            });
                        }
                    }
                }
            }


            // If the creature is not in water and is not standing on ground,
            // it can fall one voxel downward in free space.
            if (!inWater && !standingOnGround)
            {
                successors.Add(new MoveAction
                {
                    Diff = new Vector3(1, 0, 1),
                    MoveType = MoveType.Fall
                });
            }

            // If the creature can fly and is not underwater, it can fly
            // to any adjacent empty cell.
            if (CanFly && !inWater)
            {
                for (int dx = 0; dx <= 2; dx++)
                {
                    for (int dz = 0; dz <= 2; dz++)
                    {
                        for (int dy = 0; dy <= 2; dy++)
                        {
                            if (dx == 1 && dz == 1 && dy == 1) continue;

                            if (IsEmpty(neighborHood[dx, 1, dz]))
                            {
                                successors.Add(new MoveAction
                                {
                                    Diff = new Vector3(dx, dy, dz),
                                    MoveType = MoveType.Fly
                                });
                            }
                        }
                    }
                }
            }

            // Now, validate each move action that the creature might take.
            foreach (MoveAction v in successors)
            {
                VoxelHandle n = neighborHood[(int)v.Diff.X, (int)v.Diff.Y, (int)v.Diff.Z];
                if (n != null && (n.IsEmpty || n.WaterLevel > 0))
                {
                    // Do one final check to see if there is an object blocking the motion.
                    bool blockedByObject = false;
                    List<IBoundedObject> objectsAtNeighbor = Creature.Manager.World.CollisionManager.GetObjectsAt(
                        n, CollisionManager.CollisionType.Static);

                    // If there is an object blocking the motion, determine if it can be passed through.
                    if (objectsAtNeighbor != null)
                    {
                        IEnumerable<GameComponent> bodies = objectsAtNeighbor.OfType<GameComponent>();
                        IList<GameComponent> enumerable = bodies as IList<GameComponent> ?? bodies.ToList();

                        foreach (GameComponent body in enumerable)
                        {
                            var door = body.GetRoot().EnumerateAll().OfType<Door>().FirstOrDefault();
                            // If there is an enemy door blocking movement, we can destroy it to get through.
                            if (door != null)
                            {
                                if (
                                    Creature.World.Diplomacy.GetPolitics(door.TeamFaction, Creature.Faction)
                                        .GetCurrentRelationship() !=
                                    Relationship.Loving)
                                {
                                    if (Can(MoveType.DestroyObject))
                                        yield return (new MoveAction
                                        {
                                            Diff = v.Diff,
                                            MoveType = MoveType.DestroyObject,
                                            InteractObject = door,
                                            DestinationVoxel = new VoxelHandle(n),
                                            SourceVoxel = new VoxelHandle(voxel)
                                        });
                                    blockedByObject = true;
                                }
                            }
                        }
                    }
                    // If no object blocked us, we can move freely as normal.
                    if (!blockedByObject)
                    {
                        MoveAction newAction = v;
                        newAction.SourceVoxel = new VoxelHandle(voxel);
                        newAction.DestinationVoxel = new VoxelHandle(n);
                       yield return newAction;
                    }
                }
            }
        }
        /// <summary> Each action has a cost, a speed, and a validity check </summary>
        public class ActionStats
        {
            public bool CanMove = false;
            public float Cost = 1.0f;
            public float Speed = 1.0f;
        }

        // Inverts GetMoveActions. So, returns the list of move actions whose target is the current voxel.
        // Very, very slow.
        public IEnumerable<MoveAction> GetInverseMoveActions(VoxelHandle current)
        {
            foreach (var v in Neighbors.EnumerateAllNeighbors(current.Coordinate)
                .Select(n => new VoxelHandle(current.Chunk.Manager.ChunkData, n))
                .Where(h => h.Chunk != null))
            {
                foreach (var a in GetMoveActions(v).Where(a => a.DestinationVoxel.Equals(current)))
                    yield return a;
            }
        }
    }
}
