using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Applies damage to the creature over time.
    /// </summary>
    public class OngoingDamageBuff : StatusEffect
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

        public override StatusEffect Clone()
        {
            return new OngoingDamageBuff
            {
                EffectTime = Timer.Clone(EffectTime),
                Particles = Particles,
                ParticleTimer = Timer.Clone(ParticleTimer),
                SoundOnEnd = SoundOnEnd,
                SoundOnStart = SoundOnStart,
                DamageType = DamageType,
                DamagePerSecond = DamagePerSecond,
            };
        }
    }
}
