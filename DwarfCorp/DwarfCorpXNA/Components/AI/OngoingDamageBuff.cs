using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Applies damage to the creature over time.
    /// </summary>
    public class OngoingDamageBuff : Buff
    {
        /// <summary> The type of damage to apply </summary>
        public Creature.DamageType DamageType { get; set; }
        /// <summary> The amount of damage to take in HP per second </summary>
        public float DamagePerSecond { get; set; }

        public override void Update(DwarfTime time, Creature creature)
        {
            var dt = (float)time.ElapsedGameTime.TotalSeconds;
            creature.Damage(DamagePerSecond * dt, DamageType);
            base.Update(time, creature);
        }

        public override Buff Clone()
        {
            return new OngoingDamageBuff
            {
                EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                Particles = Particles,
                ParticleTimer =
                    new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                SoundOnEnd = SoundOnEnd,
                SoundOnStart = SoundOnStart,
                DamageType = DamageType,
                DamagePerSecond = DamagePerSecond
            };
        }
    }
}
