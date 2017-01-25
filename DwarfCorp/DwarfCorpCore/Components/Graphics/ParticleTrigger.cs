// ParticleTrigger.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
            TriggerAmount = 2;
            BoxTriggerTimes = 10;
            TriggerInBox = true;
        }

        public void Trigger()
        {
            Trigger(TriggerAmount);
        }

        public void Trigger(int num)
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
                    World.ParticleManager.Effects[EmitterName].Trigger(num, triggerPos, Tint);
                }
            }
            else
            {
                World.ParticleManager.Effects[EmitterName].Trigger(num, p, Tint);
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