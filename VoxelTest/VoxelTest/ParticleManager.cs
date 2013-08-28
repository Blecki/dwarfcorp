using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class ParticleManager
    {
        public Dictionary<string, ParticleEmitter> Emitters { get; set; }
        public ComponentManager Components { get; set; }

        public ParticleManager(ComponentManager components)
        {
            Components = components;
            Emitters = new Dictionary<string, ParticleEmitter>();
        }

        public void Trigger(string emitter, Vector3 position, Color tint, int num)
        {
            Emitters[emitter].Trigger(num, position, tint);
        }

        public void RegisterEffect(string name, EmitterData data)
        {
            Emitters[name] = new ParticleEmitter(Components, name, Components.RootComponent, Matrix.Identity, data);
            Emitters[name].LightsWithVoxels = false;
            Emitters[name].DepthSort = false;
            Emitters[name].Tint = Color.White;
            Emitters[name].FrustrumCull = false;
        }


    }
}
