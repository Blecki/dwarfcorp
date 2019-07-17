using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary> Causes the creature to have a Thought for a specified time </summary>
    /// Literally only used by PotionOfGlee...
    public class ThoughtBuff : StatusEffect
    {
        public string Description = "";
        public float HappinessModifier = 0.0f;
        [JsonProperty] private Thought SavedThought;

        public ThoughtBuff()
        {
        }

        public override void OnApply(Creature creature)
        {
            SavedThought = creature.AddThought(Description, new TimeSpan(1, 0, 0, 0), HappinessModifier);
            base.OnApply(creature);
        }

        public override void OnEnd(Creature creature)
        {
            if (creature.Physics.GetComponent<DwarfThoughts>().HasValue(out var thoughts))
                thoughts.RemoveThought(SavedThought);
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
                Description = Description,
                HappinessModifier = HappinessModifier
            };
        }

        public override bool IsRelevant(Creature creature)
        {
            return creature.Stats.Happiness.IsDissatisfied();
        }
    }
}
