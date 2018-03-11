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
    [JsonObject(IsReference = true)]
    public class CreatureMovement
    {

        public CreatureMovement()
        {

        }

        public CreatureMovement(CreatureAI Parent)
        {
            this.Parent = Parent;
            Actions = new Dictionary<MoveType, ActionStats>
            {
                {
                    MoveType.EnterVehicle,
                    new ActionStats
                    {
                        CanMove = false,
                        Cost = 1.0f,
                        Speed = 1.0f
                    }
                },
                {
                    MoveType.ExitVehicle,
                    new ActionStats
                    {
                        CanMove = false,
                        Cost = 1.0f,
                        Speed = 1.0f
                    }
                },
                {
                    MoveType.RideVehicle,
                    new ActionStats
                    {
                        CanMove = false,
                        Cost = 0.1f,
                        Speed = 10.0f
                    }
                },
                {
                    MoveType.Climb,
                    new ActionStats
                    {
                        CanMove = true,
                        Cost = 1.0f,
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
        [JsonIgnore]
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
        private VoxelHandle[, ,] GetNeighborhood(VoxelHandle Voxel)
        {
            var neighborhood = new VoxelHandle[3, 3, 3];

            for (var dx = -1; dx <= 1; ++dx)
                for (var dy = -1; dy <= 1; ++dy)
                    for (var dz = -1; dz <= 1; ++dz)
                    {
                        var v = new VoxelHandle(Creature.World.ChunkManager.ChunkData,
                            Voxel.Coordinate + new GlobalVoxelOffset(dx, dy, dz));
                        neighborhood[dx + 1, dy + 1, dz + 1] = v;
                    }

            return neighborhood;
        }

        /// <summary> Determines whether the voxel has any neighbors in X or Z directions </summary>
        private bool HasNeighbors(VoxelHandle[,,] Neighborhood)
        {
            for (var x = 0; x < 3; ++x)
                for (var z = 0; z < 3; ++z)
                {
                    if (x == 1 && z == 1)
                        continue;

                    if (Neighborhood[x, 1, z].IsValid && (!Neighborhood[x, 1, z].IsEmpty))
                        return true;
                }

            return false;
        }

        /// <summary> gets a list of actions that the creature can take from the given position </summary>
        public IEnumerable<MoveAction> GetMoveActions(Vector3 Pos)
        {
            var vox = new VoxelHandle(Creature.World.ChunkManager.ChunkData,
                GlobalVoxelCoordinate.FromVector3(Pos));
            return GetMoveActions(new MoveState() { Voxel = vox });
        }



        /// <summary> gets the list of actions that the creature can take from a given voxel. </summary>
        public IEnumerable<MoveAction> GetMoveActions(MoveState state)
        {
            var voxel = state.Voxel;
            if (!voxel.IsValid || !voxel.IsEmpty)
                yield break;

            CollisionManager objectHash = Creature.Manager.World.CollisionManager;

            var neighborHood = GetNeighborhood(voxel);
            bool inWater = (neighborHood[1, 1, 1].IsValid && neighborHood[1, 1, 1].WaterCell.WaterLevel > WaterManager.inWaterThreshold);
            bool standingOnGround = (neighborHood[1, 0, 1].IsValid && !neighborHood[1, 0, 1].IsEmpty);
            bool topCovered = (neighborHood[1, 2, 1].IsValid && !neighborHood[1, 2, 1].IsEmpty);
            bool hasNeighbors = HasNeighbors(neighborHood);
            bool isClimbing = false;

            var successors = new List<MoveAction>();
            bool isRiding = state.VehicleState.IsRidingVehicle;

            if (CanClimb || Can(MoveType.RideVehicle))
            {
                //Climbing ladders.
                var bodies = objectHash.EnumerateIntersectingObjects(voxel.GetBoundingBox(), CollisionManager.CollisionType.Static).OfType<GameComponent>();

                if (!isRiding)
                {
                    var ladder = bodies.FirstOrDefault(component => component.Tags.Contains("Climbable"));

                    // if the creature can climb objects and a ladder is in this voxel,
                    // then add a climb action.
                    if (ladder != null && CanClimb)
                    {
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(1, 2, 1),
                            MoveType = MoveType.Climb,
                            InteractObject = ladder
                        });

                        isClimbing = true;

                        if (!standingOnGround)
                        {
                            successors.Add(new MoveAction
                            {
                                Diff = new Vector3(1, 0, 1),
                                MoveType = MoveType.Climb,
                                InteractObject = ladder
                            });
                        }

                        standingOnGround = true;
                    }
                }

                if (!isRiding)
                {
                    var rails = bodies.OfType<Rail.RailEntity>().Where(r => r.Active);

                    if (rails.Count() > 0 && Can(MoveType.RideVehicle))
                    {
                        {
                            foreach (var rail in rails)
                            {

                                if (rail.GetContainingVoxel() != state.Voxel)
                                    continue;

                                
                                successors.Add(new MoveAction()
                                {
                                    SourceState = state,
                                    DestinationState = new MoveState()
                                    {
                                        VehicleState = new VehicleState()
                                        {
                                            Rail = rail
                                        },
                                        Voxel = rail.GetContainingVoxel()
                                    },
                                    MoveType = MoveType.EnterVehicle,
                                    Diff = new Vector3(1, 1, 1)
                                });
                            }
                        }
                    }
                }

                if (Can(MoveType.ExitVehicle) && isRiding)
                {
                    successors.Add(new MoveAction()
                    {
                        SourceState = state,
                        DestinationState = new MoveState()
                        {
                            VehicleState = new VehicleState(),
                            Voxel = state.Voxel
                        },
                        MoveType = MoveType.ExitVehicle,
                        Diff = new Vector3(1, 1, 1)
                    });
                }

                if (Can(MoveType.RideVehicle) && isRiding)
                {
                    foreach(var neighbor in Rail.RailHelper.EnumerateForwardNetworkConnections(state.VehicleState.PrevRail, state.VehicleState.Rail))
                    {
                        var neighborRail =  Creature.Manager.FindComponent(neighbor) as Rail.RailEntity;
                        if (neighborRail == null || !neighborRail.Active)
                            continue;

                        successors.Add(new MoveAction()
                        {
                            SourceState = state,
                            DestinationState = new MoveState()
                            {
                                Voxel = neighborRail.GetContainingVoxel(),
                                VehicleState = new VehicleState()
                                {
                                    Rail = neighborRail,
                                    PrevRail = state.VehicleState.Rail
                                }
                            },
                            MoveType = MoveType.RideVehicle,
                        });
                    }
                }
            }

            // If the creature can climb walls and is not blocked by a voxl above.
            if (!isRiding && CanClimbWalls && !topCovered)
            {
                var walls = new VoxelHandle[]
                {
                    neighborHood[2, 1, 1], neighborHood[0, 1, 1], neighborHood[1, 1, 2],
                    neighborHood[1, 1, 0]
                };

                var wall = VoxelHandle.InvalidHandle;
                foreach (var w in walls)
                    if (w.IsValid && !w.IsEmpty)
                        wall = w;

                if (wall.IsValid)
                {
                    isClimbing = true;
                    successors.Add(new MoveAction
                    {
                        Diff = new Vector3(1, 2, 1),
                        MoveType = MoveType.ClimbWalls,
                        ActionVoxel = wall
                    });

                    if (!standingOnGround)
                    {
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(1, 0, 1),
                            MoveType = MoveType.ClimbWalls,
                            ActionVoxel = wall
                        });
                    }
                }
            }

            // If the creature either can walk or is in water, add the 
            // eight-connected free neighbors around the voxel.
            if (!isRiding && ((CanWalk && standingOnGround) || (CanSwim && inWater)))
            {
                // If the creature is in water, it can swim. Otherwise, it will walk.
                var moveType = inWater ? MoveType.Swim : MoveType.Walk;
                if (!neighborHood[0, 1, 1].IsValid || neighborHood[0,1,1].IsEmpty)
                    // +- x
                    successors.Add(new MoveAction
                    {
                        Diff = new Vector3(0, 1, 1),
                        MoveType = moveType
                    });

                if (!neighborHood[2, 1, 1].IsValid || neighborHood[2, 1, 1].IsEmpty)
                    successors.Add(new MoveAction
                    {
                        Diff = new Vector3(2, 1, 1),
                        MoveType = moveType
                    });

                if (!neighborHood[1, 1, 0].IsValid || neighborHood[1, 1, 0].IsEmpty)
                    // +- z
                    successors.Add(new MoveAction
                    {
                        Diff = new Vector3(1, 1, 0),
                        MoveType = moveType
                    });

                if (!neighborHood[1, 1, 2].IsValid || neighborHood[1, 1, 2].IsEmpty)
                    successors.Add(new MoveAction
                    {
                        Diff = new Vector3(1, 1, 2),
                        MoveType = moveType
                    });

                // Only bother worrying about 8-connected movement if there are
                // no full neighbors around the voxel.
                if (!hasNeighbors)
                {
                    if (!neighborHood[2, 1, 2].IsValid || neighborHood[2, 1, 2].IsEmpty)
                        // +x + z
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(2, 1, 2),
                            MoveType = moveType
                        });

                    if (!neighborHood[2, 1, 0].IsValid || neighborHood[2, 1, 0].IsEmpty)
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(2, 1, 0),
                            MoveType = moveType
                        });

                    if (!neighborHood[0, 1, 2].IsValid || neighborHood[0, 1, 2].IsEmpty)
                        // -x -z
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(0, 1, 2),
                            MoveType = moveType
                        });

                    if (!neighborHood[0, 1, 0].IsValid || neighborHood[0, 1, 0].IsEmpty)
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
            if (!isRiding && (!topCovered && (standingOnGround || (CanSwim && inWater) || isClimbing)))
            {
                for (int dx = 0; dx <= 2; dx++)
                {
                    for (int dz = 0; dz <= 2; dz++)
                    {
                        if (dx == 1 && dz == 1) continue;

                        if (neighborHood[dx, 1, dz].IsValid && !neighborHood[dx, 1, dz].IsEmpty)
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
            if (!isRiding && !inWater && !standingOnGround)
            {
                successors.Add(new MoveAction
                {
                    Diff = new Vector3(1, 0, 1),
                    MoveType = MoveType.Fall
                });
            }

            // If the creature can fly and is not underwater, it can fly
            // to any adjacent empty cell.
            if (!isRiding && CanFly && !inWater)
            {
                for (int dx = 0; dx <= 2; dx++)
                {
                    for (int dz = 0; dz <= 2; dz++)
                    {
                        for (int dy = 0; dy <= 2; dy++)
                        {
                            if (dx == 1 && dz == 1 && dy == 1) continue;

                            if (!neighborHood[dx, 1, dz].IsValid || neighborHood[dx, 1, dz].IsEmpty)
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
                var n = v.DestinationVoxel.IsValid ? v.DestinationVoxel : neighborHood[(int)v.Diff.X, (int)v.Diff.Y, (int)v.Diff.Z];
                if (n.IsValid && (isRiding || n.IsEmpty || n.WaterCell.WaterLevel > 0))
                {
                    // Do one final check to see if there is an object blocking the motion.
                    bool blockedByObject = false;
                    if (!isRiding)
                    {
                        var objectsAtNeighbor = Creature.Manager.World.CollisionManager.EnumerateIntersectingObjects(
                            n.GetBoundingBox(), CollisionManager.CollisionType.Static);

                        // If there is an object blocking the motion, determine if it can be passed through.

                        foreach (var body in objectsAtNeighbor.OfType<GameComponent>())
                        {
                            var door = body.GetRoot().EnumerateAll().OfType<Door>().FirstOrDefault();
                            // If there is an enemy door blocking movement, we can destroy it to get through.
                            if (door != null)
                            {
                                if (
                                    Creature.World.Diplomacy.GetPolitics(door.TeamFaction, Creature.Faction)
                                        .GetCurrentRelationship() ==
                                    Relationship.Hateful)
                                {
                                    if (Can(MoveType.DestroyObject))
                                        yield return (new MoveAction
                                        {
                                            Diff = v.Diff,
                                            MoveType = MoveType.DestroyObject,
                                            InteractObject = door,
                                            DestinationVoxel = n,
                                            SourceVoxel = voxel
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
                        newAction.SourceVoxel = voxel;
                        newAction.DestinationVoxel = n;
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

        private GameComponent GetBodyAt(VoxelHandle voxel, CollisionManager objectHash, string tag)
        {
            return objectHash.EnumerateIntersectingObjects(voxel.GetBoundingBox(),
                CollisionManager.CollisionType.Static).OfType<GameComponent>().FirstOrDefault(component => component.Tags.Contains(tag));
        }


        public IEnumerable<MoveAction> GetInverseMoveActions_Experimental(VoxelHandle voxel)
        {
            if (!voxel.IsValid || !voxel.IsEmpty)
                yield break;

            CollisionManager objectHash = Creature.Manager.World.CollisionManager;

            var neighborHood = GetNeighborhood(voxel);
            bool inWater = (neighborHood[1, 1, 1].IsValid && neighborHood[1, 1, 1].WaterCell.WaterLevel > WaterManager.inWaterThreshold);
            bool standingOnGround = (neighborHood[1, 0, 1].IsValid && !neighborHood[1, 0, 1].IsEmpty);
            bool topCovered = (neighborHood[1, 2, 1].IsValid && !neighborHood[1, 2, 1].IsEmpty);
            bool hasNeighbors = HasNeighbors(neighborHood);
            bool isClimbing = false;

            var successors = new List<MoveAction>();

            if (CanClimb)
            {
                var ladderAt = GetBodyAt(voxel, objectHash, "Climbable");
                var ladderAbove = GetBodyAt(VoxelHelpers.GetVoxelAbove(voxel), objectHash, "Climbable");
                var ladderBelow = GetBodyAt(VoxelHelpers.GetNeighbor(voxel, new GlobalVoxelOffset(0, -1, 0)), objectHash, "Climbable");
                // If there was a ladder above the creature, we could have climbed down it to get here.
                if (ladderAbove != null)
                {
                    successors.Add(new MoveAction
                    {
                        Diff = new Vector3(1, 2, 1),
                        MoveType = MoveType.Climb,
                        InteractObject = ladderAbove
                    });
                }

                // If there was a ladder below the creature, we could have climbed up to get here.
                if (ladderBelow != null)
                {
                    successors.Add(new MoveAction
                    {
                        Diff = new Vector3(1, 0, 1),
                        MoveType = MoveType.Climb,
                        InteractObject = ladderBelow
                    });
                }

                if (ladderAt != null)
                {
                    standingOnGround = true;
                    isClimbing = true;
                }
            }

            // If the creature can climb walls, check to see if there are any walls nearby that we could have climbed here from.
            if (CanClimbWalls && !topCovered)
            {

                // First check the voxel above to see if it has any walls nearby.
                if (!topCovered)
                {
                    var wallsAbove = new VoxelHandle[]
                    {
                        neighborHood[2, 2, 1], neighborHood[0, 2, 1], neighborHood[1, 2, 2],
                        neighborHood[1, 2, 0]
                    };
                    var wallAbove = VoxelHandle.InvalidHandle;
                    foreach (var w in wallsAbove)
                        if (w.IsValid && !w.IsEmpty)
                        {
                            wallAbove = w;
                            break;
                        }
                   

                    // If there was a wall above us, we could have climbed down to get here.
                    if (wallAbove.IsValid)
                    {
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(1, 2, 1),
                            MoveType = MoveType.ClimbWalls,
                            ActionVoxel = wallAbove
                        });
                    }
                }

                // Now check the walls below
                if (!standingOnGround)
                {
                    var wallsBelow = new VoxelHandle[]
                    {
                        neighborHood[2, 0, 1], neighborHood[0, 0, 1], neighborHood[1, 0, 2],
                        neighborHood[1, 0, 0]
                    };
                    var wallBelow = VoxelHandle.InvalidHandle;
                    foreach (var w in wallsBelow)
                        if (w.IsValid && !w.IsEmpty)
                        {
                            wallBelow = w;
                            break;
                        }


                    // If there was a wall below us, we could have climbed up to get here.
                    if (wallBelow.IsValid)
                    {
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(1, 0, 1),
                            MoveType = MoveType.ClimbWalls,
                            ActionVoxel = wallBelow
                        });
                    }
                }
            }

            // If the creature either can walk or is in water, add the 
            // eight-connected free neighbors around the voxel.
            if (CanWalk || CanSwim)
            {
                // If the creature is in water, it can swim. Otherwise, it will walk.
                var moveType = inWater ? MoveType.Swim : MoveType.Walk;
                if (neighborHood[0, 1, 1].IsValid &&
                    neighborHood[0, 1, 1].IsEmpty)
                {
                    if (moveType == MoveType.Swim || (neighborHood[0, 0, 1].IsValid &&
                    !neighborHood[0, 0, 1].IsEmpty))
                    {
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(0, 1, 1),
                            MoveType = moveType
                        });
                    }
                }

                if (neighborHood[2, 1, 1].IsValid && neighborHood[2, 1, 1].IsEmpty)
                {
                    if (moveType == MoveType.Swim || (neighborHood[2, 0, 1].IsValid &&
                    !neighborHood[2, 0, 1].IsEmpty))
                    {
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(2, 1, 1),
                            MoveType = moveType
                        });
                    }
                }


                if (neighborHood[1, 1, 0].IsValid && neighborHood[1, 1, 0].IsEmpty)
                {
                    if (moveType == MoveType.Swim || (neighborHood[1, 0, 0].IsValid &&
                    !neighborHood[1, 0, 0].IsEmpty))
                    {
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(1, 1, 0),
                            MoveType = moveType
                        });
                    }
                }

                if (neighborHood[1, 1, 2].IsValid && neighborHood[1, 1, 2].IsEmpty)
                {
                    if (moveType == MoveType.Swim || (neighborHood[1, 0, 2].IsValid &&
                    !neighborHood[1, 0, 2].IsEmpty))
                    {
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(1, 1, 2),
                            MoveType = moveType
                        });
                    }
                }

                // Only bother worrying about 8-connected movement if there are
                // no full neighbors around the voxel.
                if (!hasNeighbors)
                {
                    if (neighborHood[2, 1, 2].IsValid && neighborHood[2, 1, 2].IsEmpty)
                    {
                        if (moveType == MoveType.Swim || (neighborHood[2, 0, 2].IsValid &&
                        !neighborHood[2, 0, 2].IsEmpty))
                        {
                            successors.Add(new MoveAction
                            {
                                Diff = new Vector3(2, 1, 2),
                                MoveType = moveType
                            });
                        }
                    }

                    if (neighborHood[2, 1, 0].IsValid && neighborHood[2, 1, 0].IsEmpty)
                    {
                        if (moveType == MoveType.Swim || (neighborHood[2, 0, 0].IsValid &&
                        !neighborHood[2, 0, 0].IsEmpty))
                        {
                            successors.Add(new MoveAction
                            {
                                Diff = new Vector3(2, 1, 0),
                                MoveType = moveType
                            });
                        }
                    }

                    if (neighborHood[0, 1, 2].IsValid && neighborHood[0, 1, 2].IsEmpty)
                    {
                        if (moveType == MoveType.Swim || (neighborHood[0, 0, 2].IsValid &&
                        !neighborHood[0, 0, 2].IsEmpty))
                        {
                            successors.Add(new MoveAction
                            {
                                Diff = new Vector3(0, 1, 2),
                                MoveType = moveType
                            });
                        }
                    }

                    if (neighborHood[0, 1, 0].IsValid && neighborHood[0, 1, 0].IsEmpty)
                    {
                        if (moveType == MoveType.Swim || (neighborHood[0, 0, 0].IsValid &&
                        !neighborHood[0, 0, 0].IsEmpty))
                        {
                            successors.Add(new MoveAction
                            {
                                Diff = new Vector3(0, 1, 0),
                                MoveType = moveType
                            });
                        }
                    }
                }
            }

            // Now a somewhat tricky part. We've got to figure out which cells can jump to this cell.
            // The rules for jumping are as follows:
            // 1) The voxel must be exactly 1 below this one.
            // 2) The voxel must be exactly 1 away in the other dimensions
            // 3) The voxel above the one we're jumping from must be free.
            // 4) The voxel below the one we're jumping from must be filled, or be in water, or
            //    must be part of a climb action.
            // Step 4 is really hard, so let's ignore it for now.

            // First, check the 3x3 neighborhood around the voxel.
            if (standingOnGround)
            {
                for (int dx = 0; dx <= 2; dx++)
                {
                    for (int dz = 0; dz <= 2; dz++)
                    {
                        // Ignore the current voxel (can't jump from there)
                        if (dx == 1 && dz == 1) continue;
                        // Can't jump from neighbor because neighbor's head isn't clear.
                        if (!(neighborHood[dx, 1, dz].IsValid && neighborHood[dx, 1, dz].IsEmpty)) continue;
                        
                        // Now just assume we can jump from the neighbor below us. TODO (step 4)
                        successors.Add(new MoveAction
                        {
                            Diff = new Vector3(dx, 0, dz),
                            MoveType = MoveType.Jump
                        });
                    }
                }
            }


            // If the creature is not in water and is not standing on ground,
            // it can fall one voxel downward in free space. So check the voxel above,
            // if it is free and has no water, then we could have fallen from it.
            if (neighborHood[1, 2, 1].IsValid && neighborHood[1, 2, 1].IsEmpty && neighborHood[1, 2, 1].WaterCell.WaterLevel == 0)
            {
                successors.Add(new MoveAction
                {
                    Diff = new Vector3(1, 2, 1),
                    MoveType = MoveType.Fall
                });
            }

            // If the creature can fly and is not underwater, it can fly
            // to any adjacent empty cell. Luckily this is reversible so we don't have to change
            // anything from GetMoveactions
            if (CanFly && !inWater)
            {
                for (int dx = 0; dx <= 2; dx++)
                {
                    for (int dz = 0; dz <= 2; dz++)
                    {
                        for (int dy = 0; dy <= 2; dy++)
                        {
                            if (dx == 1 && dz == 1 && dy == 1) continue;

                            if (!neighborHood[dx, 1, dz].IsValid || neighborHood[dx, 1, dz].IsEmpty)
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
                var n = neighborHood[(int)v.Diff.X, (int)v.Diff.Y, (int)v.Diff.Z];
                if (n.IsValid && (n.IsEmpty || n.WaterCell.WaterLevel > 0))
                {
                    // Do one final check to see if there is an object blocking the motion.
                    bool blockedByObject = false;
                    var objectsAtNeighbor = Creature.Manager.World.CollisionManager.EnumerateIntersectingObjects(
                        n.GetBoundingBox(), CollisionManager.CollisionType.Static);

                    // If there is an object blocking the motion, determine if it can be passed through.

                    foreach (var body in objectsAtNeighbor.OfType<GameComponent>())
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
                                        Diff = -(v.Diff - Vector3.One),
                                        MoveType = MoveType.DestroyObject,
                                        InteractObject = door,
                                        DestinationVoxel = voxel,
                                        SourceVoxel = n
                                    });
                                blockedByObject = true;
                            }
                        }
                    }

                    // If no object blocked us, we can move freely as normal.
                    if (!blockedByObject)
                    {
                        MoveAction newAction = v;
                        newAction.Diff = -(v.Diff - Vector3.One);
                        newAction.DestinationVoxel = voxel;
                        newAction.SourceVoxel = n;
                        yield return newAction;
                    }
                }
            }
        }

        // Inverts GetMoveActions. So, returns the list of move actions whose target is the current voxel.
        // Very, very slow.
        public IEnumerable<MoveAction> GetInverseMoveActions(MoveState currentstate)
        {
            CollisionManager objectHash = Creature.Manager.World.CollisionManager;
            var current = currentstate.Voxel;
            foreach (var v in VoxelHelpers.EnumerateAllNeighbors(current.Coordinate)
                .Select(n => new VoxelHandle(current.Chunk.Manager.ChunkData, n))
                .Where(h => h.IsValid && h.IsEmpty))
            {
                foreach (var a in GetMoveActions(new MoveState() { Voxel = v}).Where(a => a.DestinationState == currentstate))
                    yield return a;

                // Now that dwarfs can ride vehicles, the inverse of the move actions becomes extremely complicated. We must now
                // iterate through all rails intersecting every neighbor and see if we can find a connection from that rail to this one.
                // Further, we must iterate through the entire rail network and enumerate all possible directions in and out of that rail.
                // Yay!
                var bodies = objectHash.EnumerateIntersectingObjects(v.GetBoundingBox(), CollisionManager.CollisionType.Static);
                var rails = bodies.OfType<Rail.RailEntity>().Where(r => r.Active);
                foreach (var rail in rails)
                {
                    if (rail.GetContainingVoxel() != v)
                        continue;

                    foreach (var neighborRail in rail.NeighborRails.Select(neighbor => Creature.Manager.FindComponent(neighbor.NeighborID) as Rail.RailEntity))
                        foreach (var a in GetMoveActions(new MoveState() { Voxel = v, VehicleState = new VehicleState() { Rail = rail, PrevRail = neighborRail } }).Where(a => a.DestinationState == currentstate))
                         yield return a;

                    foreach (var a in GetMoveActions(new MoveState() { Voxel = v, VehicleState = new VehicleState() { Rail = rail, PrevRail = null } }).Where(a => a.DestinationState == currentstate))
                        yield return a;
                }
            }
        }
    }
}
