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
    [JsonObject(IsReference = true)]
    public class ParticleEffect
    {
        public List<ParticleEmitter> Emitters { get; set; }

        public ParticleEffect()
        {
            Emitters = new List<ParticleEmitter>();
        }

        

        public void Trigger(int num, Vector3 position, Color tint)
        {
            for (int i = 0; i < num; i++)
            {
                Emitters[MathFunctions.Random.Next(Emitters.Count)].Trigger(1, position, tint);
            }
        }
        
    }

    /// <summary>
    /// This class manages a set of particle effects, and allows them to be triggered
    /// at locations in 3D space.
    /// </summary>
    [JsonObject(IsReference =  true)]
    public class ParticleManager
    {
        public Dictionary<string, ParticleEffect> Effects { get; set; }
        [JsonIgnore]
        public ComponentManager Components { get; set; }

        public ParticleManager()
        {
            
        }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Components = DwarfGame.World.ComponentManager;
        }
        

        public ParticleManager(ComponentManager components)
        {
            Components = components;
            Effects = new Dictionary<string, ParticleEffect>();
            components.ParticleManager = this;
        }

        public void Trigger(string emitter, Vector3 position, Color tint, int num)
        {
            Effects[emitter].Trigger(num, position, tint);
        }

        public void RegisterEffect(string name, params EmitterData[] data)
        {
            List<ParticleEmitter> emitters = new List<ParticleEmitter>();

            foreach (EmitterData emitter in data)
            {
                emitters.Add(new ParticleEmitter(Components, name, Components.RootComponent, Matrix.Identity, emitter)
                {
                    LightsWithVoxels = false,
                    DepthSort = false,
                    Tint = Color.White,
                    FrustrumCull = false
                });
            }
            Effects[name] = new ParticleEffect()
            {
                Emitters = emitters
            };
        }

        /// <summary>
        /// A library function which creates a "explosion" particle effect (bouncy particles)
        /// </summary>
        /// <param name="assetName">Particle texture name</param>
        /// <param name="name">Name of the effect</param>
        /// <returns>A particle emitter which behaves like an explosion.</returns>
        public ParticleEffect CreateGenericExplosion(string assetName, string name)
        {
            List<Point> frm = new List<Point>
            {
                new Point(0, 0)
            };
            Texture2D tex = TextureManager.GetTexture(assetName);
            EmitterData testData = new EmitterData
            {
                Animation = new Animation(GameState.Game.GraphicsDevice, new SpriteSheet(assetName), assetName, tex.Width, tex.Height, frm, true, Color.White, 1.0f, 1.0f, 1.0f, false),
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
            return Effects[name];
        }

        public static EmitterData CreateExplosionLike(string name, SpriteSheet sheet, Point frame, BlendState state)
        {
            Texture2D tex = TextureManager.GetTexture(sheet.AssetName);
            EmitterData data = new EmitterData
            {
                Animation = new Animation(GameState.Game.GraphicsDevice, sheet, name, sheet.FrameWidth, sheet.FrameHeight, new List<Point>() { frame }, true, Color.White, 1.0f, 1.0f, 1.0f, false),
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
                Sleeps = true,
                Damping = 0.1f
            };

            return data;
        }

        /// <summary>
        /// Creates a generic particle effect which is like a "puff" (cloudy particles which float)
        /// </summary>
        public static EmitterData CreatePuffLike(string name, SpriteSheet sheet, Point frame, BlendState state)
        {
            Texture2D tex = TextureManager.GetTexture(sheet.AssetName);
            EmitterData data = new EmitterData
            {
                Animation = new Animation(GameState.Game.GraphicsDevice, sheet, name, sheet.FrameWidth, sheet.FrameHeight, new List<Point>(){frame}, true, Color.White, 1.0f, 1.0f, 1.0f, false),
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
                Texture = TextureManager.GetTexture(sheet.AssetName),
                Blend = state
            };

            return data;
        }
    }

}