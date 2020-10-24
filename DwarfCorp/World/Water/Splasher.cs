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
