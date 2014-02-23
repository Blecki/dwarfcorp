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

        public MeleeAct(CreatureAIComponent agent) :
            base(agent)
        {
            Name = "Attack!";
            EnergyLoss = 200.0f;
        }

        public override IEnumerable<Status> Run()
        {
            bool targetDead = false;
            Creature.Sprite.ResetAnimations(Creature.CharacterMode.Attacking);
            while(!targetDead)
            {
                Creature.LocalTarget = new Vector3(Agent.TargetComponent.GlobalTransform.Translation.X,
                    Creature.Physics.GlobalTransform.Translation.Y,
                    Agent.TargetComponent.GlobalTransform.Translation.Z);


                Vector3 diff = Creature.LocalTarget - Creature.Physics.GlobalTransform.Translation;

                Creature.Physics.Face(Creature.LocalTarget);


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
                else
                {
                    Creature.Physics.OrientWithVelocity = false;
                    Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.9f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.9f);
                }

                List<HealthComponent> healths = Agent.TargetComponent.GetChildrenOfTypeRecursive<HealthComponent>();

                foreach(HealthComponent health in healths)
                {
                    health.Damage(Creature.Stats.BaseChopSpeed * (float) Act.LastTime.ElapsedGameTime.TotalSeconds);
                }

                if(Agent.TargetComponent.IsDead)
                {
                    Creature.Faction.ChopDesignations.Remove(Agent.TargetComponent);
                    Agent.TargetComponent = null;

                    Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
                    Creature.Physics.OrientWithVelocity = true;
                    Creature.Physics.Face(Creature.Physics.Velocity + Creature.Physics.GlobalTransform.Translation);
                    yield return Status.Success;
                    targetDead = true;
                    break;
                }

                Creature.CurrentCharacterMode = Creature.CharacterMode.Attacking;

                Creature.Weapon.PlayNoise();
                Creature.Status.Energy.CurrentValue -= EnergyLoss * Dt * Creature.Stats.Tiredness;

                PhysicsComponent component = Agent.TargetComponent as PhysicsComponent;
                if(component != null)
                {
                    if(PlayState.Random.Next(100) < 10)
                    {
                        PhysicsComponent phys = component;
                        {
                            PlayState.ParticleManager.Trigger("blood_particle", phys.GlobalTransform.Translation, Color.White, 5);
                        }


                        Vector3 f = phys.GlobalTransform.Translation - Creature.Physics.GlobalTransform.Translation;
                        if(f.Length() > 2.0f)
                        {
                            Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
                            Creature.Physics.OrientWithVelocity = true;
                            Creature.Physics.Face(Creature.Physics.Velocity + Creature.Physics.GlobalTransform.Translation);
                            yield return Status.Fail;
                            break;
                        }
                        f.Y = 0.0f;

                        f.Normalize();
                        f *= 20;


                        phys.ApplyForce(f, Dt);
                    }
                }

                yield return Status.Running;
            }
        }
    }

}