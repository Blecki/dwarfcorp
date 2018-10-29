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
            ValidPathTimer = new Timer(.75f + MathFunctions.Rand(), false, Timer.TimerMode.Real);
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

        private MoveActionTempStorage __storage = new MoveActionTempStorage();

        // Offset from voxel location to bounding box center.
        public Vector3 GetBoundingBoxOffset()
        {
            Vector3 half = Vector3.One * 0.5f;
            half.Y = Creature.Physics.BoundingBox.Extents().Y * 1.5f;
            return half;
        }

        public List<MoveAction> GetPath()
        {
            return Agent.Blackboard.GetData<List<MoveAction>>(PathName);
        }

        public void SetPath(List<MoveAction> path)
        {
            Agent.Blackboard.SetData(PathName, path);
        }

        public bool IsPathValid(List<MoveAction> path, int idx)
        {
            if (path.Count == 0)
            {
                return false;
            }
            var bodies = Agent.World.PlayerFaction.OwnedObjects.Where(o => o.Tags.Contains("Teleporter")).ToList();
            for (int i = idx; i < path.Count - 1; i++)
            {
                if (!path[i].SourceVoxel.IsValid)
                {
                    continue;
                }
                var neighbors = Agent.Movement.GetMoveActions(path[i].SourceState, Agent.World.OctTree, bodies, __storage);
                if (!neighbors.Any(n => n.DestinationState == path[i + 1].SourceState))
                {
                    return false;
                }
            }
            return true;
        }

        public float GetActionTime(MoveAction action, int index)
        {
            if (action.MoveType == MoveType.EnterVehicle)
                return 0.5f;
            if (action.MoveType == MoveType.ExitVehicle)
                return 0.1f;
            MoveAction nextAction = action;
            bool hasNextAction = false;
            Vector3 diff = Vector3.Zero;
            float diffNorm = 0.0f;
            Vector3 half = GetBoundingBoxOffset();
            int nextID = index + 1;
            if (nextID < Path.Count)
            {
                hasNextAction = true;
                nextAction = Path[nextID];
                if (nextAction.SourceVoxel.IsValid)
                {
                    diff = (nextAction.SourceVoxel.WorldPosition + half - (action.SourceVoxel.WorldPosition + half)) + Vector3.One * 1e-5f;
                    diffNorm = diff.Length();
                }
                else
                {
                    throw new InvalidOperationException("Something is bad.");
                }
            }
            else
            {
                diff = (action.DestinationVoxel.WorldPosition + half - (action.SourceVoxel.WorldPosition + half)) + Vector3.One * 1e-5f;
                diffNorm = diff.Length();
                hasNextAction = true;
            }
            var speed = Agent.Movement.Speed(action.MoveType);

            float unitTime = (1.25f / (Agent.Stats.BuffedDex + 0.001f) + RandomTimeOffset) /
                             speed;
            
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
                    dt = Math.Max(GetActionTime(action, i), 1e-3f);
                    ActionTimes.Add(dt);
                    time += dt;
                    i++;
                }
                Vector3 half = GetBoundingBoxOffset();
                RandomPositionOffsets[0] = Agent.Position - (Path[0].SourceVoxel.WorldPosition + half);
                TrajectoryTimer = new Timer(time, true);
                return true;
            }
            return true;
        }

        public IEnumerable<Status> SnapToFirst()
        {
            Vector3 half = GetBoundingBoxOffset();

            if (!Path[0].SourceVoxel.IsValid)
                yield break;
            Vector3 target = Path[0].SourceVoxel.WorldPosition + half + RandomPositionOffsets[0];
            Matrix transform = Agent.Physics.LocalTransform;
            if ((target - Agent.Physics.Position).Length() > 4.0f)
            {
                Agent.SetMessage("Failed to follow path. First voxel too far away.");
                yield return Act.Status.Fail;
                yield break;
            }
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

        private MoveType lastMovement = MoveType.Walk;
        public IEnumerable<Status> PerformCurrentAction()
        { 
            MoveAction action = Path.First();
            float t = 0;
            int currentIndex = 0;
            if (!GetCurrentAction(ref action, ref t, ref currentIndex))
            {
                CleanupMinecart();
                yield break;
            }
            //Trace.Assert(t >= 0);
            Trace.Assert(action.SourceVoxel.IsValid);
            int nextID = currentIndex + 1;
            bool hasNextAction = false;
            Vector3 half = GetBoundingBoxOffset();
            Vector3 nextPosition = Vector3.Zero;
            Vector3 currPosition = action.SourceVoxel.WorldPosition + half;

            currPosition += RandomPositionOffsets[currentIndex];
            if (nextID < Path.Count)
            {
                hasNextAction = true;
                nextPosition = Path[nextID].SourceVoxel.WorldPosition;
                nextPosition += RandomPositionOffsets[nextID] + half;
            }
            else
            {
                hasNextAction = true;
                nextPosition = action.DestinationVoxel.WorldPosition + half;
            }

            Matrix transform = Agent.Physics.LocalTransform;
            Vector3 diff = (nextPosition - currPosition);
            Agent.GetRoot().SetFlag(GameComponent.Flag.Visible, true);
            switch (action.MoveType)
            {
                case MoveType.EnterVehicle:
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
                        Vector3 end = nextPosition + Vector3.Up * 0.5f;
                        Vector3 dx = (end - start) * t + start;
                        dx.Y = start.Y * (1 - t) + end.Y * (t) + z;
                        transform.Translation = dx;
                        Agent.Physics.Velocity = new Vector3(diff.X, (dx.Y - Agent.Physics.Position.Y), diff.Z);
                    }
                    else
                    {
                        transform.Translation = currPosition;
                    }
                    if (t > 0.9f)
                        SetupMinecart();

                    break;
                case MoveType.ExitVehicle:
                    CleanupMinecart();
                    transform.Translation = currPosition;
                    break;
                case MoveType.RideVehicle:
                    SetupMinecart();
                    Creature.CurrentCharacterMode = CharacterMode.Minecart;
                    var rail = action.SourceState.VehicleState.Rail;
                    if (rail == null)
                    {
                        if (hasNextAction)
                        {
                            transform.Translation = diff * t + currPosition;
                            Agent.Physics.Velocity = diff;
                        }
                        else
                        {
                            transform.Translation = currPosition;
                        }
                    }
                    else
                    {
                        //Drawer3D.DrawBox(rail.GetContainingVoxel().GetBoundingBox(), Color.Green, 0.1f, true);
                        var pos = rail.InterpolateSpline(t, action.SourceVoxel.WorldPosition + Vector3.One * 0.5f, action.DestinationVoxel.WorldPosition + Vector3.One * 0.5f);
                        transform.Translation = pos + Vector3.Up * 0.5f;
                        Agent.Physics.Velocity = diff;
                    }
                    break;
                case MoveType.Walk:
                    CleanupMinecart();
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
                    CleanupMinecart();
                    Creature.NoiseMaker.MakeNoise("Swim", Agent.Position, true);
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = CharacterMode.Swimming;
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
                case MoveType.Jump:
                    CleanupMinecart();
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
                    CleanupMinecart();
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
                    CleanupMinecart();
                    if (((int)((t + 1) * 100)) % 50 == 0)
                    {
                        Creature.NoiseMaker.MakeNoise("Climb", Agent.Position, false);
                    }
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = CharacterMode.Climbing;
                    Creature.OverrideCharacterMode = true;
                    if (hasNextAction)
                    {

                        if (action.MoveType == MoveType.ClimbWalls && action.ActionVoxel.IsValid)
                        {
                            Agent.Physics.Velocity = (action.DestinationVoxel.WorldPosition + Vector3.One * 0.5f) - currPosition;
                            transform.Translation = diff * t + currPosition;
                        }
                        else if (action.MoveType == MoveType.Climb && action.InteractObject != null)
                        {
                            var ladderPosition = action.InteractObject.GetRoot().GetComponent<Body>().Position;
                            transform.Translation = diff * t + currPosition;
                            Agent.Physics.Velocity = ladderPosition - currPosition;
                        }
                    }
                    else
                    {
                        transform.Translation = currPosition;
                    }
                    break;
                case MoveType.Fly:
                    CleanupMinecart();
                    if (((int)((t + 1) * 100)) % 2 == 0)
                    {
                        Creature.NoiseMaker.MakeNoise("Flap", Agent.Position, false);
                    }
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
                case MoveType.Dig:
                    CleanupMinecart();
                    var destroy = new DigAct(Creature.AI, new KillVoxelTask(action.DestinationVoxel)) { CheckOwnership = false } ;
                    destroy.Initialize();
                    foreach (var status in destroy.Run())
                    {
                        if (status == Act.Status.Fail)
                            yield return Act.Status.Fail;
                        yield return Act.Status.Running;
                    }
                    yield return Act.Status.Fail;
                    yield break;
                case MoveType.DestroyObject:
                    CleanupMinecart();
                    var melee = new MeleeAct(Creature.AI, (Body) action.InteractObject);
                    melee.Initialize();
                    foreach (var status in melee.Run())
                    {
                        if (status == Act.Status.Fail)
                            yield return Act.Status.Fail;
                        yield return Act.Status.Running;
                    }
                    yield return Act.Status.Success;
                    yield break;
                case MoveType.Teleport:
                    if (lastMovement != MoveType.Teleport)
                    {
                        if (action.InteractObject != null)
                        {
                            var teleporter = action.InteractObject.GetComponent<MagicalObject>();
                            if (teleporter != null)
                                teleporter.CurrentCharges--;
                        }
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_ic_dwarf_magic_research, currPosition, true, 1.0f);
                    }
                    Agent.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, false);
                    Agent.World.ParticleManager.Trigger("star_particle", diff * t + currPosition, Color.White, 1);
                    if (action.InteractObject != null)
                    {
                        Agent.World.ParticleManager.Trigger("green_flame", (action.InteractObject as Body).Position, Color.White, 1);
                    }
                    transform.Translation = action.DestinationVoxel.WorldPosition + Vector3.One * 0.5f;
                    break;
            }

            Agent.Physics.LocalTransform = transform;
            lastMovement = action.MoveType;
        }

        private void SetupMinecart()
        {
            var layers = Agent.GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>();
            if (layers != null)
            {
                if (layers.GetLayers().GetLayer("minecart") == null)
                    layers.AddLayer(LayeredSprites.LayerLibrary.EnumerateLayers("minecart").FirstOrDefault(), LayeredSprites.LayerLibrary.BaseDwarfPalette);
            }
        }

        private void CleanupMinecart()
        {
            var layers = Agent.GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>();
            if (layers != null && layers.GetLayers().GetLayer("minecart") != null)
                layers.RemoveLayer("minecart");
        }

        public override IEnumerable<Status> Run()
        {
            InitializePath();
            if (Path == null || Path.Count == 0)
                yield return Act.Status.Success;
            if (TrajectoryTimer == null) yield break;
            while (!TrajectoryTimer.HasTriggered)
            {
                Agent.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, true);
                TrajectoryTimer.Update(DwarfTime.LastTime);
                ValidPathTimer.Update(DwarfTime.LastTime);
                foreach (Status status in PerformCurrentAction())
                {
                    if (status == Status.Fail)
                    {
                        CleanupMinecart();
                        yield return Status.Fail;
                    }
                    else if (status == Status.Success)
                    {
                        break;
                    }
                    Creature.Physics.AnimationQueue.Clear();
                    yield return Status.Running;
                }

                if (Debugger.Switches.DrawPaths)
                {
                    List<Vector3> points =
                        Path.Select(
                            (v, i) => v.SourceVoxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f) + RandomPositionOffsets[i])
                            .ToList();
                    points.Add(Path[Path.Count - 1].DestinationVoxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f));

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
                    colors.Add(Color.White);
                    Drawer3D.DrawLineList(points, colors, 0.1f);
                }

                float t = 0;
                int currentIndex = 0;
                MoveAction action = new MoveAction();
                if (GetCurrentAction(ref action, ref t, ref currentIndex))
                { 
                    // Check if the path has been made invalid
                    if (ValidPathTimer.HasTriggered && !IsPathValid(Path, currentIndex))
                    {
                        Creature.OverrideCharacterMode = false;
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        CleanupMinecart();
                        Agent.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, true);
                        yield return Status.Fail;
                    }
                 }
                Creature.Physics.AnimationQueue.Clear();
                yield return Status.Running;
            }
            Creature.OverrideCharacterMode = false;
            SetPath(null);
            Agent.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, true);
            CleanupMinecart();
            yield return Status.Success;
        }

        public override void OnCanceled()
        {
            Creature.OverrideCharacterMode = false;
            CleanupMinecart();
            SetPath(null);
            Agent.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, true);

            base.OnCanceled();
        }
    }


}