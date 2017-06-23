using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary> Increases the creature's stats for a time </summary>
    public class StatBuff : Buff
    {
        public StatBuff()
        {
            Buffs = new CreatureStats.StatNums();
        }

        public StatBuff(float time, CreatureStats.StatNums buffs) :
            base(time)
        {
            Buffs = buffs;
        }

        /// <summary> The amount to add to the creature's stats </summary>
        public CreatureStats.StatNums Buffs { get; set; }

        public override Buff Clone()
        {
            return new StatBuff
            {
                EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                Particles = Particles,
                ParticleTimer =
                    new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                SoundOnEnd = SoundOnEnd,
                SoundOnStart = SoundOnStart,
                Buffs = Buffs
            };
        }

        public override void Update(DwarfTime time, Creature creature)
        {
            base.Update(time, creature);
        }

        public override void OnApply(Creature creature)
        {
            creature.Stats.StatBuffs += Buffs;
            base.OnApply(creature);
        }

        public override void OnEnd(Creature creature)
        {
            creature.Stats.StatBuffs -= Buffs;
            base.OnEnd(creature);
        }
    }
}
