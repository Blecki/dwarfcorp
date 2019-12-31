using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Heals the creature continuously over time.
    /// </summary>
    public class OngoingHealBuff : StatusEffect
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
        private Timer DamageTimer = new Timer(1.0f, false);
        public override void Update(DwarfTime time, Creature creature)
        {
            DamageTimer.Update(time);

            if (DamageTimer.HasTriggered)
            {
                creature.Heal(DamagePerSecond);
                creature.DrawLifeTimer.Reset();
                IndicatorManager.DrawIndicator((DamagePerSecond).ToString() + " HP",
                    creature.Physics.Position, 1.0f,
                     GameSettings.Current.Colors.GetColor("Positive", Microsoft.Xna.Framework.Color.Green));
            }
           
            base.Update(time, creature);
        }

        public override StatusEffect Clone()
        {
            return new OngoingHealBuff
            {
                EffectTime = Timer.Clone(EffectTime),
                Particles = Particles,
                ParticleTimer = Timer.Clone(ParticleTimer),
                SoundOnEnd = SoundOnEnd,
                SoundOnStart = SoundOnStart,
                DamagePerSecond = DamagePerSecond
            };
        }

        public override bool IsRelevant(Creature creature)
        {
            return creature.Hp < creature.MaxHealth;
        }
    }
}
