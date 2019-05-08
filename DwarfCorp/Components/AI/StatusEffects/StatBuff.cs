using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary> Increases the creature's stats for a time </summary>
    public class StatBuff : StatusEffect
    {
        public StatBuff()
        {
            Buffs = new StatAdjustment();
        }

        public StatBuff(float time, StatAdjustment buffs) :
            base(time)
        {
            Buffs = buffs;
        }

        /// <summary> The amount to add to the creature's stats </summary>
        public StatAdjustment Buffs { get; set; }

        public override StatusEffect Clone()
        {
            return new StatBuff
            {
                EffectTime = Timer.Clone(EffectTime),
                Particles = Particles,
                ParticleTimer = Timer.Clone(ParticleTimer),
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
            creature.Stats.AddStatAdjustment(Buffs);
            base.OnApply(creature);
        }

        public override void OnEnd(Creature creature)
        {
            creature.Stats.RemoveStatAdjustment(Buffs.Name);
            base.OnEnd(creature);
        }

        public override bool IsRelevant(Creature creature)
        {
            return creature.Stats.Buffs.Count == 0;
        }
    }
}
