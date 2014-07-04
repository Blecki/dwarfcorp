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

        public MeleeAct(CreatureAI agent, Body target) :
            base(agent)
        {
            Name = "Attack!";
            EnergyLoss = 200.0f;
            Target = target;
            foreach (Attack attack in agent.Creature.Attacks.Where(attack => attack.Mode == Attack.AttackMode.Melee))
            {
                CurrentAttack = attack;
                break;
            }
        }

        public override IEnumerable<Status> Run()
        {
            if (CurrentAttack == null)
            {
                yield return Status.Fail;
                yield break;
            }

            bool targetDead = false;
            Creature.Sprite.ResetAnimations(Creature.CharacterMode.Attacking);
            while(!targetDead)
            {
                // Find the location of the melee target
                Creature.LocalTarget = new Vector3(Target.GlobalTransform.Translation.X,
                    Creature.Physics.GlobalTransform.Translation.Y,
                    Target.GlobalTransform.Translation.Z);

                Creature.Physics.Collide(Target.BoundingBox);
                Vector3 diff = Creature.LocalTarget - Creature.Physics.GlobalTransform.Translation;

                Creature.Physics.Face(Creature.LocalTarget);

                // If we are close to the target, apply force to it
                if(diff.Length() > 1.0f)
                {
                    Vector3 output = Creature.Controller.GetOutput(Act.Dt, Creature.LocalTarget, Creature.Physics.GlobalTransform.Translation) * 0.9f;
                    Creature.Physics.ApplyForce(output, Act.Dt);
                    output.Y = 0.0f;

                    if((Creature.LocalTarget - Creature.Physics.GlobalTransform.Translation).Y > 0.3)
                    {
                        Agent.Jump(Act.LastTime);
                    }
                    Creature.Physics.OrientWithVelocity = true;
                }

                // Else run toward the target
                else
                {
                    Creature.Physics.OrientWithVelocity = false;
                    Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.9f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.9f);
                }

                CurrentAttack.Perform(Target, Act.LastTime, Creature.Stats.Strength + Creature.Stats.Size, Creature.AI.Position);
                if(Target.IsDead)
                {
                    if (Creature.Faction.ChopDesignations.Contains(Target))
                    {
                        Creature.Faction.ChopDesignations.Remove(Target);
                    }

                    if (Creature.Faction.AttackDesignations.Contains(Target))
                    {
                        Creature.Faction.AttackDesignations.Remove(Target);
                    }

                    Target= null;
                    Agent.Stats.XP += 10;
                    Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
                    Creature.Physics.OrientWithVelocity = true;
                    Creature.Physics.Face(Creature.Physics.Velocity + Creature.Physics.GlobalTransform.Translation);
                    Creature.Stats.NumThingsKilled++;
                    yield return Status.Success;
                    targetDead = true;
                    break;
                }
                else
                {
                    /*
                    Creature creature = Target.GetChildrenOfType<Creature>().FirstOrDefault();

                    if (creature != null)
                    {
                        creature.AI.Tasks.Add(new KillEntityTask(Creature.Physics));
                    }
                     */
                }

                Creature.CurrentCharacterMode = Creature.CharacterMode.Attacking;
                Creature.Status.Energy.CurrentValue -= EnergyLoss * Dt * Creature.Stats.Tiredness;

                yield return Status.Running;
            }
        }
    }

}