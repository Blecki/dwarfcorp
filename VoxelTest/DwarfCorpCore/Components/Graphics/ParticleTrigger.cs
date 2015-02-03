using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// This component emits particles either on the object's death, or when
    /// explicitly triggered.
    /// </summary>
    public class ParticleTrigger : Tinter
    {
        public string EmitterName { get; set; }
        public bool TriggerOnDeath { get; set; }
        public int TriggerAmount { get; set; }
        public bool TriggerInBox { get; set; }
        public int BoxTriggerTimes { get; set; }
        public string SoundToPlay { get; set; }

        public ParticleTrigger()
        {
            
        }

        public ParticleTrigger(string emitter, ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(name, parent, localTransform, boundingBoxExtents, boundingBoxPos, false)
        {
            SoundToPlay = ContentPaths.Audio.explode;
            EmitterName = emitter;
            TriggerOnDeath = true;
            TriggerAmount = 10;
            BoxTriggerTimes = 10;
            TriggerInBox = true;
        }

        public void Trigger()
        {
            Vector3 p = GlobalTransform.Translation;
            if(TriggerInBox)
            {
                Vector3 ext = GetBoundingBox().Max - GetBoundingBox().Min;
                for(int i = 0; i < BoxTriggerTimes; i++)
                {
                    Vector3 triggerPos = GetBoundingBox().Min + new Vector3(MathFunctions.Rand() * ext.X,
                        MathFunctions.Rand() * ext.Y,
                        MathFunctions.Rand() * ext.Z)
                        ;
                    PlayState.ParticleManager.Effects[EmitterName].Trigger(TriggerAmount, triggerPos, Tint);
                }
            }
            else
            {
                PlayState.ParticleManager.Effects[EmitterName].Trigger(TriggerAmount, p, Tint);
            }
        }

        public override void Die()
        {
            if(TriggerOnDeath)
            {
                if(!string.IsNullOrEmpty(SoundToPlay))
                    SoundManager.PlaySound(SoundToPlay, GlobalTransform.Translation);
                Trigger();
            }
            base.Die();
        }
    }

}