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
                     GameSettings.Default.Colors.GetColor("Positive", Microsoft.Xna.Framework.Color.Green));
            }
           
            base.Update(time, creature);
        }

        public override Buff Clone()
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

    public class CureDiseaseBuff : Buff
    {

        public CureDiseaseBuff()
        {
            EffectTime = new Timer(1.0f, true);
            ParticleTimer = new Timer(1.0f, true);
        }

        public override bool IsRelevant(Creature creature)
        {
            return creature.Buffs.Any(buff => buff is Disease);
        }

        public override void OnApply(Creature creature)
        {
            foreach(var disease in creature.Buffs.OfType<Disease>())
            {
                disease.OnEnd(creature);
            }

            creature.Buffs.RemoveAll(buff => buff is Disease);
            base.OnApply(creature);
        }
    }

}
