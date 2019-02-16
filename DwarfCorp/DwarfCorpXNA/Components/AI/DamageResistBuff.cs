using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    ///<summary> A Buff which allows the creature to resist some amount of damage of a specific kind </summary>
    public class DamageResistBuff : Buff
    {
        public DamageResistBuff()
        {
        }

        /// <summary> The kind of damage to ignore </summary>
        public Creature.DamageType DamageType { get; set; }
        /// <summary> The amount of damage to ignore. </summary>
        public float Bonus { get; set; }

        public override Buff Clone()
        {
            return new DamageResistBuff
            {
                EffectTime = Timer.Clone(EffectTime),
                Particles = Particles,
                ParticleTimer = Timer.Clone(ParticleTimer),
                SoundOnEnd = SoundOnEnd,
                SoundOnStart = SoundOnStart,
                DamageType = DamageType,
                Bonus = Bonus
            };
        }

        public override void OnApply(Creature creature)
        {
            creature.Resistances[DamageType] += Bonus;
            base.OnApply(creature);
        }

        public override void OnEnd(Creature creature)
        {
            creature.Resistances[DamageType] -= Bonus;
            base.OnEnd(creature);
        }

        public override bool IsRelevant(Creature creature)
        {
            return creature.Hp < creature.MaxHealth;
        }
    }

}
