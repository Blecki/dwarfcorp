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

        public MeleeAct(CreatureAI agent, string target) :
            base(agent)
        {
            Timeout = new Timer(100.0f, false);
            Training = false;
            Name = "Attack!";
            EnergyLoss = 200.0f;
            TargetName = target;
            foreach (Attack attack in agent.Creature.Attacks)
            {
                CurrentAttack = attack;
                break;
            }
        }

        public MeleeAct(CreatureAI agent, Body target) :
            base(agent)
        {
            Timeout = new Timer(100.0f, false);
            Training = false;
            Name = "Attack!";
            EnergyLoss = 200.0f;
            Target = target;
            foreach (Attack attack in agent.Creature.Attacks)
            {
                CurrentAttack = attack;
                break;
            }
        }

        public override void OnCanceled()
        {
            Creature.Physics.Orientation = Physics.OrientMode.RotateY;
            Creature.OverrideCharacterMode = false;
            Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
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

                List<Creature.MoveAction> neighbors = Agent.Movement.GetMoveActions(Agent.Position);
                neighbors.Sort((a, b) =>
                {
                    if (a.Equals(b)) return 0;

                    float da = (a.Voxel.Position - Target.Position).LengthSquared();
                    float db = (b.Voxel.Position - Target.Position).LengthSquared();

                    return da.CompareTo(db);
                });

                neighbors.RemoveAll(
                    a => a.MoveType == Creature.MoveType.Jump || a.MoveType == Creature.MoveType.Climb);

                if (neighbors.Count == 0)
                {
                    yield return Status.Fail;
                    yield break;
                }

                Creature.MoveAction furthest = neighbors.Last();
                bool reachedTarget = false;
                while (!reachedTarget)
                {
                    Vector3 output = Creature.Controller.GetOutput(DwarfTime.Dt, furthest.Voxel.Position + Vector3.One*0.5f,
                        Agent.Position);
                    Creature.Physics.ApplyForce(output, DwarfTime.Dt);
                    yield return Status.Running;
                    if ((furthest.Voxel.Position - Agent.Position).Length() < 1)
                    {
                        reachedTarget = true;
                    }
                    Agent.Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
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

            if (Target == null && TargetName != null)
            {
                Target = Agent.Blackboard.GetData<Body>(TargetName);

                if (Target == null)
                {
                    yield return Status.Fail;
                    yield break;
                }
            }

            bool avoided = false;
            while(true)
            {
                Timeout.Update(DwarfTime.LastTime);

                if (Timeout.HasTriggered)
                {
                    if (Training)
                    {
                        Agent.AddXP(10);
                        Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                        Creature.OverrideCharacterMode = false;
                        Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
                        yield return Status.Success;
                        yield break;
                    }
                    else
                    {
                        Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                        Creature.OverrideCharacterMode = false;
                        Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
                        yield return Status.Fail;
                        yield break;
                    }
                }

                if (Target == null || Target.IsDead)
                {
                    Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                    yield return Status.Success;
                }

                // Find the location of the melee target
                Vector3 targetPos = new Vector3(Target.GlobalTransform.Translation.X,
                    Target.GetBoundingBox().Min.Y,
                    Target.GlobalTransform.Translation.Z);

                bool collides = Creature.Physics.Collide(Target.BoundingBox);
                Vector3 diff = targetPos - Creature.AI.Position;

                Creature.Physics.Face(targetPos);

                // If we are far away from the target, run toward it
                if (diff.Length() > CurrentAttack.Range * 8 && !collides)
                {
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
                    yield return Status.Fail;
                }
                if(diff.Length() > CurrentAttack.Range && !collides)
                {
                    Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
                    Vector3 output = Creature.Controller.GetOutput(DwarfTime.Dt, targetPos, Creature.Physics.GlobalTransform.Translation) * 0.9f;
                    output.Y = 0.0f;
                    Creature.Physics.ApplyForce(output, DwarfTime.Dt);

                    if ((targetPos - Creature.AI.Position).Y > 0.3 && Creature.IsOnGround)
                    {
                        Agent.Jump(DwarfTime.LastTime);
                    }
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                }
                else if (!avoided && (CurrentAttack.Mode != Attack.AttackMode.Melee &&
                    diff.Length() < CurrentAttack.Range*0.75f && !collides))
                {
                    /*
                   
                    Vector3 output = Creature.Controller.GetOutput(DwarfTime.Dt, targetPos, Creature.Physics.GlobalTransform.Translation) * 0.9f;
                    output.Y = 0.0f;
                    Creature.Physics.ApplyForce(-output, DwarfTime.Dt);
                     
                    Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                    */
                    foreach (Act.Status stat in AvoidTarget(CurrentAttack.Range, 3.0f))
                    {
                        yield return Status.Running;
                    }
                    avoided = true;
                }
                // Else, stop and attack
                else
                {
                    avoided = false;
                    Creature.Physics.Orientation = Physics.OrientMode.Fixed;
                    Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.9f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.9f);
                    CurrentAttack.RechargeTimer.Reset(CurrentAttack.RechargeRate);

                    Creature.Sprite.ResetAnimations(Creature.CharacterMode.Attacking);
                    Creature.Sprite.PlayAnimations(Creature.CharacterMode.Attacking);
                    Creature.CurrentCharacterMode = Creature.CharacterMode.Attacking;

                    while (!CurrentAttack.Perform(Creature, Target, DwarfTime.LastTime, Creature.Stats.BuffedStr + Creature.Stats.BuffedSiz,
                            Creature.AI.Position, Creature.Faction.Name))
                    {
                        Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.9f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.9f);
                        yield return Status.Running;
                    }

                    while (!Agent.Creature.Sprite.CurrentAnimation.IsDone())
                    {
                        yield return Status.Running;
                    }

                    Creature.CurrentCharacterMode = Creature.CharacterMode.Attacking;
                    Creature.Sprite.PauseAnimations(Creature.CharacterMode.Attacking);

                    CurrentAttack.RechargeTimer.Reset(CurrentAttack.RechargeRate);

                    while (!CurrentAttack.RechargeTimer.HasTriggered)
                    {
                        Creature.Sprite.PauseAnimations(Creature.CharacterMode.Attacking);
                        CurrentAttack.RechargeTimer.Update(DwarfTime.LastTime);
                        Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.9f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.9f);
                        yield return Status.Running;
                    }

                    Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
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

                        Target = null;
                        Agent.AddXP(10);
                        Creature.Physics.Face(Creature.Physics.Velocity + Creature.Physics.GlobalTransform.Translation);
                        Creature.Stats.NumThingsKilled++;
                        Creature.AI.AddThought(Thought.ThoughtType.KilledThing);
                        Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                        Creature.OverrideCharacterMode = false;
                        Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
                        yield return Status.Success;
                        break;
                    }
                
                }

                yield return Status.Running;
            }
        }
    }

}