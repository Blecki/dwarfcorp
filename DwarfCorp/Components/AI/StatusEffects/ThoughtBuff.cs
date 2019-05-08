using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary> Causes the creature to have a Thought for a specified time </summary>
    public class ThoughtBuff : StatusEffect
    {
        public ThoughtBuff()
        {
        }

        public ThoughtBuff(float time, Thought.ThoughtType type) :
            base(time)
        {
            ThoughtType = type;
        }

        /// <summary> The Thought the creature has during the buff </summary>
        public Thought.ThoughtType ThoughtType { get; set; }

        public override void OnApply(Creature creature)
        {
            creature.Physics.GetComponent<DwarfThoughts>()?.AddThought(ThoughtType);
            base.OnApply(creature);
        }

        public override void OnEnd(Creature creature)
        {
            creature.Physics.GetComponent<DwarfThoughts>()?.RemoveThought(ThoughtType);
            base.OnApply(creature);
        }

        public override StatusEffect Clone()
        {
            return new ThoughtBuff
            {
                EffectTime = Timer.Clone(EffectTime),
                Particles = Particles,
                ParticleTimer = Timer.Clone(ParticleTimer),
                SoundOnEnd = SoundOnEnd,
                SoundOnStart = SoundOnStart,
                ThoughtType = ThoughtType
            };
        }

        public override bool IsRelevant(Creature creature)
        {
            return creature.Stats.Happiness.IsDissatisfied();
        }
    }
}
