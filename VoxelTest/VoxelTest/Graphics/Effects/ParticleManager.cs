using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This class manages a set of particle effects, and allows them to be triggered
    /// at locations in 3D space.
    /// </summary>
    [JsonObject(IsReference =  true)]
    public class ParticleManager
    {
        public Dictionary<string, ParticleEmitter> Emitters { get; set; }
        public ComponentManager Components { get; set; }

        public ParticleManager(ComponentManager components)
        {
            Components = components;
            Emitters = new Dictionary<string, ParticleEmitter>();
            components.ParticleManager = this;
        }

        public void Trigger(string emitter, Vector3 position, Color tint, int num)
        {
            Emitters[emitter].Trigger(num, position, tint);
        }

        public void RegisterEffect(string name, EmitterData data)
        {
            Emitters[name] = new ParticleEmitter(Components, name, Components.RootComponent, Matrix.Identity, data)
            {
                LightsWithVoxels = false,
                DepthSort = false,
                Tint = Color.White,
                FrustrumCull = false
            };
        }
    }

}