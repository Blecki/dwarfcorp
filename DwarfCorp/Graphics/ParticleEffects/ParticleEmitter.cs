using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Scale;
        public float Angle;
        public float AngularVelocity;
        public float LifeRemaining;
        public Color LightRamp;
        public InstanceData InstanceData;
        public float TimeAlive;
        public int Frame;
        public Vector3 Direction;
        public Color Tint;
    }

    public class EmitterData : ICloneable
    {
        public enum ParticleBlend
        {
            NonPremultiplied,
            Additive,
            AlphaBlend,
            Opaque
        }
        public int MaxParticles;
        public int ParticlesPerFrame;
        public bool ReleaseOnce;
        public float EmissionFrequency;
        public Vector3 ConstantAccel;
        public float MinScale;
        public float MaxScale;
        public float GrowthSpeed;
        public float Damping = 0.5f;
        public float MinAngle;
        public float MaxAngle;
        public float MinAngular;
        public float MaxAngular;
        public float AngularDamping;
        public float LinearDamping;
        public SpriteSheet SpriteSheet;
        public Animation Animation;
        public float EmissionRadius;
        public float EmissionSpeed;
        public float ParticleDecay;
        public bool CollidesWorld = false;
        public bool Sleeps = false;
        public bool HasLighting = true;
        public bool RotatesWithVelocity = false;
        public ParticleBlend Blend = ParticleBlend.NonPremultiplied;
        public string SpatterType = null;
        public bool FixedRotation = false;

        [JsonIgnore]
        public BlendState BlendMode
        {
            get
            {
                switch (Blend)
                {
                    case ParticleBlend.Additive:
                        return BlendState.Additive;
                    case ParticleBlend.NonPremultiplied:
                        return BlendState.NonPremultiplied;
                    case ParticleBlend.Opaque:
                        return BlendState.Opaque;
                    case ParticleBlend.AlphaBlend:
                        return BlendState.AlphaBlend;
                }
                return BlendState.Opaque;
            }
        }

        public bool EmitsLight = false;
        public bool UseManualControl = false;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    /// <summary>
    /// This component manages a set of particles, and can emit them at certain locations. The particle manager keeps track of a small set of these.
    /// </summary>
    [JsonObject(IsReference =  true)]
    public class ParticleEmitter
    {
        [JsonIgnore]
        public List<FixedInstanceArray> Sprites { get; set; }
        private int maxParticles = 0;
        private AnimationPlayer AnimPlayer = new AnimationPlayer();

        public int MaxParticles
        {
            get { return maxParticles; }
            set
            {
                if(Sprites != null) {foreach(var sprites in Sprites) {sprites.SetNumInstances(MaxParticles);}}
                maxParticles = value;
            }
        }

        public List<Particle> Particles { get; set; }
        public EmitterData Data { get; set; }
        public Timer TriggerTimer { get; set; }
        private static Camera _camera = null;

        public static void Cleanup()
        {
            _camera = null;
        }

        public static Matrix MatrixFromParticle(EmitterData data, Particle particle)
        {
            if (!data.FixedRotation)
            {
                Matrix rot = Matrix.CreateRotationZ(particle.Angle);
                Matrix bill = Matrix.CreateBillboard(particle.Position, _camera.Position, _camera.UpVector, null);
                Matrix noTransBill = bill;
                noTransBill.Translation = Vector3.Zero;

                Matrix worldRot = Matrix.CreateScale(particle.Scale) * rot * noTransBill;
                worldRot.Translation = bill.Translation;
                return worldRot;
            }
            else
            {
                Vector3 up = Math.Abs(Vector3.Dot(particle.Direction, Vector3.Up)) > 0.9 ? Vector3.UnitZ : Vector3.Up;
                Matrix toReturn = Matrix.CreateLookAt(Vector3.Zero, particle.Direction, up);
                toReturn.Translation = particle.Position;
                return toReturn;
            }
        }

        public ParticleEmitter(ComponentManager manager, string name, Matrix localTransform, EmitterData emitterData) 
        {
            Particles = new List<Particle>();

            if(emitterData == null)
            {
                return;
            }
            Data = emitterData;
            maxParticles = Data.MaxParticles;
            Sprites = new List<FixedInstanceArray>();
            for (var t = 0; t < Data.Animation.GetFrameCount(); ++t)
            {
                var primitive = new BillboardPrimitive();
                primitive.SetFrame(Data.SpriteSheet, Data.SpriteSheet.GetTileRectangle(Data.Animation.Frames[t]), 1.0f, 1.0f, Color.White, Data.Animation.Tint);
                Sprites.Add(new FixedInstanceArray(name, primitive,
                    Data.SpriteSheet.AssetName,
                    Data.MaxParticles, Data.BlendMode));
            }
            AnimPlayer.Play(Data.Animation);

            TriggerTimer = new Timer(Data.EmissionFrequency, Data.ReleaseOnce);
        }

        public float Rand(float min, float max)
        {
            return (float) (MathFunctions.Random.NextDouble() * (max - min) + min);
        }

        public Vector3 RandVec(float scale)
        {
            return new Vector3(Rand(-scale, scale) * 0.5f, Rand(-scale, scale) * 0.5f, Rand(-scale, scale) * 0.5f);
        }

        public void Trigger(int num, Vector3 origin, Color tint)
        {
            lock (Particles)
            {
                for (int i = 0; i < num; i++)
                {
                    if (Particles.Count < Data.MaxParticles)
                    {
                        bool sampleFound = false;

                        Vector3 sample = new Vector3(99999, 99999, 9999);

                        while (!sampleFound)
                        {
                            sample = RandVec(Data.EmissionRadius);

                            if (sample.Length() < Data.EmissionRadius)
                            {
                                sampleFound = true;
                            }
                        }


                        Vector3 position = sample + origin;
                        Vector3 velocity = (sample);
                        velocity.Normalize();
                        velocity *= Data.EmissionSpeed;
                        CreateParticle(position, velocity, tint);
                    }
                }
            }
        }

        public static int CompareZDepth(Particle A, Particle B)
        {
            if(A == B)
            {
                return 0;
            }

            if((_camera.Position - A.Position).LengthSquared() < (_camera.Position - B.Position).LengthSquared())
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        public void Render(Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect)
        {
            foreach (var sprites in Sprites)
            {
                sprites.Render(graphicsDevice, effect, camera);
            }
        }


        public Particle CreateParticle(Vector3 pos, Vector3 velocity, Color tint, Vector3 direction)
        {
            Particle toAdd = new Particle
            {
                Velocity = velocity,
                Scale = Rand(Data.MinScale, Data.MaxScale),
                Angle = Rand(Data.MinAngle, Data.MaxAngle),
                AngularVelocity = Rand(Data.MinAngular, Data.MaxAngular),
                LifeRemaining = 1.0f,
                Tint = tint, // Tint is not actually used!
                Position = pos,
                TimeAlive = 0.0f,
                Direction = direction
            };
            toAdd.InstanceData = new InstanceData(Matrix.Identity, toAdd.LightRamp, true);

            lock (Particles)
            {
                Particles.Add(toAdd);
            }

            if (toAdd.InstanceData != null)
            {
                Sprites[0].Add(toAdd.InstanceData);
            }
            return toAdd;
        }

        public Particle CreateParticle(Vector3 pos, Vector3 velocity, Color tint)
        {
            return CreateParticle(pos, velocity, tint, velocity);
        }

        public void RemoveParticle(Particle p)
        {
            p.LifeRemaining = -1;
        }

        public void Update(ParticleManager manager, DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            ParticleEmitter._camera = camera;

            List<Particle> toRemove = new List<Particle>();

            TriggerTimer.Update(gameTime);
            if(TriggerTimer.HasTriggered && Data.ParticlesPerFrame > 0)
            {
                Trigger(Data.ParticlesPerFrame, Vector3.Zero, new Color(255, 255, 0));
            }


            bool particlePhysics = GameSettings.Current.ParticlePhysics;

            lock (Particles)
            {
                foreach (var p in Particles)
                {
                    float vel = p.Velocity.LengthSquared();
                    if (Data.EmitsLight && p.Scale > 0.1f)
                    {
                        DynamicLight.TempLights.Add(new DynamicLight(10.0f, 255.0f, false) { Position = p.Position });
                    }
                    p.Position += p.Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

                    if (Data.RotatesWithVelocity)
                    {
                        Vector3 cameraVel = camera.Project(p.Velocity + camera.Position);
                        float projectionX = cameraVel.X;
                        float projectionY = cameraVel.Y;

                        p.Angle = (float)Math.Atan2(projectionY, projectionX);
                    }
                    else
                    {
                        p.Angle += (float)(p.AngularVelocity * gameTime.ElapsedGameTime.TotalSeconds);
                    }
                    if (!Data.Sleeps || vel > 0.01f)
                        p.Velocity += Data.ConstantAccel * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    p.Velocity *= Data.LinearDamping;
                    p.AngularVelocity *= Data.AngularDamping;


                    if (!Data.UseManualControl)
                    {
                        p.LifeRemaining -= Data.ParticleDecay * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    }
                    else if (p.TimeAlive > 60)
                    {
                        p.LifeRemaining = 0;
                    }

                    p.Scale += Data.GrowthSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

                    p.Scale = Math.Max(p.Scale, 0.0f);

                    var v = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(p.Position));

                    if (Data.HasLighting)
                    {
                        if (v.IsValid)
                            p.LightRamp = new Color(v.Sunlight ? 255 : 0, 255, 0);
                    }
                    else
                    {
                        p.LightRamp = new Color(255, 255, 0);
                    }

                    if (Data.CollidesWorld && particlePhysics && vel > 0.2f)
                    {
                        if (v.IsValid && !v.IsEmpty)
                        {
                            BoundingBox b = new BoundingBox(p.Position - Vector3.One * p.Scale * 0.5f, p.Position + Vector3.One * p.Scale * 0.5f);
                            BoundingBox vBox = v.GetBoundingBox();
                            var contact = new Collision.Contact();
                            if (Collision.TestStaticAABBAABB(b, vBox, ref contact))
                            {
                                p.Position += contact.NEnter * contact.Penetration;
                                Vector3 newVelocity = Vector3.Reflect(p.Velocity, -contact.NEnter);
                                p.Velocity = newVelocity * Data.Damping;
                                p.AngularVelocity *= 0.5f;
                                if (Data.Sleeps)
                                {
                                    p.Velocity = Vector3.Zero;
                                    p.AngularVelocity = 0.0f;
                                    vel = 0.0f;
                                }
                                if (!String.IsNullOrEmpty(Data.SpatterType))
                                {
                                    var above = VoxelHelpers.GetVoxelAbove(v);
                                    if (!above.IsValid || above.IsEmpty)
                                    {
                                        float x = MathFunctions.Clamp(p.Position.X, vBox.Min.X + 0.1f, vBox.Max.X - 0.1f);
                                        float z = MathFunctions.Clamp(p.Position.Z, vBox.Min.Z + 0.1f, vBox.Max.Z - 0.1f);
                                        manager.Create(Data.SpatterType,
                                            VertexNoise.Warp(new Vector3(x, v.RampType == RampType.None ? v.WorldPosition.Y + 1.02f : v.WorldPosition.Y + 0.6f, z)), Vector3.Zero, Color.White, Vector3.Up);
                                    }
                                    else
                                    {
                                        manager.Create(Data.SpatterType, p.Position - contact.NEnter * contact.Penetration * 0.95f, Vector3.Zero, Color.White, contact.NEnter);
                                    }
                                    p.LifeRemaining = -1.0f;
                                }

                            }
                        }
                    }

                    if (p.LifeRemaining <= 0)
                    {
                        if (p.InstanceData != null)
                        {
                            p.InstanceData.ShouldDraw = false;
                            p.InstanceData.Transform = Matrix.CreateTranslation(camera.Position + new Vector3(-1000, -1000, -1000));
                            Sprites[p.Frame].Remove(p.InstanceData);
                        }

                        toRemove.Add(p);
                    }

                    else
                    if (p.InstanceData != null)
                    {
                        p.TimeAlive += (float)gameTime.ElapsedGameTime.TotalSeconds + MathFunctions.Rand() * 0.01f;
                        int prevFrame = p.Frame;
                        int newFrame = AnimPlayer.GetFrame(p.TimeAlive);
                        if (vel < 0.2f && Data.Sleeps)
                        {
                            newFrame = prevFrame;
                        }
                        if (newFrame != prevFrame)
                        {
                            p.Frame = newFrame;
                            if (Sprites.Count > 0)
                            {
                                Sprites[prevFrame].Remove(p.InstanceData);
                                Sprites[newFrame].Add(p.InstanceData);
                            }
                            if (/*!Data.Animation.Loops && */p.Frame == Data.Animation.Frames.Count - 1)
                            {
                                p.LifeRemaining *= 0.1f;
                            }
                        }
                        p.InstanceData.ShouldDraw = true;
                        p.InstanceData.Transform = MatrixFromParticle(Data, p);
                        p.InstanceData.LightRamp = p.LightRamp;
                    }
                }

                foreach (Particle p in toRemove)
                {
                    Particles.Remove(p);
                }
            }

            foreach (var sprites in Sprites)
            {
                sprites.Update(gameTime, camera, GameState.Game.GraphicsDevice, chunks.World.Renderer.PersistentSettings.MaxViewingLevel);
            }
        }
    }

}
