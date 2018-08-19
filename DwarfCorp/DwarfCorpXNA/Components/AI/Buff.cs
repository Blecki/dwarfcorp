using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A buff is an ongoing effect applied to a creature. This can heal the creature,
    /// damage it, or apply any other kind of effect.
    /// </summary>
    public class Buff
    {
        public Buff()
        {
        }

        /// <summary>
        /// Create a buff which persists for the specified time.
        /// </summary>
        public Buff(float time)
        {
            EffectTime = new Timer(time, true);
            ParticleTimer = new Timer(0.25f, false, Timer.TimerMode.Real);
        }

        /// <summary> Time that the effect persists for </summary>
        public Timer EffectTime { get; set; }

        /// <summary> If true, the buff is active. </summary>
        public bool IsInEffect
        {
            get { return !EffectTime.HasTriggered; }
        }

        /// <summary> Particles to generate during the buff. </summary>
        public string Particles { get; set; }
        /// <summary> Every time this triggers, a particle gets released </summary>
        public Timer ParticleTimer { get; set; }
        /// <summary> Sound to play when the buff starts </summary>
        public string SoundOnStart { get; set; }
        /// <summary> Sound to play when the buff ends </summary>
        public string SoundOnEnd { get; set; }


        /// <summary> Called when the Buff is added to a Creature </summary>
        public virtual void OnApply(Creature creature)
        {
            if (!string.IsNullOrEmpty(SoundOnStart))
            {
                SoundManager.PlaySound(SoundOnStart, creature.Physics.Position, true, 0.0f);
            }
        }

        /// <summary> Called when the Buff is removed from a Creature </summary>
        public virtual void OnEnd(Creature creature)
        {
            if (!string.IsNullOrEmpty(SoundOnEnd))
            {
                SoundManager.PlaySound(SoundOnEnd, creature.Physics.Position, true, 1.0f);
            }
        }

        public virtual bool IsRelevant(Creature creature)
        {
            return true;
        }

        /// <summary> Updates the Buff </summary>
        public virtual void Update(DwarfTime time, Creature creature)
        {
            EffectTime.Update(time);
            ParticleTimer.Update(time);

            if (ParticleTimer.HasTriggered && !string.IsNullOrEmpty(Particles))
            {
                creature.Manager.World.ParticleManager.Trigger(Particles, creature.Physics.Position, Color.White, 1);
            }
        }

        /// <summary> Creates a new Buff that is a deep copy of this one. </summary>
        public virtual Buff Clone()
        {
            return new Buff
            {
                EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                Particles = Particles,
                ParticleTimer =
                    new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                SoundOnEnd = SoundOnEnd,
                SoundOnStart = SoundOnStart
            };
        }
    }
}
