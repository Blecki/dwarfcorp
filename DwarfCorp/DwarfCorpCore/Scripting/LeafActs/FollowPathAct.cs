// FollowPathAct.cs
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
using System.IO;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    // FollowPathAct.cs
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
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.Xna.Framework;

    namespace DwarfCorp
    {
        /// <summary>
        /// A creature moves along a planned path until the path is completed, or
        /// it detects failure.
        /// </summary>
        [Newtonsoft.Json.JsonObject(IsReference = true)]
        public class FollowPathAnimationAct : CreatureAct
        {
            private string PathName { get; set; }
            public Timer ValidPathTimer { get; set; }
            public Creature.MoveAction CurrentAction { get; set; }
            public Timer CurrentActionTimer { get; set; }
            public List<Creature.MoveAction> Path { get; set; }
            public int CurrentActionIndex { get; set; }
            public Physics.CollisionMode CollisionMode { get; set; }
            public float RandomTimeOffset { get; set; }
            public List<Vector3> RandomPositionOffsets { get; set; } 
            public FollowPathAnimationAct(CreatureAI agent, string pathName) :
                base(agent)
            {
                Name = "Follow path";
                PathName = pathName;
                ValidPathTimer = new Timer(.75f, false);
                CurrentActionIndex = -1;
                CollisionMode = Agent.Physics.CollideMode;
                RandomTimeOffset = MathFunctions.Rand(0.0f, 0.1f);
            }

            public List<Creature.MoveAction> GetPath()
            {
                return Agent.Blackboard.GetData<List<Creature.MoveAction>>(PathName);
            }

            public void SetPath(List<Creature.MoveAction> path)
            {
                Agent.Blackboard.SetData(PathName, path);
            }

            public bool IsPathValid(List<Creature.MoveAction> path)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    if (!path[i].Voxel.IsEmpty) return false;
                    List<Creature.MoveAction> neighbors = Agent.Movement.GetMoveActions(path[i].Voxel);
                    bool valid = false;
                    foreach (Creature.MoveAction vr in neighbors)
                    {
                        Vector3 dif = vr.Voxel.Position - path[i + 1].Voxel.Position;
                        if (dif.Length() < .1)
                        {
                            valid = true;
                        }
                    }
                    if (!valid) return false;
                }
                return true;
            }

            public void SetActionTimer()
            {
                int nextID = CurrentActionIndex + 1;
                Creature.MoveAction nextAction = CurrentAction;
                bool hasNextAction = false;
                Vector3 diff = Vector3.Zero;
                float diffNorm = 0.0f;
                Vector3 half = Vector3.One * 0.5f;
                if (nextID < Path.Count)
                {
                    hasNextAction = true;
                    nextAction = Path[nextID];
                    diff = (nextAction.Voxel.Position + half - (CurrentAction.Voxel.Position + half));
                    diffNorm = diff.Length();
                }
                float unitTime = 1.25f/(Agent.Stats.Dexterity + 0.001f) + RandomTimeOffset;
                if (hasNextAction)
                {
                    switch (CurrentAction.MoveType)
                    {
                        case Creature.MoveType.Walk:
                            CurrentActionTimer.Reset(unitTime * diffNorm);
                            break;
                        case Creature.MoveType.Climb:
                            CurrentActionTimer.Reset(unitTime * diffNorm * 2.5f);
                            break;
                        case Creature.MoveType.Jump:
                            CurrentActionTimer.Reset(unitTime * diffNorm * 1.5f);
                            break;
                        case Creature.MoveType.Swim:
                            CurrentActionTimer.Reset(unitTime * diffNorm * 2.0f);
                            break;
                        case Creature.MoveType.Fall:
                            CurrentActionTimer.Reset(unitTime * diffNorm);
                            break;
                    }

                }
            }

            public bool NextAction()
            {
                if (Path == null) 
                    return false;

                CurrentActionIndex++;
                if (Path.Count <= CurrentActionIndex) 
                    return false;
                CurrentAction = Path[CurrentActionIndex];
                SetActionTimer();
                return true;
            }


            public bool InitializePath()
            {
                // ERROR CHECKS / INITIALIZING
                Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
                Creature.OverrideCharacterMode = false;
                Agent.Physics.Orientation = Physics.OrientMode.RotateY;
                Agent.Physics.CollideMode = Physics.CollisionMode.None;
                Agent.Physics.Gravity = Vector3.Zero;
                CurrentActionTimer = new Timer(1.0f, false);
                Path = GetPath();
                if (Path == null)
                {
                    SetPath(null);
                    Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    Agent.Physics.CollideMode = CollisionMode;
                    return false;
                }
                else if (Path.Count > 0)
                {
                    RandomPositionOffsets = new List<Vector3>();
                    foreach (Creature.MoveAction action in Path)
                    {
                        RandomPositionOffsets.Add(MathFunctions.RandVector3Box(-0.1f, 0.1f, 0.0f, 0.0f, -0.1f, 0.1f));
                    }
                    CurrentActionIndex = -1;
                    return true;
                }
                else return true;
            }

            public IEnumerable<Status> SnapToCurrent()
            {
                Vector3 half = Vector3.One * 0.5f;
                if (CurrentAction.Voxel == null) 
                    yield break;
                Vector3 target = CurrentAction.Voxel.Position + half + RandomPositionOffsets[CurrentActionIndex];
                Matrix transform = Agent.Physics.LocalTransform;
                do
                {
                    transform.Translation = target*0.15f + transform.Translation*0.85f;
                    Agent.Physics.LocalTransform = transform;
                    yield return Status.Running;
                } while ((target - Agent.Physics.Position).Length() > 0.1f);
            }

            public void PerformCurrentAction()
            {
                float t = CurrentActionTimer.CurrentTimeSeconds/CurrentActionTimer.TargetTimeSeconds;

                if (CurrentActionIndex == 0)
                {
                    t = Easing.SineEaseIn(CurrentActionTimer.CurrentTimeSeconds, 0.0f, 1.0f, CurrentActionTimer.TargetTimeSeconds);
                }
                else if (CurrentActionIndex == Path.Count - 2)
                {
                    t = Easing.SineEaseOut(CurrentActionTimer.CurrentTimeSeconds, 0.0f, 1.0f, CurrentActionTimer.TargetTimeSeconds);
                }
                

                int nextID = CurrentActionIndex + 1;
                bool hasNextAction = false;
                Vector3 half = Vector3.One * 0.5f;
                Vector3 nextPosition = Vector3.Zero;
                Vector3 currPosition = CurrentAction.Voxel.Position + half;
                currPosition += RandomPositionOffsets[CurrentActionIndex];
                if (nextID < Path.Count)
                {
                    hasNextAction = true;
                    nextPosition = Path[nextID].Voxel.Position;
                    nextPosition += RandomPositionOffsets[nextID] + half;
                }
                Matrix transform = Agent.Physics.LocalTransform;
                Vector3 diff = (nextPosition - currPosition);

                switch (CurrentAction.MoveType)
                {
                    case Creature.MoveType.Walk:
                        Creature.OverrideCharacterMode = false;
                        if (hasNextAction)
                        {
                            transform.Translation = diff*t + currPosition;
                            Agent.Physics.Velocity = diff;
                        }
                        else
                        {
                            transform.Translation = currPosition;
                        }
                        break;
                    case Creature.MoveType.Swim:
                        Creature.OverrideCharacterMode = false;
                        if (hasNextAction)
                        {
                            transform.Translation = diff*t + currPosition;
                            Agent.Physics.Velocity = diff;
                        }
                        else
                        {
                            transform.Translation = currPosition;
                        }
                        break;
                    case Creature.MoveType.Jump:
                        Creature.OverrideCharacterMode = false;
                        if (hasNextAction)
                        {
                            float z = Easing.Ballistic(CurrentActionTimer.CurrentTimeSeconds, CurrentActionTimer.TargetTimeSeconds, 1.0f);
                            Vector3 start = currPosition;
                            Vector3 end = nextPosition;
                            Vector3 dx = (end - start) * t + start;
                            dx.Y = start.Y * (1 - t) + end.Y * (t) + z;
                            transform.Translation = dx;
                            Agent.Physics.Velocity = new Vector3(diff.X, (dx.Y - Agent.Physics.Position.Y), diff.Z);
                        }
                        else
                        {
                            transform.Translation = currPosition;
                        }
                        break;
                    case Creature.MoveType.Fall:
                        Creature.OverrideCharacterMode = false;
                        if (hasNextAction)
                        {
                            transform.Translation = diff*t + currPosition;
                            Agent.Physics.Velocity = diff;
                        }
                        else
                        {
                            transform.Translation = currPosition;
                        }
                        break;
                    case Creature.MoveType.Climb:
                        Creature.OverrideCharacterMode = true;
                        Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
                        
                        if (hasNextAction)
                        {
                            transform.Translation = diff*t + currPosition;
                            Agent.Physics.Velocity = diff;
                        }
                        else
                        {
                            transform.Translation = currPosition;
                        }
                        break;
                }
                
                Agent.Physics.LocalTransform = transform;
            }

            public override IEnumerable<Status> Run()
            {
                InitializePath();
                while (NextAction())
                {
                    if (CurrentActionIndex >= Path.Count)
                    {
                        break;
                    }

                    if (CurrentActionIndex == 0)
                    {
                        foreach (Act.Status status in SnapToCurrent())
                        {
                            yield return status;
                        }
                    }

                    while (!CurrentActionTimer.HasTriggered)
                    {
                        if (Agent.DrawPath)
                        {
                            List<Vector3> points =
                                Path.Select((v, i) => v.Voxel.Position + new Vector3(0.5f, 0.5f, 0.5f) + RandomPositionOffsets[i]).ToList();
                            Drawer3D.DrawLineList(points, Color.Red, 0.1f);
                        }
                        CurrentActionTimer.Update(DwarfTime.LastTime);
                        ValidPathTimer.Update(DwarfTime.LastTime);

                        PerformCurrentAction();

                        // Check if the path has been made invalid
                        if (ValidPathTimer.HasTriggered && !IsPathValid(Path))
                        {
                            Creature.OverrideCharacterMode = false;
                            Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                            Agent.Physics.CollideMode = CollisionMode;
                            yield return Status.Fail;
                        }
                        yield return Status.Running;
                    }
                }
                Creature.OverrideCharacterMode = false;
                SetPath(null);
                Agent.Physics.CollideMode = CollisionMode;
                yield return Status.Success;
            }


            public override void OnCanceled()
            {
                Creature.OverrideCharacterMode = false;
                SetPath(null);
                Agent.Physics.CollideMode = CollisionMode;
                base.OnCanceled();
            }
        }


        /// <summary>
        /// A creature moves along a planned path until the path is completed, or
        /// it detects failure.
        /// </summary>
        [Newtonsoft.Json.JsonObject(IsReference = true)]
        public class FollowPathAct : CreatureAct
        {
            private string PathName { get; set; }

            public float EnergyLoss { get; set; }

            public Timer ValidPathTimer { get; set; }

            public FollowPathAct(CreatureAI agent, string pathName) :
                base(agent)
            {
                Name = "Follow path";
                PathName = pathName;
                EnergyLoss = 10.0f;
                ValidPathTimer = new Timer(.75f, false);
            }

            public List<Creature.MoveAction> GetPath()
            {
                return Agent.Blackboard.GetData<List<Creature.MoveAction>>(PathName);
            }

            public void SetPath(List<Creature.MoveAction> path)
            {
                Agent.Blackboard.SetData(PathName, path);
            }

            public bool IsPathValid(List<Creature.MoveAction> path)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    if (!path[i].Voxel.IsEmpty) return false;
                    List<Creature.MoveAction> neighbors = Agent.Movement.GetMoveActions(path[i].Voxel);
                    bool valid = false;
                    foreach (Creature.MoveAction vr in neighbors)
                    {
                        Vector3 dif = vr.Voxel.Position - path[i + 1].Voxel.Position;
                        if (dif.Length() < .1)
                        {
                            valid = true;
                        }
                    }
                    if (!valid) return false;
                }
                return true;
            }

            public override IEnumerable<Status> Run()
            {
                Physics.CollisionMode collisionMode = Agent.Physics.CollideMode;
                Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
                Creature.OverrideCharacterMode = false;
                while (true)
                {
                    // ERROR CHECKS / INITIALIZING
                    Agent.Physics.Orientation = Physics.OrientMode.RotateY;
                    //Agent.Physics.CollideMode = Physics.CollisionMode.UpDown;

                    List<Creature.MoveAction> path = GetPath();

                    if (path == null)
                    {
                        SetPath(null);
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        Agent.Physics.CollideMode = collisionMode;
                        yield return Status.Fail;
                        break;
                    }
                    else if (path.Count > 0)
                    {
                        TargetVoxel = path.ElementAt(0).Voxel;
                    }
                    else
                    {
                        Agent.Physics.CollideMode = collisionMode;
                        yield return Status.Success;
                        break;
                    }

                    Creature.MoveAction currentAction = path.ElementAt(0);

                    // IF WE ARE MOVING
                    if (TargetVoxel != null)
                    {
                        Agent.Physics.IsSleeping = false;
                        Agent.LocalControlTimeout.Update(DwarfTime.LastTime);
                        ValidPathTimer.Update(DwarfTime.LastTime);

                        // Check if the path has been made invalid
                        if (ValidPathTimer.HasTriggered && !IsPathValid(path))
                        {
                            Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                            Agent.Physics.CollideMode = collisionMode;
                            yield return Status.Fail;
                        }

                        if (Agent.LocalControlTimeout.HasTriggered)
                        {
                            Vector3 target = TargetVoxel.Position + new Vector3(.5f, .5f, .5f);
                            float distToTarget = (target - Agent.Position).Length();
                            while (distToTarget > 0.1f)
                            {
                                Agent.Position = 0.9f*Agent.Position + 0.1f*target;
                                distToTarget = (target - Agent.Position).Length();
                                yield return Status.Running;
                            }
                            Agent.LocalControlTimeout.Reset(Agent.LocalControlTimeout.TargetTimeSeconds);
                            Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                            yield return Status.Running;
                        }


                        if (PreviousTargetVoxel == null)
                        {
                            LocalTarget = TargetVoxel.Position + new Vector3(0.5f, 0.5f, 0.5f);
                        }
                        else
                        {
                            LocalTarget = MathFunctions.ClosestPointToLineSegment(Agent.Position,
                                PreviousTargetVoxel.Position,
                                TargetVoxel.Position, 0.25f) + new Vector3(0.5f, 0.5f, 0.5f);
                        }

                        Vector3 desiredVelocity = (LocalTarget - Agent.Position);
                        desiredVelocity.Normalize();
                        desiredVelocity *= Creature.Stats.MaxSpeed;

                        Vector3 output =
                            Agent.Creature.Controller.GetOutput((float) DwarfTime.LastTime.ElapsedGameTime.TotalSeconds,
                                desiredVelocity,
                                Creature.Physics.Velocity);

                        output.Y = 0.0f;
                        Agent.Creature.Physics.ApplyForce(output*0.25f,
                            (float) DwarfTime.LastTime.ElapsedGameTime.TotalSeconds);

                        float yDifference = (LocalTarget - Agent.Position).Y;

                        if (yDifference > 0.25f)
                        {
                            Agent.Jump(DwarfTime.LastTime);
                        }


                        if (Agent.DrawPath)
                        {
                            Drawer3D.DrawLineList(
                                new List<Vector3> {LocalTarget, Agent.Creature.Physics.GlobalTransform.Translation},
                                Color.White, 0.01f);

                            List<Vector3> points =
                                path.Select(v => v.Voxel.Position + new Vector3(0.5f, 0.5f, 0.5f)).ToList();

                            Drawer3D.DrawLineList(points, Color.Red, 0.1f);
                        }

                        bool goToNextNode;
                        if (path.Count > 1)
                        {
                            goToNextNode = (yDifference < 0.05 &&
                                            (LocalTarget - Agent.Creature.Physics.GlobalTransform.Translation).Length() <
                                            0.5f);
                        }
                        else
                        {
                            goToNextNode = (LocalTarget - Agent.Creature.Physics.GlobalTransform.Translation).Length() <
                                           0.25f;
                        }

                        if (goToNextNode)
                        {
                            if (path.Count > 0)
                            {
                                PreviousTargetVoxel = TargetVoxel;
                                Agent.LocalControlTimeout.Reset(Agent.LocalControlTimeout.TargetTimeSeconds);
                                TargetVoxel = path[0].Voxel;
                                path.RemoveAt(0);
                            }
                            else
                            {
                                PreviousTargetVoxel = null;
                                SetPath(null);
                                Agent.Physics.CollideMode = collisionMode;
                                yield return Status.Success;
                                break;
                            }
                        }
                    }
                    else
                    {
                        PreviousTargetVoxel = null;
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        Agent.Physics.CollideMode = collisionMode;
                        yield return Status.Fail;
                        break;
                    }

                    yield return Status.Running;
                }
            }

            public Voxel TargetVoxel { get; set; }
            public Voxel PreviousTargetVoxel { get; set; }
            public Vector3 LocalTarget { get; set; }
        }

    }
}