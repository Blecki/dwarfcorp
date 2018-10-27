// WanderAct.cs
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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A creature randomly applies force at intervals to itself.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class WanderAct : CreatureAct
    {
        public Timer WanderTime { get; set; }
        public Timer TurnTime { get; set; }
        public float Radius { get; set; }
        public Vector3 LocalTarget { get; set; }
        public WanderAct()
        {
            
        }

        public WanderAct(CreatureAI creature, float seconds, float turnTime, float radius) :
            base(creature)
        {
            Name = "Wander";
            WanderTime = new Timer(seconds, false);
            TurnTime = new Timer(turnTime, false);
            Radius = radius;
        }

        public override void Initialize()
        {
            WanderTime.Reset(WanderTime.TargetTimeSeconds);
            TurnTime.Reset(TurnTime.TargetTimeSeconds);
            LocalTarget = Agent.Position;
            base.Initialize();
        }


        public override IEnumerable<Status> Run()
        {
            Vector3 oldPosition = Agent.Position;
            bool firstIter = true;
            Creature.Controller.Reset();
            WanderTime.Reset();
            TurnTime.Reset();
            while(!WanderTime.HasTriggered)
            {
                Creature.OverrideCharacterMode = false;
                Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                Creature.CurrentCharacterMode = CharacterMode.Walking;
                WanderTime.Update(DwarfTime.LastTime);

                if (!Creature.IsOnGround)
                {
                    yield return Status.Success;
                    yield break;
                }
                if(TurnTime.Update(DwarfTime.LastTime) || TurnTime.HasTriggered || firstIter)
                {
                    int iters = 0;

                    while (iters < 100)
                    {
                        iters++;
                        Vector2 randTarget = MathFunctions.RandVector2Circle() * Radius;
                        LocalTarget = new Vector3(randTarget.X, 0, randTarget.Y) + oldPosition;
                        VoxelHandle voxel = new VoxelHandle(Agent.World.ChunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(LocalTarget));
                        bool foundLava = false;
                        foreach (VoxelHandle neighbor in VoxelHelpers.EnumerateAllNeighbors(voxel.Coordinate).Select(coord => new VoxelHandle(Agent.World.ChunkManager.ChunkData, coord)))
                        {
                            if (neighbor.IsValid && neighbor.Chunk != null)
                            {
                                if (neighbor.LiquidType == LiquidType.Lava)
                                {
                                    foundLava = true;
                                }
                            }
                        }
                        if (!foundLava)
                        {
                            break;
                        }
                    }

                    if (iters == 100)
                    {
                        yield return Act.Status.Fail;
                        yield break;
                    }
                    firstIter = false;
                    TurnTime.Reset(TurnTime.TargetTimeSeconds + MathFunctions.Rand(-0.1f, 0.1f));
                }

                float dist = (LocalTarget - Agent.Position).Length();


                if (dist < 0.5f)
                {
                    Creature.Physics.Velocity *= 0.0f;
                    Creature.CurrentCharacterMode = CharacterMode.Idle;
                    yield return Status.Running;
                }
                else
                {
                    
                    Vector3 output =
                        Creature.Controller.GetOutput((float) DwarfTime.LastTime.ElapsedGameTime.TotalSeconds,
                            LocalTarget, Agent.Position);
                    output.Y = 0.0f;

                    Creature.Physics.ApplyForce(output * 0.5f, (float) DwarfTime.LastTime.ElapsedGameTime.TotalSeconds);
                    Creature.CurrentCharacterMode = CharacterMode.Walking;
                }

                yield return Status.Running;
            }
            Creature.CurrentCharacterMode = CharacterMode.Idle;
            yield return Status.Success;
        }
    }

    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class LongWanderAct : CompoundCreatureAct
    {
        public int PathLength { get; set; }
        public float Radius { get; set; }
        public bool Is2D { get; set; }

        public LongWanderAct()
        {
            
        }

        public LongWanderAct(CreatureAI creature) : base(creature)
        {
            
        }

        public IEnumerable<Status> FindRandomPath()
        {
            Vector3 target = MathFunctions.RandVector3Cube()*Radius + Creature.AI.Position;
            if (Creature.AI.PositionConstraint.Contains(target) == ContainmentType.Disjoint)
            {
                target = MathFunctions.RandVector3Box(Creature.AI.PositionConstraint);
            }
            if (Is2D) target.Y = Creature.AI.Position.Y;
            List<MoveAction> path = new List<MoveAction>();
            VoxelHandle curr = Creature.Physics.CurrentVoxel;
            var bodies = Agent.World.PlayerFaction.OwnedObjects.Where(o => o.Tags.Contains("Teleporter")).ToList();
            var voxelNeighborhood = new VoxelHandle[3, 3, 3];

            for (int i = 0; i < PathLength; i++)
            {
                var actions = Creature.AI.Movement.GetMoveActions(new MoveState() { Voxel = curr }, Creature.World.OctTree, bodies, voxelNeighborhood);

                MoveAction? bestAction = null;
                float bestDist = float.MaxValue;
                foreach (MoveAction action in actions)
                {
                    if (Is2D && (action.MoveType == MoveType.Climb || action.MoveType == MoveType.ClimbWalls))
                    {
                        continue;
                    }
                    float dist = (action.DestinationVoxel.WorldPosition - target).LengthSquared() * Creature.AI.Movement.Cost(action.MoveType);

                    if (dist < bestDist && ! path.Any(a => a.DestinationVoxel == action.DestinationVoxel))
                    {
                        bestDist = dist;
                        bestAction = action;
                    }
                }

                if (bestAction.HasValue && 
                    !path.Any(p => p.DestinationVoxel.Equals(bestAction.Value.DestinationVoxel) && p.MoveType == bestAction.Value.MoveType &&
                    Creature.AI.PositionConstraint.Contains(bestAction.Value.DestinationVoxel.WorldPosition + Vector3.One * 0.5f) == ContainmentType.Contains))
                {
                    MoveAction action = bestAction.Value;
                    action.DestinationVoxel = curr;
                    path.Add(action);
                    curr = bestAction.Value.DestinationVoxel;
                }
                else
                {
                    break;
                }
            }
            if (path.Count > 0)
            {
                Creature.AI.Blackboard.SetData("RandomPath", path);
                yield return Status.Success;
            }
            else
            {
                yield return Status.Fail;
            }
        }

        public override void Initialize()
        {
            Tree = new Sequence(new Wrap(FindRandomPath), new FollowPathAct(Creature.AI, "RandomPath"));
            base.Initialize();
        }
    }

}