using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class CureDiseaseBuff : StatusEffect
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
                disease.OnEnd(creature);

            creature.Buffs.RemoveAll(buff => buff is Disease);
            base.OnApply(creature);
        }

        public override StatusEffect Clone()
        {
            return new CureDiseaseBuff
            {
                EffectTime = Timer.Clone(EffectTime),
                Particles = Particles,
                ParticleTimer = Timer.Clone(ParticleTimer),
                SoundOnEnd = SoundOnEnd,
                SoundOnStart = SoundOnStart,
            };
        }
    }

}
