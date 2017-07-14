// MeleeAct.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature attacks a target until the target is dead.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class MeleeAct : CreatureAct
    {
        public float EnergyLoss { get; set; }
        public Attack CurrentAttack { get; set; }
        public Body Target { get; set; }
        public bool Training { get; set; }
        public Timer Timeout { get; set; }
        public string TargetName { get; set; }
        public Timer FailTimer { get; set; }
        public MeleeAct(CreatureAI agent, string target) :
            base(agent)
        {
            FailTimer = new Timer(5.0f, false);
            Timeout = new Timer(100.0f, false);
            Training = false;
            Name = "Attack!";
            EnergyLoss = 200.0f;
            TargetName = target;
            CurrentAttack = Datastructures.SelectRandom(agent.Creature.Attacks);
        }

        public MeleeAct(CreatureAI agent, Body target) :
            base(agent)
        {
            FailTimer = new Timer(5.0f, false);
            Timeout = new Timer(100.0f, false);
            Training = false;
            Name = "Attack!";
            EnergyLoss = 200.0f;
            Target = target;
            CurrentAttack = Datastructures.SelectRandom(agent.Creature.Attacks);
        }

        public override void OnCanceled()
        {
            Creature.Physics.Orientation = Physics.OrientMode.RotateY;
            Creature.OverrideCharacterMode = false;
            Creature.CurrentCharacterMode = CharacterMode.Walking;
            base.OnCanceled();
        }

        public IEnumerable<Status> AvoidTarget(float range, float time)
        {
            if (Target == null)
            {
                yield return Status.Fail;
                yield break;
            }
            Timer avoidTimer = new Timer(time, true, Timer.TimerMode.Game);
            while (true)
            {
                avoidTimer.Update(DwarfTime.LastTime);

                if (avoidTimer.HasTriggered)
                {
                    yield return Status.Success;
                }

                float dist = (Target.Position - Agent.Position).Length();

                if (dist > range)
                {
                    yield return Status.Success;
                    yield break;
                }

                List<MoveAction> neighbors = Agent.Movement.GetMoveActions(Agent.Position).ToList();
                neighbors.Sort((a, b) =>
                {
                    if (a.Equals(b)) return 0;

                    float da = (a.DestinationVoxel.WorldPosition - Target.Position).LengthSquared();
                    float db = (b.DestinationVoxel.WorldPosition - Target.Position).LengthSquared();

                    return da.CompareTo(db);
                });

                neighbors.RemoveAll(
                    a => a.MoveType == MoveType.Jump || a.MoveType == MoveType.Climb);

                if (neighbors.Count == 0)
                {
                    yield return Status.Fail;
                    yield break;
                }

                MoveAction furthest = neighbors.Last();
                bool reachedTarget = false;
                Timer timeout = new Timer(2.0f, true);
                while (!reachedTarget)
                {
                    Vector3 output = Creature.Controller.GetOutput(DwarfTime.Dt, furthest.DestinationVoxel.WorldPosition + Vector3.One*0.5f,
                        Agent.Position);
                    Creature.Physics.ApplyForce(output, DwarfTime.Dt);

                    if (Creature.AI.Movement.CanFly)
                    {
                        Creature.Physics.ApplyForce(Vector3.Up * 10, DwarfTime.Dt);
                    }

                    timeout.Update(DwarfTime.LastTime);

                    yield return Status.Running;
                    if (timeout.HasTriggered || (furthest.DestinationVoxel.WorldPosition - Agent.Position).Length() < 1)
                    {
                        reachedTarget = true;
                    }
                    Agent.Creature.CurrentCharacterMode = CharacterMode.Walking;
                }
            yield return Status.Success;
                yield break;
            }
        }

        public override IEnumerable<Status> Run()
        {
            if (CurrentAttack == null)
            {
                yield return Status.Fail;
                yield break;
            }

            Timeout.Reset();
            FailTimer.Reset();
            if (Target == null && TargetName != null)
            {
                Target = Agent.Blackboard.GetData<Body>(TargetName);

                if (Target == null)
                {
                    yield return Status.Fail;
                    yield break;
                }
            }

            Inventory targetInventory = Target.GetComponent<Inventory>();

            if (targetInventory != null)
            {
                targetInventory.OnDeath += targetInventory_OnDeath;
            }

            CharacterMode defaultCharachterMode = Creature.AI.Movement.CanFly
                ? CharacterMode.Flying
                : CharacterMode.Walking;

            bool avoided = false;
            while(true)
            {
                Timeout.Update(DwarfTime.LastTime);
                FailTimer.Update(DwarfTime.LastTime);
                if (FailTimer.HasTriggered)
                {
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = defaultCharachterMode;
                    yield return Status.Fail;
                    yield break;
                }

                if (Timeout.HasTriggered)
                {
                    if (Training)
                    {
                        Agent.AddXP(1);
                        Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                        Creature.OverrideCharacterMode = false;
                        Creature.CurrentCharacterMode = defaultCharachterMode;
                        yield return Status.Success;
                        yield break;
                    }
                    else
                    {
                        Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                        Creature.OverrideCharacterMode = false;
                        Creature.CurrentCharacterMode = defaultCharachterMode;
                        yield return Status.Fail;
                        yield break;
                    }
                }

                if (Target == null || Target.IsDead)
                {
                    Creature.CurrentCharacterMode = defaultCharachterMode;
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                    yield return Status.Success;
                }

                // Find the location of the melee target
                Vector3 targetPos = new Vector3(Target.GlobalTransform.Translation.X,
                    Target.GetBoundingBox().Min.Y,
                    Target.GlobalTransform.Translation.Z);

                Vector2 diff = new Vector2(targetPos.X, targetPos.Z) - new Vector2(Creature.AI.Position.X, Creature.AI.Position.Z);

                Creature.Physics.Face(targetPos);

                bool intersectsbounds = Creature.Physics.BoundingBox.Intersects(Target.BoundingBox);

                // If we are really far from the target, something must have gone wrong.
                if (!intersectsbounds && diff.Length() > CurrentAttack.Range * 8)
                {
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = defaultCharachterMode;
                    yield return Status.Fail;
                }
                // If we're out of attack range, run toward the target.
                if(!Creature.AI.Movement.IsSessile && !intersectsbounds && diff.Length() > CurrentAttack.Range)
                {
                    Creature.CurrentCharacterMode = defaultCharachterMode;
                    /*
                    Vector3 output = Creature.Controller.GetOutput(DwarfTime.Dt, targetPos, Creature.Physics.GlobalTransform.Translation) * 0.9f;
                    output.Y = 0.0f;
                    if (Creature.AI.Movement.CanFly)
                    {
                        Creature.Physics.ApplyForce(-Creature.Physics.Gravity, DwarfTime.Dt);
                    }
                    if (Creature.AI.Movement.IsSessile)
                    {
                        output *= 0.0f;
                    }
                    Creature.Physics.ApplyForce(output, DwarfTime.Dt);
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                     */
                    GreedyPathAct greedyPath = new GreedyPathAct(Creature.AI, Target, CurrentAttack.Range * 0.75f) {PathLength = 5};
                    greedyPath.Initialize();

                    foreach (Act.Status stat in greedyPath.Run())
                    {
                        if (stat == Act.Status.Running)
                        {
                            yield return Status.Running;
                        }
                        else break;
                    }
                }
                // If we have a ranged weapon, try avoiding the target for a few seconds to get within range.
                else if (!Creature.AI.Movement.IsSessile && !intersectsbounds && !avoided && (CurrentAttack.Mode == Attack.AttackMode.Ranged &&
                    diff.Length() < CurrentAttack.Range*0.15f))
                {
                    FailTimer.Reset();
                    foreach (Act.Status stat in AvoidTarget(CurrentAttack.Range, 3.0f))
                    {
                        yield return Status.Running;
                    }
                    avoided = true;
                }
                // Else, stop and attack
                else
                {
                    if (CurrentAttack.Mode == Attack.AttackMode.Ranged 
                        && VoxelHelpers.DoesRayHitSolidVoxel(Creature.World.ChunkManager.ChunkData,
                            Creature.AI.Position, Target.Position))
                    {
                        yield return Status.Fail;
                        yield break;
                    }

                    FailTimer.Reset();
                    avoided = false;
                    Creature.Physics.Orientation = Physics.OrientMode.Fixed;
                    Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.9f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.9f);
                    CurrentAttack.RechargeTimer.Reset(CurrentAttack.RechargeRate);

                    Creature.Sprite.ResetAnimations(CharacterMode.Attacking);
                    Creature.Sprite.PlayAnimations(CharacterMode.Attacking);
                    Creature.CurrentCharacterMode = CharacterMode.Attacking;
                    Creature.OverrideCharacterMode = true;
                    while (!CurrentAttack.Perform(Creature, Target, DwarfTime.LastTime, Creature.Stats.BuffedStr + Creature.Stats.BuffedSiz,
                            Creature.AI.Position, Creature.Faction.Name))
                    {
                        Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.9f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.9f);
                        if (Creature.AI.Movement.CanFly)
                        {
                            Creature.Physics.ApplyForce(-Creature.Physics.Gravity * 0.1f, DwarfTime.Dt);
                        }
                        yield return Status.Running;
                    }

                    while (!Agent.Creature.Sprite.CurrentAnimation.IsDone())
                    {
                        if (Creature.AI.Movement.CanFly)
                        {
                            Creature.Physics.ApplyForce(-Creature.Physics.Gravity, DwarfTime.Dt);
                        }
                        yield return Status.Running;
                    }
                    Creature.CurrentCharacterMode = CharacterMode.Attacking;
                    Creature.Sprite.ReloopAnimations(CharacterMode.Attacking);

                    CurrentAttack.RechargeTimer.Reset(CurrentAttack.RechargeRate);

                    Vector3 dogfightTarget = Vector3.Zero;
                    while (!CurrentAttack.RechargeTimer.HasTriggered && !Target.IsDead)
                    {
                        CurrentAttack.RechargeTimer.Update(DwarfTime.LastTime);
                        if (CurrentAttack.Mode == Attack.AttackMode.Dogfight)
                        {
                            Creature.CurrentCharacterMode = CharacterMode.Attacking;
                            dogfightTarget += MathFunctions.RandVector3Cube()*0.1f;
                            Vector3 output = Creature.Controller.GetOutput(DwarfTime.Dt, dogfightTarget + Target.Position, Creature.Physics.GlobalTransform.Translation) * 0.9f;
                            Creature.Physics.ApplyForce(output - Creature.Physics.Gravity, DwarfTime.Dt);
                        }
                        else
                        {
                            Creature.Sprite.PauseAnimations(CharacterMode.Attacking);
                            Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.9f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.9f);
                            if (Creature.AI.Movement.CanFly)
                            {
                                Creature.Physics.ApplyForce(-Creature.Physics.Gravity, DwarfTime.Dt);
                            }
                        }
                        yield return Status.Running;
                    }

                    Creature.CurrentCharacterMode = defaultCharachterMode;
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                    if (Target.IsDead)
                    {
                        if (Creature.Faction.ChopDesignations.Contains(Target))
                        {
                            Creature.Faction.ChopDesignations.Remove(Target);
                        }

                        if (Creature.Faction.AttackDesignations.Contains(Target))
                        {
                            Creature.Faction.AttackDesignations.Remove(Target);
                        }

                        //Creature.World.GoalManager.OnGameEvent(new Goals.Events.CreatyreKilled
                        //{
                        //    Agressor = Creature.AI,
                        //    Victim = Target.
                        //})

                        Target = null;
                        Agent.AddXP(10);
                        Creature.Physics.Face(Creature.Physics.Velocity + Creature.Physics.GlobalTransform.Translation);
                        Creature.Stats.NumThingsKilled++;
                        Creature.AI.AddThought(Thought.ThoughtType.KilledThing);
                        Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                        Creature.OverrideCharacterMode = false;
                        Creature.CurrentCharacterMode = defaultCharachterMode;
                        yield return Status.Success;
                        break;
                    }
                
                }

                yield return Status.Running;
            }
        }

        void targetInventory_OnDeath(List<Body> items)
        {
            if (items == null) return;

            foreach (Body item in items)
            {
                Agent.Creature.Gather(item);
            }
        }
    }

    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GreedyPathAct : CompoundCreatureAct
    {
        public int PathLength { get; set; }
        public bool Is2D { get; set; }
        public Body Target { get; set; }
        public float Threshold { get; set; }

        public GreedyPathAct()
        {

        }

        public GreedyPathAct(CreatureAI creature, Body target, float threshold)
            : base(creature)
        {
            Target = target;
            Threshold = threshold;
        }

        public IEnumerable<Status> FindGreedyPath()
        {
            Vector3 target = Target.Position;

            if (Is2D) target.Y = Creature.AI.Position.Y;
            List<MoveAction> path = new List<MoveAction>();
            VoxelHandle curr = Creature.Physics.CurrentVoxel;
            for (int i = 0; i < PathLength; i++)
            {
                IEnumerable<MoveAction> actions =
                    Creature.AI.Movement.GetMoveActions(curr);

                MoveAction? bestAction = null;
                float bestDist = float.MaxValue;

                foreach (MoveAction action in actions)
                {
                    float dist = (action.DestinationVoxel.WorldPosition - target).LengthSquared();

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestAction = action;
                    }
                }

                Vector3 half = Vector3.One*0.5f;
                if (bestAction.HasValue &&
                    !path.Any(p => p.DestinationVoxel.Equals(bestAction.Value.DestinationVoxel) && p.MoveType == bestAction.Value.MoveType))
                {
                    path.Add(bestAction.Value);
                    MoveAction action = bestAction.Value;
                    action.DestinationVoxel = new VoxelHandle(curr);
                    curr = bestAction.Value.DestinationVoxel;
                    bestAction = action;

                    if (((bestAction.Value.DestinationVoxel.WorldPosition + half) - target).Length() < Threshold)
                    {
                        break;
                    }
                }

            }
            if (path.Count > 0)
            {
                path.Insert(0,
                    new MoveAction()
                    {
                        Diff = Vector3.Zero,
                        DestinationVoxel = new VoxelHandle(Creature.Physics.CurrentVoxel),
                        MoveType = MoveType.Walk
                    });
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
            Tree = new Sequence(new Wrap(FindGreedyPath), new FollowPathAct(Creature.AI, "RandomPath"));
            base.Initialize();
        }
    }
}