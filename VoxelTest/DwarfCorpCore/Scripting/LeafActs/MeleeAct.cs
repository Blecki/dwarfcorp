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

        public IEnumerable<Status> AvoidTarget(float range)
        {
            if (Target == null)
            {
                yield return Status.Fail;
                yield break;
            }
            while (true)
            {
                float dist = (Target.Position - Agent.Position).Length();

                if (dist > range)
                {
                    yield return Status.Success;
                    yield break;
                }

                List<Creature.MoveAction> neighbors = Agent.Chunks.ChunkData.GetMovableNeighbors(Agent.Position);
                neighbors.Sort((a, b) =>
                {
                    if (a.Equals(b)) return 0;

                    float da = (a.Voxel.Position - Target.Position).LengthSquared();
                    float db = (b.Voxel.Position - Target.Position).LengthSquared();

                    return da.CompareTo(db);
                });

                neighbors.RemoveAll(
                    a => a.MoveType == Creature.MoveType.Jump || a.MoveType == DwarfCorp.Creature.MoveType.Climb);

                if (neighbors.Count == 0)
                {
                    yield return Status.Fail;
                    yield break;
                }

                Creature.MoveAction furthest = neighbors.Last();

                Vector3 output = Creature.Controller.GetOutput(DwarfTime.Dt, furthest.Voxel.Position + Vector3.One*0.5f, Agent.Position);
                Creature.Physics.ApplyForce(output, DwarfTime.Dt);
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
                else if (CurrentAttack.Mode != Attack.AttackMode.Melee &&
                    diff.Length() < CurrentAttack.Range*0.75f && !collides)
                {
                    /*
                   
                    Vector3 output = Creature.Controller.GetOutput(DwarfTime.Dt, targetPos, Creature.Physics.GlobalTransform.Translation) * 0.9f;
                    output.Y = 0.0f;
                    Creature.Physics.ApplyForce(-output, DwarfTime.Dt);
                     
                    Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                    */
                    foreach (Act.Status stat in AvoidTarget(CurrentAttack.Range))
                    {
                        yield return Status.Running;
                    }
                }
                // Else, stop and attack
                else
                {
                    Creature.Physics.Orientation = Physics.OrientMode.Fixed;
                    Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.9f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.9f);
                    Creature.Sprite.ResetAnimations(Creature.CharacterMode.Attacking);
                    Creature.CurrentCharacterMode = Creature.CharacterMode.Attacking;
                    CurrentAttack.RechargeTimer.Reset(CurrentAttack.RechargeRate);
                    while (
                        !CurrentAttack.Perform(Target, DwarfTime.LastTime, Creature.Stats.BuffedStr + Creature.Stats.BuffedSiz,
                            Creature.AI.Position))
                    {
                        Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.9f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.9f);
                        yield return Status.Running;
                    }
                    CurrentAttack.RechargeTimer.Reset(CurrentAttack.RechargeRate);
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