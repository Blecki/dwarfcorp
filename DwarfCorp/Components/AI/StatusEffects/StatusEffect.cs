using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class StatusEffect
    {
        public StatusEffect()
        {
        }

        public StatusEffect(float time)
        {
            EffectTime = new Timer(time, true);
            ParticleTimer = new Timer(0.25f, false, Timer.TimerMode.Real);
        }

        public Timer EffectTime;

        [JsonIgnore]
        public bool IsInEffect
        {
            get { return !EffectTime.HasTriggered; }
        }

        public string Particles;
        public Timer ParticleTimer;
        public string SoundOnStart;
        public string SoundOnEnd;

        /// <summary> Called when the Buff is added to a Creature </summary>
        public virtual void OnApply(Creature creature)
        {
            if (!string.IsNullOrEmpty(SoundOnStart))
                SoundManager.PlaySound(SoundOnStart, creature.Physics.Position, true, 0.0f);
        }

        /// <summary> Called when the Buff is removed from a Creature </summary>
        public virtual void OnEnd(Creature creature)
        {
            if (!string.IsNullOrEmpty(SoundOnEnd))
                SoundManager.PlaySound(SoundOnEnd, creature.Physics.Position, true, 1.0f);
        }

        public virtual bool IsRelevant(Creature creature)
        {
            return true;
        }

        /// <summary> Updates the Buff </summary>
        public virtual void Update(DwarfTime time, Creature creature)
        {
            if (EffectTime != null)
                EffectTime.Update(time);

            if (ParticleTimer != null)
            {
                ParticleTimer.Update(time);

                if (ParticleTimer.HasTriggered && !string.IsNullOrEmpty(Particles))
                    creature.Manager.World.ParticleManager.Trigger(Particles, creature.Physics.Position, Color.White, 1);
            }
        }

        /// <summary> Creates a new Buff that is a deep copy of this one. </summary>
        public virtual StatusEffect Clone()
        {
            return new StatusEffect
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
