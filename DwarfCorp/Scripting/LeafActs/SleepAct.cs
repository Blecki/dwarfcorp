using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature remains inactive, recharging its energy until it is satisfied.
    /// </summary>
    public class SleepAct : CreatureAct
    {
        public float RechargeRate { get; set; }

        public bool Teleport { get; set; }

        public Vector3 TeleportLocation { get; set; }

        public Vector3 PreTeleport { get; set; }

        public enum SleepType
        {
            Sleep,
            Heal
        }

        public float HealRate { get; set; }

        public SleepType Type { get; set; }

        public SleepAct()
        {
            Name = "Sleep";
            RechargeRate = 1.0f;
            Teleport = false;
            Type = SleepType.Sleep;
        }

        public SleepAct(CreatureAI creature) :
            base(creature)
        {
            Name = "Sleep";
            RechargeRate = 1.0f;
            Teleport = false;
            Type = SleepType.Sleep;
        }

        public override void OnCanceled()
        {
            Creature.Stats.IsAsleep = false;
            Creature.CurrentCharacterMode = CharacterMode.Idle;
            Creature.OverrideCharacterMode = false;
            Creature.Physics.IsSleeping = false;
            Creature.Physics.AllowPhysicsSleep = false;
            base.OnCanceled();
        }

        public override IEnumerable<Status> Run()
        {
            float startingHealth = Creature.Stats.Health.CurrentValue;
            PreTeleport = Creature.AI.Position;
            if (Type == SleepType.Sleep)
            {
                while (!Creature.Stats.Energy.IsSatisfied() && Creature.Manager.World.Time.IsNight())
                {
                    if (Creature.Physics.IsInLiquid)
                    {
                        Creature.Stats.IsAsleep = false;
                        Creature.CurrentCharacterMode = CharacterMode.Idle;
                        Creature.OverrideCharacterMode = false;
                        yield return Status.Fail;
                    }
                    if (Teleport)
                    {
                        Creature.AI.Position = TeleportLocation;
                        Creature.Physics.Velocity = Vector3.Zero;
                        Creature.Physics.LocalPosition = TeleportLocation;
                        Creature.Physics.AllowPhysicsSleep = true;
                        Creature.Physics.IsSleeping = true;
                    }
                    Creature.CurrentCharacterMode = CharacterMode.Sleeping;
                    Creature.Stats.Energy.CurrentValue += DwarfTime.Dt*RechargeRate;
                    if (Creature.Stats.Health.CurrentValue < startingHealth)
                    {
                        Creature.Stats.IsAsleep = false;
                        Creature.CurrentCharacterMode = CharacterMode.Idle;
                        Creature.OverrideCharacterMode = false;
                        yield return Status.Fail;
                    }

                    Creature.Stats.IsAsleep = true;
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Running;
                }

                if (Teleport)
                {
                    Creature.AI.Position = PreTeleport;
                    Creature.Physics.Velocity = Vector3.Zero;
                    Creature.Physics.LocalPosition = TeleportLocation;
                    Creature.Physics.IsSleeping = false;
                    Creature.Physics.AllowPhysicsSleep = false;
                }

                Creature.AddThought(Thought.ThoughtType.Slept);
                Creature.Stats.IsAsleep = false;
                Creature.Physics.IsSleeping = false;
                Creature.Physics.AllowPhysicsSleep = false;
                yield return Status.Success;
            }
            else
            {
                while (Creature.Stats.Health.IsDissatisfied() || Creature.Stats.Buffs.Any(buff => buff is Disease))
                {
                    if (Creature.Physics.IsInLiquid)
                    {
                        Creature.Stats.IsAsleep = false;
                        Creature.CurrentCharacterMode = CharacterMode.Idle;
                        Creature.OverrideCharacterMode = false;
                        yield return Status.Fail;
                    }
                    if (Teleport)
                    {
                        Creature.AI.Position = TeleportLocation;
                        Creature.Physics.Velocity = Vector3.Zero;
                        Creature.Physics.LocalPosition = TeleportLocation;
                        Creature.Physics.IsSleeping = true;
                        Creature.Physics.AllowPhysicsSleep = true;
                    }
                    Creature.CurrentCharacterMode = CharacterMode.Sleeping;
                    Creature.Stats.Energy.CurrentValue += DwarfTime.Dt*RechargeRate;
                    Creature.Heal(DwarfTime.Dt * HealRate);
                    Creature.Stats.IsAsleep = true;
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Running;
                }

                if (Teleport)
                {
                    Creature.AI.Position = PreTeleport;
                    Creature.Physics.Velocity = Vector3.Zero;
                    Creature.Physics.LocalPosition = TeleportLocation;
                    Creature.Physics.IsSleeping = false;
                    Creature.Physics.AllowPhysicsSleep = false;
                }
                Creature.AddThought(Thought.ThoughtType.Slept);
                Creature.Stats.IsAsleep = false;
                Creature.Physics.IsSleeping = false;
                Creature.Physics.AllowPhysicsSleep = false;
                yield return Status.Success;
            }
        }
    }
}
