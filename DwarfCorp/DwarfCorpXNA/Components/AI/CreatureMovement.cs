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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class CreatureMovement
    {

        private int TeleportDistance = SummoningCircle.TeleportDistance;
        private int TeleportDistanceSquared = SummoningCircle.TeleportDistance * SummoningCircle.TeleportDistance;

        public CreatureMovement()
        {

        }

        public CreatureMovement(CreatureAI Parent)
        {
            this.Parent = Parent;
            Actions = new Dictionary<MoveType, ActionStats>
            {
                {
                    MoveType.Teleport,
                    new ActionStats
                    {
                        CanMove = false,
                        Cost = 1.0f,
                        Speed = 1.0f
                    }
                },
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
                    MoveType.Dig,
                    new ActionStats
                    {
                        CanMove = false,
                        Cost = 500.0f,
                        Speed = 1.0f
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

        private bool _isDwarf = false;
        private bool _checkedDwarf = false;
        public bool IsDwarf
        {
            get
            {
                if (_checkedDwarf)
                    return _isDwarf;
                _isDwarf = (Parent != null) && Parent.Active && (Parent.Creature != null && Parent.Faction == Parent.World.PlayerFaction);
                _checkedDwarf = true;
                return _isDwarf;
            }
        }

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

        [JsonIgnore]
        public bool CanDig
        {
            get { return Can(MoveType.Dig); }
            set { SetCan(MoveType.Dig, value); }
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
        private void GetNeighborhood(ChunkData chunks, VoxelHandle Voxel, VoxelHandle[,,] Into)
        {
            for (var dx = -1; dx <= 1; ++dx)
                for (var dy = -1; dy <= 1; ++dy)
                    for (var dz = -1; dz <= 1; ++dz)
                    {
                        var v = new VoxelHandle(chunks,
                            Voxel.Coordinate + new GlobalVoxelOffset(dx, dy, dz));
                        Into[dx + 1, dy + 1, dz + 1] = v;
                    }
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
        public IEnumerable<MoveAction> GetMoveActions(Vector3 Pos, OctTreeNode<Body> octree, List<Body> teleportObjects)
        {
            var vox = new VoxelHandle(Creature.World.ChunkManager.ChunkData,
                GlobalVoxelCoordinate.FromVector3(Pos));
            return GetMoveActions(new MoveState() { Voxel = vox }, octree, teleportObjects, null);
        }



        /// <summary> gets the list of actions that the creature can take from a given voxel. </summary>
        public IEnumerable<MoveAction> GetMoveActions(MoveState state, OctTreeNode<Body> OctTree, List<Body> teleportObjects, MoveActionTempStorage Storage)
        {
            if (Parent == null)
                yield break;

            if (!state.Voxel.IsValid)
                yield break;

            if (Creature == null)
                yield break;

            if (Storage == null)
                Storage = new MoveActionTempStorage();

            GetNeighborhood(state.Voxel.Chunk.Manager.ChunkData, state.Voxel, Storage.Neighborhood);

            bool inWater = (Storage.Neighborhood[1, 1, 1].IsValid && Storage.Neighborhood[1, 1, 1].LiquidLevel > WaterManager.inWaterThreshold);
            bool standingOnGround = (Storage.Neighborhood[1, 0, 1].IsValid && !Storage.Neighborhood[1, 0, 1].IsEmpty);
            bool topCovered = (Storage.Neighborhood[1, 2, 1].IsValid && !Storage.Neighborhood[1, 2, 1].IsEmpty);
            bool hasNeighbors = HasNeighbors(Storage.Neighborhood);
            bool isRiding = state.VehicleState.IsRidingVehicle;

            var neighborHoodBounds = new BoundingBox(Storage.Neighborhood[0, 0, 0].GetBoundingBox().Min, Storage.Neighborhood[2, 2, 2].GetBoundingBox().Max);
            Storage.NeighborObjects.Clear();
            OctTree.EnumerateItems(neighborHoodBounds, Storage.NeighborObjects);

            if (Can(MoveType.Teleport))
                foreach (var obj in teleportObjects)
                    if ((obj.Position - state.Voxel.WorldPosition).LengthSquared() < TeleportDistanceSquared)
                        yield return new MoveAction()
                        {
                            InteractObject = obj,
                            MoveType = MoveType.Teleport,
                            SourceVoxel = state.Voxel,
                            DestinationState = new MoveState()
                            {
                                Voxel = new VoxelHandle(state.Voxel.Chunk.Manager.ChunkData, GlobalVoxelCoordinate.FromVector3(obj.Position))
                            }
                        };

            var successors = EnumerateSuccessors(state, state.Voxel, Storage, inWater, standingOnGround, topCovered, hasNeighbors, isRiding);

            // Now, validate each move action that the creature might take.
            foreach (MoveAction v in successors)
            {
                var n = v.DestinationVoxel.IsValid ? v.DestinationVoxel : Storage.Neighborhood[(int)v.Diff.X, (int)v.Diff.Y, (int)v.Diff.Z];
                if (n.IsValid && (v.MoveType == MoveType.Dig || isRiding || n.IsEmpty || n.LiquidLevel > 0))
                {
                    // Do one final check to see if there is an object blocking the motion.
                    bool blockedByObject = false;
                    if (!isRiding)
                    {
                        var objectsAtNeighbor = Storage.NeighborObjects.Where(o => o.GetBoundingBox().Intersects(n.GetBoundingBox()));

                        // If there is an object blocking the motion, determine if it can be passed through.

                        foreach (var body in objectsAtNeighbor)
                        {
                            var door = body as Door;
                            // ** Doors are in the octtree, pretty sure this was always pointless -- var door = body.GetRoot().EnumerateAll().OfType<Door>().FirstOrDefault();
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
                                            SourceState = state
                                        });
                                    blockedByObject = true;
                                }
                            }
                        }
                    }

                    // If no object blocked us, we can move freely as normal.
                    if (!blockedByObject && n.LiquidType != LiquidType.Lava)
                    {
                        MoveAction newAction = v;
                        newAction.SourceState = state;
                        newAction.DestinationVoxel = n;
                        yield return newAction;
                    }
                }
            }
        }

        private IEnumerable<MoveAction> EnumerateSuccessors(
            MoveState state, 
            VoxelHandle voxel, 
            MoveActionTempStorage Storage, 
            bool inWater, 
            bool standingOnGround, 
            bool topCovered, 
            bool hasNeighbors, 
            bool isRiding)
        {
            bool isClimbing = false;

            if (isRiding)
            {
                if (Can(MoveType.ExitVehicle)) // Possibly redundant... If they can ride they should be able to exit right?
                {
                    yield return (new MoveAction()
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

                if (Can(MoveType.RideVehicle))
                {
                    foreach (var neighbor in Rail.RailHelper.EnumerateForwardNetworkConnections(state.VehicleState.PrevRail, state.VehicleState.Rail))
                    {
                        var neighborRail = Creature.Manager.FindComponent(neighbor) as Rail.RailEntity;
                        if (neighborRail == null || !neighborRail.Active)
                            continue;

                        yield return (new MoveAction()
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

                yield break; // Nothing can be done without exiting the rails first.
            }

            if (CanClimb || Can(MoveType.RideVehicle))
            {
                //Climbing ladders and riding rails.                

                var bodies = Storage.NeighborObjects.Where(o => o.GetBoundingBox().Intersects(voxel.GetBoundingBox()));

                // if the creature can climb objects and a ladder is in this voxel,
                // then add a climb action.
                if (CanClimb)
                {
                    var ladder = bodies.FirstOrDefault(component => component.Tags.Contains("Climbable"));

                    if (ladder != null)
                    {
                        yield return new MoveAction
                        {
                            Diff = new Vector3(1, 2, 1),
                            MoveType = MoveType.Climb,
                            InteractObject = ladder
                        };

                        if (!standingOnGround)
                        {
                            yield return (new MoveAction
                            {
                                Diff = new Vector3(1, 0, 1),
                                MoveType = MoveType.Climb,
                                InteractObject = ladder
                            });
                        }
                        standingOnGround = true;
                    }
                }

                if (Can(MoveType.RideVehicle))
                {
                    var rails = bodies.OfType<Rail.RailEntity>().Where(r => r.Active);

                    if (rails.Count() > 0 && Can(MoveType.RideVehicle))
                    {
                        {
                            foreach (var rail in rails)
                            {

                                if (rail.GetContainingVoxel() != state.Voxel)
                                    continue;


                                yield return (new MoveAction()
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

                            if (dy == 1 && standingOnGround)
                            {
                                continue;
                            }

                            if (!Storage.Neighborhood[dx, 1, dz].IsValid || Storage.Neighborhood[dx, 1, dz].IsEmpty)
                            {
                                yield return (new MoveAction
                                {
                                    Diff = new Vector3(dx, dy, dz),
                                    MoveType = MoveType.Fly
                                });
                            }
                        }
                    }
                }
            }

            // If the creature is not in water and is not standing on ground,
            // it can fall one voxel downward in free space.
            if (!inWater && !standingOnGround)
            {
                yield return (new MoveAction
                {
                    Diff = new Vector3(1, 0, 1),
                    MoveType = MoveType.Fall
                });
            }

            // If the creature can climb walls and is not blocked by a voxl above.
            if (CanClimbWalls && !topCovered)
            {
                // This monstrosity is unrolling an inner loop so that we don't have to allocate an array or
                // enumerators.
                var wall = VoxelHandle.InvalidHandle;
                var n211 = Storage.Neighborhood[2, 1, 1];
                if (n211.IsValid && !n211.IsEmpty)
                {
                    wall = n211;
                }
                else
                {
                    var n011 = Storage.Neighborhood[0, 1, 1];
                    if (n011.IsValid && !n011.IsEmpty)
                    {
                        wall = n011;
                    }
                    else
                    {
                        var n112 = Storage.Neighborhood[1, 1, 2];
                        if (n112.IsValid && !n112.IsEmpty)
                        {
                            wall = n112;
                        }
                        else
                        {
                            var n110 = Storage.Neighborhood[1, 1, 0];
                            if (n110.IsValid && !n110.IsEmpty)
                            {
                                wall = n110;
                            }
                        }
                    }
                }

                if (wall.IsValid)
                {
                    isClimbing = true;
                    yield return(new MoveAction
                    {
                        Diff = new Vector3(1, 2, 1),
                        MoveType = MoveType.ClimbWalls,
                        ActionVoxel = wall
                    });

                    if (!standingOnGround)
                    {
                        yield return(new MoveAction
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
            if ((CanWalk && standingOnGround) || (CanSwim && inWater))
            {
                // If the creature is in water, it can swim. Otherwise, it will walk.
                var moveType = inWater ? MoveType.Swim : MoveType.Walk;
                if (!Storage.Neighborhood[0, 1, 1].IsValid || Storage.Neighborhood[0, 1, 1].IsEmpty)
                    // +- x
                    yield return(new MoveAction
                    {
                        Diff = new Vector3(0, 1, 1),
                        MoveType = moveType
                    });

                if (!Storage.Neighborhood[2, 1, 1].IsValid || Storage.Neighborhood[2, 1, 1].IsEmpty)
                    yield return(new MoveAction
                    {
                        Diff = new Vector3(2, 1, 1),
                        MoveType = moveType
                    });

                if (!Storage.Neighborhood[1, 1, 0].IsValid || Storage.Neighborhood[1, 1, 0].IsEmpty)
                    // +- z
                    yield return(new MoveAction
                    {
                        Diff = new Vector3(1, 1, 0),
                        MoveType = moveType
                    });

                if (!Storage.Neighborhood[1, 1, 2].IsValid || Storage.Neighborhood[1, 1, 2].IsEmpty)
                    yield return(new MoveAction
                    {
                        Diff = new Vector3(1, 1, 2),
                        MoveType = moveType
                    });

                // Only bother worrying about 8-connected movement if there are
                // no full neighbors around the voxel.
                if (!hasNeighbors)
                {
                    if (!Storage.Neighborhood[2, 1, 2].IsValid || Storage.Neighborhood[2, 1, 2].IsEmpty)
                        // +x + z
                        yield return(new MoveAction
                        {
                            Diff = new Vector3(2, 1, 2),
                            MoveType = moveType
                        });

                    if (!Storage.Neighborhood[2, 1, 0].IsValid || Storage.Neighborhood[2, 1, 0].IsEmpty)
                        yield return(new MoveAction
                        {
                            Diff = new Vector3(2, 1, 0),
                            MoveType = moveType
                        });

                    if (!Storage.Neighborhood[0, 1, 2].IsValid || Storage.Neighborhood[0, 1, 2].IsEmpty)
                        // -x -z
                        yield return(new MoveAction
                        {
                            Diff = new Vector3(0, 1, 2),
                            MoveType = moveType
                        });

                    if (!Storage.Neighborhood[0, 1, 0].IsValid || Storage.Neighborhood[0, 1, 0].IsEmpty)
                        yield return(new MoveAction
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

                        if (Storage.Neighborhood[dx, 1, dz].IsValid && !Storage.Neighborhood[dx, 1, dz].IsEmpty)
                        {
                            yield return(new MoveAction
                            {
                                Diff = new Vector3(dx, 2, dz),
                                MoveType = MoveType.Jump
                            });
                        }
                    }
                }
            }            

            if (CanDig)
            {
                // This loop is unrolled for speed. It gets the manhattan neighbors and tells the creature that it can mine
                // the surrounding rock to get through.
                VoxelHandle neighbor = Storage.Neighborhood[0, 1, 1];
                if (neighbor.IsValid && !neighbor.IsEmpty && (!IsDwarf || !neighbor.IsPlayerBuilt))
                {
                    yield return (new MoveAction
                    {
                        Diff = new Vector3(0, 1, 1),
                        MoveType = MoveType.Dig,
                        DestinationVoxel = neighbor,
                    });
                }

                neighbor = Storage.Neighborhood[2, 1, 1];
                if (neighbor.IsValid && !neighbor.IsEmpty && (!IsDwarf || !neighbor.IsPlayerBuilt))
                {
                    yield return (new MoveAction
                    {
                        Diff = new Vector3(2, 1, 1),
                        MoveType = MoveType.Dig,
                        DestinationVoxel = neighbor,
                    });
                }

                neighbor = Storage.Neighborhood[1, 1, 2];
                if (neighbor.IsValid && !neighbor.IsEmpty && (!IsDwarf || !neighbor.IsPlayerBuilt))
                {
                    yield return (new MoveAction
                    {
                        Diff = new Vector3(1, 1, 2),
                        MoveType = MoveType.Dig,
                        DestinationVoxel = neighbor,
                    });
                }

                neighbor = Storage.Neighborhood[1, 1, 0];
                if (neighbor.IsValid && !neighbor.IsEmpty && (!IsDwarf || !neighbor.IsPlayerBuilt))
                {
                    yield return (new MoveAction
                    {
                        Diff = new Vector3(1, 1, 0),
                        MoveType = MoveType.Dig,
                        DestinationVoxel = neighbor,
                    });
                }

                neighbor = Storage.Neighborhood[1, 2, 1];
                if (neighbor.IsValid && !neighbor.IsEmpty && (!IsDwarf || !neighbor.IsPlayerBuilt))
                {
                    yield return (new MoveAction
                    {
                        Diff = new Vector3(1, 2, 1),
                        MoveType = MoveType.Dig,
                        DestinationVoxel = neighbor,
                    });
                }

                neighbor = Storage.Neighborhood[1, 0, 1];
                if (neighbor.IsValid && !neighbor.IsEmpty && (!IsDwarf || !neighbor.IsPlayerBuilt))
                {
                    yield return (new MoveAction
                    {
                        Diff = new Vector3(1, 0, 1),
                        MoveType = MoveType.Dig,
                        DestinationVoxel = neighbor,
                    });
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
        public IEnumerable<MoveAction> GetInverseMoveActions(MoveState currentstate, OctTreeNode<Body> OctTree, List<Body> teleportObjects)
        {
            if (Parent == null)
                yield break;

            if (Creature == null)
                yield break;

            var current = currentstate.Voxel;

            if (Can(MoveType.Teleport))
            {
                foreach (var obj in teleportObjects)
                {
                    if ((obj.Position - current.WorldPosition).LengthSquared() > 2)
                        continue;

                    for (int dx = -TeleportDistance; dx <= TeleportDistance; dx++)
                        for (int dz = -TeleportDistance; dz <= TeleportDistance; dz++)
                            for (int dy = -TeleportDistance; dy <= TeleportDistance; dy++)
                            {
                                if (dx * dx + dy * dy + dz * dz > TeleportDistanceSquared)
                                    continue;
                                VoxelHandle teleportNeighbor = new VoxelHandle(Parent.World.ChunkManager.ChunkData, current.Coordinate + new GlobalVoxelOffset(dx, dy, dz));

                                if (teleportNeighbor.IsValid && teleportNeighbor.IsEmpty && !VoxelHelpers.GetNeighbor(teleportNeighbor, new GlobalVoxelOffset(0, -1, 0)).IsEmpty)
                                {
                                    yield return new MoveAction()
                                    {
                                        InteractObject = obj,
                                        Diff = new Vector3(dx, dx, dz),
                                        SourceVoxel = teleportNeighbor,
                                        DestinationState = currentstate,
                                        MoveType = MoveType.Teleport
                                    };
                                }
                            }
                }
            }

            var storage = new MoveActionTempStorage();

            foreach (var v in VoxelHelpers.EnumerateCube(current.Coordinate)
                .Select(n => new VoxelHandle(current.Chunk.Manager.ChunkData, n))
                .Where(h => h.IsValid))
            {
                foreach (var a in GetMoveActions(new MoveState() { Voxel = v}, OctTree, teleportObjects, storage).Where(a => a.DestinationState == currentstate))
                    yield return a;

                if (!Can(MoveType.RideVehicle))
                    continue;

                // Now that dwarfs can ride vehicles, the inverse of the move actions becomes extremely complicated. We must now
                // iterate through all rails intersecting every neighbor and see if we can find a connection from that rail to this one.
                // Further, we must iterate through the entire rail network and enumerate all possible directions in and out of that rail.
                // Yay!

                // Actually - why not just not bother with rails when inverse pathing, since it should only be invoked when forward pathing fails anyway?
                var bodies = new HashSet<Body>();
                OctTree.EnumerateItems(v.GetBoundingBox(), bodies);
                var rails = bodies.OfType<Rail.RailEntity>().Where(r => r.Active);
                foreach (var rail in rails)
                {
                    if (rail.GetContainingVoxel() != v)
                        continue;
                  
                    foreach (var neighborRail in rail.NeighborRails.Select(neighbor => Creature.Manager.FindComponent(neighbor.NeighborID) as Rail.RailEntity))
                    {
                        var actions = GetMoveActions(new MoveState() {
                            Voxel = v, VehicleState = new VehicleState() { Rail = rail, PrevRail = neighborRail } }, OctTree, teleportObjects, storage);
                        foreach (var a in actions.Where(a => a.DestinationState == currentstate))
                        {
                            yield return a;                           
                        }
                    }

                    foreach (var a in GetMoveActions(new MoveState() { Voxel = v, VehicleState = new VehicleState() { Rail = rail, PrevRail = null } }, OctTree, teleportObjects, storage).Where(a => a.DestinationState == currentstate))
                        yield return a;
                }
            }
        }
    }
}
