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
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     A creature moves along a planned path until the path is completed, or
    ///     it detects failure.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class FollowPathAct : CreatureAct
    {
        public FollowPathAct(CreatureAI agent, string pathName) :
            base(agent)
        {
            Name = "Follow path";
            PathName = pathName;
            ValidPathTimer = new Timer(.75f, false);
            RandomTimeOffset = MathFunctions.Rand(0.0f, 0.1f);
            BlendStart = true;
            BlendEnd = true;
        }

        private string PathName { get; set; }
        public Timer ValidPathTimer { get; set; }

        public Timer TrajectoryTimer { get; set; }
        public List<MoveAction> Path { get; set; }
        public float RandomTimeOffset { get; set; }
        public List<Vector3> RandomPositionOffsets { get; set; }
        public List<float> ActionTimes { get; set; }
        public bool BlendStart { get; set; }
        public bool BlendEnd { get; set; }

        public List<MoveAction> GetPath()
        {
            return Agent.Blackboard.GetData<List<MoveAction>>(PathName);
        }

        public void SetPath(List<MoveAction> path)
        {
            Agent.Blackboard.SetData(PathName, path);
        }

        public bool IsPathValid(List<MoveAction> path)
        {
            if (path.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (!path[i].DestinationVoxel.IsEmpty) return false;
                var neighbors = Agent.Movement.GetMoveActions(path[i].DestinationVoxel);
                bool valid = false;
                foreach (MoveAction vr in neighbors)
                {
                    Vector3 dif = vr.DestinationVoxel.Position - path[i + 1].DestinationVoxel.Position;
                    if (dif.Length() < .1)
                    {
                        valid = true;
                    }
                }
                if (!valid) return false;
            }
            return true;
        }

        public float GetActionTime(MoveAction action, int index)
        {
            MoveAction nextAction = action;
            bool hasNextAction = false;
            Vector3 diff = Vector3.Zero;
            float diffNorm = 0.0f;
            Vector3 half = Vector3.One * 0.5f;
            half.Y = Creature.Physics.BoundingBox.Extents().Y * 1.5f;
            int nextID = index + 1;
            if (nextID < Path.Count)
            {
                hasNextAction = true;
                nextAction = Path[nextID];
                if (nextAction.DestinationVoxel != null)
                {
                    diff = (nextAction.DestinationVoxel.Position + half - (action.DestinationVoxel.Position + half));
                    diffNorm = diff.Length();
                }
            }
            float unitTime = (1.25f / (Agent.Stats.Dexterity + 0.001f) + RandomTimeOffset) /
                             Agent.Movement.Speed(action.MoveType);
            ;
            if (hasNextAction)
            {
                return unitTime * diffNorm;
            }
            return 0.0f;
        }


        public bool InitializePath()
        {
            // ERROR CHECKS / INITIALIZING
            Creature.CurrentCharacterMode = CharacterMode.Walking;
            Creature.OverrideCharacterMode = false;
            Agent.Physics.Orientation = Physics.OrientMode.RotateY;
            ActionTimes = new List<float>();
            Path = GetPath();
            if (Path == null)
            {
                SetPath(null);
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                return false;
            }
            if (Path.Count > 0)
            {
                RandomPositionOffsets = new List<Vector3>();
                int i = 0;
                float dt = 0;
                float time = 0;
                foreach (MoveAction action in Path)
                {
                    RandomPositionOffsets.Add(MathFunctions.RandVector3Box(-0.1f, 0.1f, 0.0f, 0.0f, -0.1f, 0.1f));
                    dt = GetActionTime(action, i);
                    ActionTimes.Add(dt);
                    time += dt;
                    i++;
                }
                RandomPositionOffsets[0] = Agent.Position - (Path[0].DestinationVoxel.Position + Vector3.One * 0.5f);
                TrajectoryTimer = new Timer(time, true);
                return true;
            }
            return true;
        }

        public IEnumerable<Status> SnapToFirst()
        {
            Vector3 half = Vector3.One * 0.5f;
            half.Y = Creature.Physics.BoundingBox.Extents().Y * 2.0f;

            if (Path[0].DestinationVoxel == null)
                yield break;
            Vector3 target = Path[0].DestinationVoxel.Position + half + RandomPositionOffsets[0];
            Matrix transform = Agent.Physics.LocalTransform;
            do
            {
                transform.Translation = target * 0.15f + transform.Translation * 0.85f;
                Agent.Physics.LocalTransform = transform;
                yield return Status.Running;
            } while ((target - Agent.Physics.Position).Length() > 0.1f);
        }

        public bool GetCurrentAction(ref MoveAction action, ref float time, ref int index)
        {
            float currentTime = 0;

            if (BlendStart && BlendEnd)
            {
                currentTime = Easing.LinearQuadBlends(TrajectoryTimer.CurrentTimeSeconds,
                    TrajectoryTimer.TargetTimeSeconds, 0.5f);
            }
            else if (BlendStart)
            {
                currentTime = Easing.CubicEaseIn(TrajectoryTimer.CurrentTimeSeconds, 0, 
                    TrajectoryTimer.TargetTimeSeconds, 
                    TrajectoryTimer.TargetTimeSeconds);
            }
            else if (BlendEnd)
            {
                currentTime = Easing.Linear(TrajectoryTimer.CurrentTimeSeconds, 0, TrajectoryTimer.TargetTimeSeconds,
                    TrajectoryTimer.TargetTimeSeconds);
            }
            float sumTime = 0.001f;
            for (int i = 0; i < ActionTimes.Count; i++)
            {
                sumTime += ActionTimes[i];
                if (currentTime < sumTime)
                {
                    action = Path[i];
                    time = 1.0f - (sumTime - currentTime) / ActionTimes[i];
                    index = i;
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<Status> PerformCurrentAction()
        {
            MoveAction action = Path.First();
            float t = 0;
            int currentIndex = 0;
            if (!GetCurrentAction(ref action, ref t, ref currentIndex))
            {
                yield break;
            }

            int nextID = currentIndex + 1;
            bool hasNextAction = false;
            Vector3 half = Vector3.One * 0.5f;
            half.Y = Creature.Physics.BoundingBox.Extents().Y * 2;
            Vector3 nextPosition = Vector3.Zero;
            Vector3 currPosition = action.DestinationVoxel.Position + half;

            currPosition += RandomPositionOffsets[currentIndex];
            if (nextID < Path.Count)
            {
                hasNextAction = true;
                nextPosition = Path[nextID].DestinationVoxel.Position;
                nextPosition += RandomPositionOffsets[nextID] + half;
            }

            Matrix transform = Agent.Physics.LocalTransform;
            Vector3 diff = (nextPosition - currPosition);

            switch (action.MoveType)
            {
                case MoveType.Walk:
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = CharacterMode.Walking;
                    if (hasNextAction)
                    {
                        transform.Translation = diff * t + currPosition;
                        Agent.Physics.Velocity = diff;
                    }
                    else
                    {
                        transform.Translation = currPosition;
                    }
                    break;
                case MoveType.Swim:
                    Creature.NoiseMaker.MakeNoise("Swim", Agent.Position, true);
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = CharacterMode.Swimming;
                    if (hasNextAction)
                    {
                        transform.Translation = diff * t + currPosition + new Vector3(0, 0.5f, 0);
                        Agent.Physics.Velocity = diff;
                    }
                    else
                    {
                        transform.Translation = currPosition;
                    }
                    break;
                case MoveType.Jump:
                    if (t < 0.5f)
                    { 
                        Creature.NoiseMaker.MakeNoise("Jump", Agent.Position, false);
                    }
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = Creature.Physics.Velocity.Y > 0
                        ? CharacterMode.Jumping
                        : CharacterMode.Falling;
                    if (hasNextAction)
                    {
                        float z = Easing.Ballistic(t, 1.0f, 1.0f);
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
                case MoveType.Fall:
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = CharacterMode.Falling;
                    if (hasNextAction)
                    {
                        transform.Translation = diff * t + currPosition;
                        Agent.Physics.Velocity = diff;
                    }
                    else
                    {
                        transform.Translation = currPosition;
                    }
                    break;
                case MoveType.Climb:
                case MoveType.ClimbWalls:
                    if ((int) (t*100)%25 == 0)
                    {
                        Creature.NoiseMaker.MakeNoise("Climb", Agent.Position, false);
                    }
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = CharacterMode.Climbing;
                    Creature.OverrideCharacterMode = true;
                    if (hasNextAction)
                    {
                        transform.Translation = diff * t + currPosition;
                        Agent.Physics.Velocity = diff;

                        if (action.ActionVoxel != null)
                        {
                            Agent.Physics.Velocity = (action.DestinationVoxel.Position + Vector3.One*0.5f) - currPosition;
                        }
                        else if (action.InteractObject != null)
                        {
                            Agent.Physics.Velocity = (action.InteractObject.GetComponent<Body>().Position) - currPosition;
                        }
                    }
                    else
                    {
                        transform.Translation = currPosition;
                    }
                    break;
                case MoveType.Fly:
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = CharacterMode.Flying;
                    Creature.OverrideCharacterMode = true;
                    if (hasNextAction)
                    {
                        transform.Translation = diff * t + currPosition;
                        Agent.Physics.Velocity = diff;
                    }
                    else
                    {
                        transform.Translation = currPosition;
                    }
                    break;
                case MoveType.DestroyObject:
                    Creature.AI.Tasks.Add(new KillEntityTask((Body)(action.InteractObject), 
                        KillEntityTask.KillType.Auto) {Priority = Task.PriorityType.Urgent});
                    yield return Act.Status.Fail;
                    yield break;
            }

            Agent.Physics.LocalTransform = transform;
        }

        public override IEnumerable<Status> Run()
        {
            if (Name == "FollowtoFoo")
            {
                Console.Out.WriteLine("There it is!");
            }
            InitializePath();
            if (Path.Count == 0)
                yield return Act.Status.Success;
            if (TrajectoryTimer == null) yield break;
            while (!TrajectoryTimer.HasTriggered)
            {
                TrajectoryTimer.Update(DwarfTime.LastTime);
                ValidPathTimer.Update(DwarfTime.LastTime);
                foreach (Status status in PerformCurrentAction())
                {
                    if (status == Status.Fail)
                    {
                        yield return Status.Fail;
                    }
                    else if (status == Status.Success)
                    {
                        break;
                    }
                    yield return Status.Running;
                }

                if (Agent.DrawPath)
                {
                    List<Vector3> points =
                        Path.Select(
                            (v, i) => v.DestinationVoxel.Position + new Vector3(0.5f, 0.5f, 0.5f) + RandomPositionOffsets[i])
                            .ToList();
                    List<Color> colors =
                            Path.Select((v, i) =>
                            {
                                switch (v.MoveType)
                                {
                                    case MoveType.Climb:
                                        return Color.Cyan;
                                    case MoveType.ClimbWalls:
                                        return Color.DarkCyan;
                                    case MoveType.DestroyObject:
                                        return Color.Orange;
                                    case MoveType.Fall:
                                        return Color.LightBlue;
                                    case MoveType.Fly:
                                        return Color.Green;
                                    case MoveType.Jump:
                                        return Color.Yellow;
                                    case MoveType.Swim:
                                        return Color.Blue;
                                    case MoveType.Walk:
                                        return Color.Red;
                                }
                                return Color.White;
                            })
                            .ToList();
                    Drawer3D.DrawLineList(points, colors, 0.1f);
                }


                // Check if the path has been made invalid
                if (ValidPathTimer.HasTriggered && !IsPathValid(Path))
                {
                    Creature.OverrideCharacterMode = false;
                    Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    yield return Status.Fail;
                }
                yield return Status.Running;
            }
            Creature.OverrideCharacterMode = false;
            SetPath(null);
            yield return Status.Success;
        }


        public override void OnCanceled()
        {
            Creature.OverrideCharacterMode = false;
            SetPath(null);
            base.OnCanceled();
        }
    }


}