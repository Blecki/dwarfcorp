using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Heals the creature continuously over time.
    /// </summary>
    public class OngoingHealBuff : Buff
    {
        public OngoingHealBuff()
        {
        }

        public OngoingHealBuff(float dps, float time) :
            base(time)
        {
            DamagePerSecond = dps;
        }

        /// <summary> Amount to heal the creature in HP per second </summary>
        public float DamagePerSecond { get; set; }

        public override void Update(DwarfTime time, Creature creature)
        {
            var dt = (float)time.ElapsedGameTime.TotalSeconds;
            creature.Heal(dt * DamagePerSecond);

            base.Update(time, creature);
        }

        public override Buff Clone()
        {
            return new OngoingHealBuff
            {
                EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                Particles = Particles,
                ParticleTimer =
                    new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                SoundOnEnd = SoundOnEnd,
                SoundOnStart = SoundOnStart,
                DamagePerSecond = DamagePerSecond
            };
        }
    }
}
