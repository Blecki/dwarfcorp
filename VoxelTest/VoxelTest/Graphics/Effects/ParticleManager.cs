using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
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
        [JsonIgnore]
        public ComponentManager Components { get; set; }

        public ParticleManager()
        {
            
        }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Components = PlayState.ComponentManager;
        }
        

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

        /// <summary>
        /// A library function which creates a "explosion" particle effect (bouncy particles)
        /// TODO: Move this to a different file
        /// </summary>
        /// <param name="assetName">Particle texture name</param>
        /// <param name="name">Name of the effect</param>
        /// <returns>A particle emitter which behaves like an explosion.</returns>
        public ParticleEmitter CreateGenericExplosion(string assetName, string name)
        {
            List<Point> frm = new List<Point>
            {
                new Point(0, 0)
            };
            Texture2D tex = TextureManager.GetTexture(assetName);
            EmitterData testData = new EmitterData
            {
                Animation = new Animation(GameState.Game.GraphicsDevice, tex, assetName, tex.Width, tex.Height, frm, true, Color.White, 1.0f, 1.0f, 1.0f, false),
                ConstantAccel = new Vector3(0, -10, 0),
                LinearDamping = 0.9999f,
                AngularDamping = 0.9f,
                EmissionFrequency = 50.0f,
                EmissionRadius = 1.0f,
                EmissionSpeed = 5.0f,
                GrowthSpeed = -0.0f,
                MaxAngle = 3.14159f,
                MinAngle = 0.0f,
                MaxParticles = 1000,
                MaxScale = 0.2f,
                MinScale = 0.1f,
                MinAngular = -5.0f,
                MaxAngular = 5.0f,
                ParticleDecay = 0.5f,
                ParticlesPerFrame = 0,
                ReleaseOnce = true,
                Texture = tex,
                CollidesWorld = true,
                Sleeps = true
            };

            RegisterEffect(name, testData);
            return Emitters[name];
        }

        /// <summary>
        /// Creates a generic particle effect which is like a "puff" (cloudy particles which float)
        /// </summary>
        /// <param name="name">Name of the effect</param>
        /// <param name="assetName">Texture asset to use</param>
        /// <param name="state">Blend mode of the particles (alpha or additive)</param>
        /// <returns>A puff emitter</returns>
        public static EmitterData CreatePuffLike(string name, string assetName, BlendState state)
        {
            List<Point> frm = new List<Point>
            {
                new Point(0, 0)
            };
            Texture2D tex = TextureManager.GetTexture(assetName);
            EmitterData data = new EmitterData
            {

                Animation = new Animation(GameState.Game.GraphicsDevice, tex, name, tex.Width, tex.Height, frm, true, Color.White, 1.0f, 1.0f, 1.0f, false),
                ConstantAccel = new Vector3(0, 3, 0),
                LinearDamping = 0.9f,
                AngularDamping = 0.99f,
                EmissionFrequency = 20.0f,
                EmissionRadius = 1.0f,
                EmissionSpeed = 2.0f,
                GrowthSpeed = -0.6f,
                MaxAngle = 3.14159f,
                MinAngle = 0.0f,
                MaxParticles = 1000,
                MaxScale = 1.0f,
                MinScale = 0.1f,
                MinAngular = -5.0f,
                MaxAngular = 5.0f,
                ParticleDecay = 0.8f,
                ParticlesPerFrame = 0,
                ReleaseOnce = true,
                Texture = TextureManager.GetTexture(assetName),
                Blend = state
            };

            return data;
        }
    }

}