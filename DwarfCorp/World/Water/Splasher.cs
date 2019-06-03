// WaterManager.cs
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
using Microsoft.Xna.Framework;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    public struct LiquidSplash
    {
        public string name;
        public Vector3 position;
        public int numSplashes;
        public string sound;
        public string entity;
    }

    public class Splasher
    {
        private Dictionary<string, Timer> splashNoiseLimiter = new Dictionary<string, Timer>();
        private ChunkManager Chunks { get; set; }

        public Splasher(ChunkManager Chunks)
        {
            this.Chunks = Chunks;

            splashNoiseLimiter["splat"] = new Timer(0.1f, false);
            splashNoiseLimiter["flame"] = new Timer(0.1f, false);
        }

        public void Splash(DwarfTime time, IEnumerable<LiquidSplash> Splashes)
        {
            foreach (var splash in Splashes)
            {
                Chunks.World.ParticleManager.Trigger(splash.name, splash.position + new Vector3(0.5f, 0.5f, 0.5f), Color.White, splash.numSplashes);
                if (splash.entity != null)
                {
                    EntityFactory.CreateEntity<GameComponent>(splash.entity, splash.position + Vector3.One * 0.5f);
                }
                if (splashNoiseLimiter[splash.name].HasTriggered)
                    SoundManager.PlaySound(splash.sound, splash.position + new Vector3(0.5f, 0.5f, 0.5f), true);
            }

            foreach (var t in splashNoiseLimiter.Values)
                t.Update(time);
        }
    }
}
