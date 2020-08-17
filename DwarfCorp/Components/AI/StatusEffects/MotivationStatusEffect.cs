using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class BaselineMotivationStatusEffect : StatusEffect
    {
        public float MotivationAdjustment;

        public BaselineMotivationStatusEffect()
        {
        }

        public BaselineMotivationStatusEffect(float MotivationAdjustment) :
            base(100.0f)
        {
            this.MotivationAdjustment = MotivationAdjustment;
        }

        public override StatusEffect Clone()
        {
            return new BaselineMotivationStatusEffect
            {
                EffectTime = Timer.Clone(EffectTime),
                Particles = Particles,
                ParticleTimer = Timer.Clone(ParticleTimer),
                SoundOnEnd = SoundOnEnd,
                SoundOnStart = SoundOnStart,
                MotivationAdjustment = MotivationAdjustment,
            };
        }

        public override string GetDescription()
        {
            return "Baseline Motivation (" + MotivationAdjustment + ")";
        }

        public override void Update(DwarfTime time, Creature creature)
        {
            creature.Stats.Motivation.CurrentValue += MotivationAdjustment; // Motivation must be re-applied periodically... (every frame)
            base.Update(time, creature);
        }

        public override void OnApply(Creature creature)
        {
            base.OnApply(creature);
        }

        public override void OnEnd(Creature creature)
        {
            base.OnEnd(creature);
        }

        public override bool IsRelevant(Creature creature)
        {
            return creature.Stats.Buffs.Count == 0;
        }
    }

    public class ManagerMotivationStatusEffect : StatusEffect
    {
        public float MotivationAdjustment;

        public ManagerMotivationStatusEffect()
        {
        }

        public ManagerMotivationStatusEffect(float MotivationAdjustment) :
            base(100.0f)
        {
            this.MotivationAdjustment = MotivationAdjustment;
        }

        public override StatusEffect Clone()
        {
            return new ManagerMotivationStatusEffect
            {
                EffectTime = Timer.Clone(EffectTime),
                Particles = Particles,
                ParticleTimer = Timer.Clone(ParticleTimer),
                SoundOnEnd = SoundOnEnd,
                SoundOnStart = SoundOnStart,
                MotivationAdjustment = MotivationAdjustment,
            };
        }

        public override string GetDescription()
        {
            return "There's a manager nearby! (" + MotivationAdjustment + ")";
        }

        public override void Update(DwarfTime time, Creature creature)
        {
            creature.Stats.Motivation.CurrentValue += MotivationAdjustment; // Motivation must be re-applied periodically... (every frame)
            base.Update(time, creature);
        }

        public override void OnApply(Creature creature)
        {
            base.OnApply(creature);
        }

        public override void OnEnd(Creature creature)
        {
            base.OnEnd(creature);
        }

        public override bool IsRelevant(Creature creature)
        {
            return creature.Stats.Buffs.Count == 0;
        }
    }

    public class HappinessMotivationStatusEffect : StatusEffect
    {
        public float MotivationAdjustment;

        public HappinessMotivationStatusEffect()
        {
        }

        public HappinessMotivationStatusEffect(float MotivationAdjustment) :
            base(100.0f)
        {
            this.MotivationAdjustment = MotivationAdjustment;
        }

        public override StatusEffect Clone()
        {
            return new HappinessMotivationStatusEffect
            {
                EffectTime = Timer.Clone(EffectTime),
                Particles = Particles,
                ParticleTimer = Timer.Clone(ParticleTimer),
                SoundOnEnd = SoundOnEnd,
                SoundOnStart = SoundOnStart,
                MotivationAdjustment = MotivationAdjustment,
            };
        }

        public override string GetDescription()
        {
            return "Happiness Motivation Adjustment (" + MotivationAdjustment + ")";
        }

        public override void Update(DwarfTime time, Creature creature)
        {
            creature.Stats.Motivation.CurrentValue += MotivationAdjustment; // Motivation must be re-applied periodically... (every frame)
            base.Update(time, creature);
        }

        public override void OnApply(Creature creature)
        {
            base.OnApply(creature);
        }

        public override void OnEnd(Creature creature)
        {
            base.OnEnd(creature);
        }

        public override bool IsRelevant(Creature creature)
        {
            return creature.Stats.Buffs.Count == 0;
        }
    }

}
