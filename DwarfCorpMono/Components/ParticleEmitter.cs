using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        public Color Tint;
        public InstanceData InstanceData;
    }

    public class EmitterData
    {
        public int MaxParticles;
        public int ParticlesPerFrame;
        public bool ReleaseOnce;
        public float EmissionFrequency;
        public Vector3 ConstantAccel;
        public float MinScale;
        public float MaxScale;
        public float GrowthSpeed;
        public float Damping;
        public float MinAngle;
        public float MaxAngle;
        public float MinAngular;
        public float MaxAngular;
        public float AngularDamping;
        public float LinearDamping;
        public Texture2D Texture;
        public Animation Animation;
        public float EmissionRadius;
        public float EmissionSpeed;
        public float ParticleDecay;
        public bool CollidesWorld = false;
        public bool Sleeps = false;
        public BlendState Blend = BlendState.AlphaBlend;
    }

    public class ParticleEmitter : TintableComponent
    {
        public FixedInstanceArray Sprites { get; set; }
        private int maxParticles = 0;
        public int MaxParticles { get { return maxParticles; } set { Sprites.SetNumInstances(MaxParticles); maxParticles = value; } }
        public List<Particle> Particles { get; set; }
        public EmitterData Data { get; set; }
        public Timer TriggerTimer { get; set; }
        private static Camera m_camera = null;

        public static Matrix MatrixFromParticle(Particle particle, Camera camera)
        {
            Matrix rot = Matrix.CreateRotationZ(particle.Angle);
            Matrix bill = Matrix.CreateBillboard(particle.Position, camera.Position, camera.UpVector, null);
            Matrix noTransBill = bill;
            noTransBill.Translation = Vector3.Zero;

            Matrix worldRot = Matrix.CreateScale(particle.Scale) * rot * noTransBill;
            worldRot.Translation = bill.Translation;
            return worldRot;
        }

        public ParticleEmitter(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, EmitterData emitterData) :
            base(manager, name, parent, localTransform, Vector3.Zero, Vector3.Zero, false)
        {
            Particles = new List<Particle>();
            Data = emitterData;
            maxParticles = Data.MaxParticles;
            Sprites = new FixedInstanceArray(name, Data.Animation.Primitives[0].VertexBuffer, emitterData.Texture, Data.MaxParticles, Data.Blend);
            Data.Animation.Play();
            
            TriggerTimer = new Timer(Data.EmissionFrequency, Data.ReleaseOnce);
        }

        public float rand(float min, float max)
        {
            return (float)(PlayState.random.NextDouble() * (max - min) + min);
        }

        public Vector3 randVec(float scale)
        {
            return new Vector3(rand(-scale, scale) * 0.5f, rand(-scale, scale) * 0.5f, rand(-scale, scale) * 0.5f);
        }

        public void Trigger(int num, Vector3 origin, Color tint)
        {
            for (int i = 0; i < num; i++)
            {
                if (Particles.Count < Data.MaxParticles)
                {
                    Particle toAdd = new Particle();

                    bool sampleFound = false;

                    Vector3 sample = new Vector3(99999, 99999, 9999);
                    
                    while (!sampleFound)
                    {
                        sample = randVec(Data.EmissionRadius);

                        if (sample.Length() < Data.EmissionRadius)
                        {
                            sampleFound = true;
                        }
                    }

                    toAdd.Position = sample + origin;
                    toAdd.Velocity = randVec(Data.EmissionSpeed);
                    
                    toAdd.Scale = rand(Data.MinScale, Data.MaxScale);
                    toAdd.Angle = rand(Data.MinAngle, Data.MaxAngle);
                    toAdd.AngularVelocity = rand(Data.MinAngular, Data.MaxAngular);
                    toAdd.LifeRemaining = 1.0f;
                    toAdd.Tint = Color.White;
                    toAdd.InstanceData = new InstanceData(Matrix.Identity, toAdd.Tint, true);

                    Particles.Add(toAdd);

                    if (toAdd.InstanceData != null)
                    {
                        Sprites.Add(toAdd.InstanceData);
                    }
                }
            }

        }

        public static int CompareZDepth(Particle A, Particle B)
        {
            if (A == B)
            {
                return 0;
            }

            if ((m_camera.Position - A.Position).LengthSquared() < (m_camera.Position - B.Position).LengthSquared())
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        public override void Render(GameTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {
            Sprites.Render(graphicsDevice, effect, camera, !renderingForWater);
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            m_camera = camera;
    
            List<Particle> toRemove = new List<Particle>();

            if (TriggerTimer.HasTriggered)
            {
                Trigger(Data.ParticlesPerFrame, Vector3.Zero, Tint);
            }
            else
            {
                TriggerTimer.Update(gameTime);
            }


            bool particlePhysics = GameSettings.Default.ParticlePhysics;

            for(int i = 0; i < Particles.Count; i++)
            {

                Particle p = Particles[i];


                if (!Data.Sleeps || p.Velocity.LengthSquared() > 0.1f)
                {
                    p.Position += p.Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    p.Angle += (float)(p.AngularVelocity * gameTime.ElapsedGameTime.TotalSeconds);
                    p.Velocity += Data.ConstantAccel * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    p.Velocity *= Data.LinearDamping;
                    p.AngularVelocity *= Data.AngularDamping;
                }
                
                
                p.LifeRemaining -= Data.ParticleDecay * (float)gameTime.ElapsedGameTime.TotalSeconds ;
                p.Scale += Data.GrowthSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                
                p.Scale = Math.Max(p.Scale, 0.0f);


                if (Data.CollidesWorld && particlePhysics && p.Velocity.LengthSquared() > 0.1f)
                {
                    List<Voxel> voxels = chunks.GetNonNullVoxelsAtWorldLocation(p.Position, 0);
                    BoundingBox b = new BoundingBox(p.Position - Vector3.One * p.Scale * 0.5f, p.Position + Vector3.One * p.Scale * 0.5f);
                    foreach (Voxel v in voxels)
                    {
                        
                        PhysicsComponent.Contact contact = new PhysicsComponent.Contact();
                        if (PhysicsComponent.TestStaticAABBAABB(b, v.GetBoundingBox(), ref contact))
                        {

                            p.Position += contact.nEnter * contact.penetration;

                            Vector3 newVelocity = (contact.nEnter * Vector3.Dot(p.Velocity, contact.nEnter));
                            p.Velocity = (p.Velocity - newVelocity) * 0.5f;
                            p.AngularVelocity *= 0.5f;
                        }
                        
                    }
                }

                if (p.LifeRemaining < 0)
                {

                    if (p.InstanceData != null)
                    {
                        p.InstanceData.ShouldDraw = false;
                        p.InstanceData.Transform = Matrix.CreateTranslation(camera.Position + new Vector3(-1000, -1000, -1000));
                        Sprites.Remove(p.InstanceData);
                    }

                    toRemove.Add(p);
                }

                else if (p.InstanceData != null)
                {
                    p.InstanceData.ShouldDraw = true;
                    p.InstanceData.Transform = MatrixFromParticle(p, camera);
                    p.InstanceData.Color = p.Tint;
                }
            }

            foreach (Particle p in toRemove)
            {
                Particles.Remove(p);
            }


            Sprites.Update(gameTime, camera, chunks.Graphics);
            base.Update(gameTime, chunks, camera);
        }

    }
}
