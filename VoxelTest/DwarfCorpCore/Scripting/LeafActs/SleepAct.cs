using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Scripting.LeafActs
{
    /// <summary>
    /// A creature remains inactive, recharging its energy until it is satisfied.
    /// </summary>
    public class SleepAct : CreatureAct
    {
        public float RechargeRate { get; set; }

        public bool Teleport { get; set; }

        public Vector3 TeleportLocation { get; set; }

        public SleepAct()
        {
            Name = "Sleep";
            RechargeRate = 1.0f;
            Teleport = false;
        }

        public SleepAct(CreatureAI creature) :
            base(creature)
        {
            Name = "Sleep";
            RechargeRate = 1.0f;
            Teleport = false;
        }

        public override void OnCanceled()
        {
            Creature.Status.IsAsleep = false;
            Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
            Creature.OverrideCharacterMode = false;
            base.OnCanceled();
        }

        public override IEnumerable<Status> Run()
        {
            float startingHealth = Creature.Status.Health.CurrentValue;
            while(!Creature.Status.Energy.IsSatisfied() && PlayState.Time.IsNight())
            {
                if(Teleport)
                {
                    Creature.AI.Position = TeleportLocation;
                }
                Creature.Status.Energy.CurrentValue += Dt * RechargeRate;
                if (Creature.Status.Health.CurrentValue < startingHealth || Creature.Sensors.Enemies.Count > 0)
                {
                    Creature.Status.IsAsleep = false;
                    Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
                    yield return Status.Fail;
                }

                Creature.Status.Health.CurrentValue = Creature.Status.Health.MaxValue;
                Creature.Status.IsAsleep = true;
                yield return Status.Running;
            }
            Creature.AI.AddThought(Thought.ThoughtType.Slept);
            Creature.Status.IsAsleep = false;
            yield return Status.Success;
        }
    }
}
