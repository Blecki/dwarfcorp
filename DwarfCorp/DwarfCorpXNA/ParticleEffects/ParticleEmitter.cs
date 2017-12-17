// ParticleEmitter.cs
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
        public Color Tint;
        public InstanceData InstanceData;
        public float TimeAlive;
        public int Frame;
    }

    [JsonObject(IsReference = true)]
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
        public Animation Animation;
        public float EmissionRadius;
        public float EmissionSpeed;
        public float ParticleDecay;
        public bool CollidesWorld = false;
        public bool Sleeps = false;
        public bool HasLighting = true;
        public bool RotatesWithVelocity = false;
        public ParticleBlend Blend = ParticleBlend.NonPremultiplied;

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

        public EmitterData Clone(SpriteSheet sheet, Point frame)
        {
            EmitterData toReturn = Clone() as EmitterData;
            if (toReturn == null) return null;
            if (toReturn.Animation != null)
            {
                toReturn.Animation = new Animation(GameState.Game.GraphicsDevice,
                    new SpriteSheet(sheet.AssetName), sheet.AssetName, sheet.FrameWidth, sheet.FrameHeight,
                    new List<Point>() {frame}, 
                    //true, 
                    Color.White, 1.0f, 1.0f, 1.0f, false);
            }
            return toReturn;
        }
    }

    /// <summary>
    /// This component manages a set of particles, and can emit them at certain locations. The particle manager keeps track of a small set of these.
    /// </summary>
    [JsonObject(IsReference =  true)]
    public class ParticleEmitter : Tinter, IUpdateableComponent, IRenderableComponent
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
        
        public static Matrix MatrixFromParticle(Particle particle)
        {
            Matrix rot = Matrix.CreateRotationZ(particle.Angle);
            Matrix bill = Matrix.CreateBillboard(particle.Position, _camera.Position, _camera.UpVector, null);
            Matrix noTransBill = bill;
            noTransBill.Translation = Vector3.Zero;

            Matrix worldRot = Matrix.CreateScale(particle.Scale) * rot * noTransBill;
            worldRot.Translation = bill.Translation;
            return worldRot;
        }

        public ParticleEmitter(GraphicsDevice Device, ComponentManager manager, string name, Matrix localTransform, EmitterData emitterData) :
            base(manager, name, localTransform, Vector3.Zero, Vector3.Zero, false)
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
                Data.Animation.UpdatePrimitive(primitive, t);
                Sprites.Add(new FixedInstanceArray(Name, primitive,
                    Data.Animation.SpriteSheet.GetTexture(),
                    Data.MaxParticles, Data.BlendMode));
            }
            AnimPlayer.Play(Data.Animation);

            TriggerTimer = new Timer(Data.EmissionFrequency, Data.ReleaseOnce);

            SetFlag(Flag.ShouldSerialize, false);
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
            for(int i = 0; i < num; i++)
            {
                if(Particles.Count < Data.MaxParticles)
                {
                    bool sampleFound = false;

                    Vector3 sample = new Vector3(99999, 99999, 9999);

                    while(!sampleFound)
                    {
                        sample = RandVec(Data.EmissionRadius);

                        if(sample.Length() < Data.EmissionRadius)
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

        public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool Ignored)
        {
            ApplyTintingToEffect(effect);
            foreach (var sprites in Sprites)
            {
                sprites.Render(graphicsDevice, effect, camera);
            }
        }

        public Particle CreateParticle(Vector3 pos, Vector3 velocity, Color tint)
        {
            Particle toAdd = new Particle
            {
                Velocity = velocity,
                Scale = Rand(Data.MinScale, Data.MaxScale),
                Angle = Rand(Data.MinAngle, Data.MaxAngle),
                AngularVelocity = Rand(Data.MinAngular, Data.MaxAngular),
                LifeRemaining = 1.0f,
                Tint = tint,
                Position = pos,
                TimeAlive = 0.0f
            };
            toAdd.InstanceData = new InstanceData(Matrix.Identity, toAdd.Tint, true);

            Particles.Add(toAdd);

            if (toAdd.InstanceData != null)
            {
                Sprites[0].Add(toAdd.InstanceData);
            }
            return toAdd;
        }

        public void RemoveParticle(Particle p)
        {
            p.LifeRemaining = -1;
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            ParticleEmitter._camera = camera;

            List<Particle> toRemove = new List<Particle>();

            TriggerTimer.Update(gameTime);
            if(TriggerTimer.HasTriggered && Data.ParticlesPerFrame > 0)
            {
                Trigger(Data.ParticlesPerFrame, Vector3.Zero, Tint);
            }


            bool particlePhysics = GameSettings.Default.ParticlePhysics;

            foreach (Particle p in Particles)
            {
                float vel = p.Velocity.LengthSquared();
                if(!Data.Sleeps || vel > 0.2f)
                {
                    if (Data.EmitsLight && p.Scale > 0.1f)
                    {
                        DynamicLight.TempLights.Add(new DynamicLight(10.0f, 255.0f, false) { Position = p.Position});
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
                    p.Velocity += Data.ConstantAccel * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    p.Velocity *= Data.LinearDamping;
                    p.AngularVelocity *= Data.AngularDamping;
                }
                else if (Data.Sleeps && vel < 0.2f)
                {
                    p.Velocity = Vector3.Zero;
                }


                if (!Data.UseManualControl)
                {
                    p.LifeRemaining -= Data.ParticleDecay*(float) gameTime.ElapsedGameTime.TotalSeconds;
                }

                p.Scale += Data.GrowthSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

                p.Scale = Math.Max(p.Scale, 0.0f);

                var v = new VoxelHandle(chunks.ChunkData,
                        GlobalVoxelCoordinate.FromVector3(p.Position));

                if (Data.HasLighting)
                {
                    if (v.IsValid && v.IsEmpty)
                        p.Tint = new Color(v.SunColor, 255, 0);
                }

                if(Data.CollidesWorld && particlePhysics && vel > 0.2f)
                {
                    BoundingBox b = new BoundingBox(p.Position - Vector3.One * p.Scale * 0.5f, p.Position + Vector3.One * p.Scale * 0.5f);
                    if(v.IsValid && !v.IsEmpty)
                    {
                        Physics.Contact contact = new Physics.Contact();
                        if(Physics.TestStaticAABBAABB(b, v.GetBoundingBox(), ref contact))
                        {
                            p.Position += contact.NEnter * contact.Penetration;
                            Vector3 newVelocity = Vector3.Reflect(p.Velocity, -contact.NEnter);
                            p.Velocity = newVelocity * Data.Damping;
                            p.AngularVelocity *= 0.5f;
                        }
                    }
                }

                if(p.LifeRemaining < 0)
                {
                    if(p.InstanceData != null)
                    {
                        p.InstanceData.ShouldDraw = false;
                        p.InstanceData.Transform = Matrix.CreateTranslation(camera.Position + new Vector3(-1000, -1000, -1000));
                        Sprites[p.Frame].Remove(p.InstanceData);
                    }

                    toRemove.Add(p);
                }

                else if(p.InstanceData != null)
                {
                    p.TimeAlive += (float)gameTime.ElapsedGameTime.TotalSeconds + MathFunctions.Rand() * 0.01f;
                    int prevFrame = p.Frame;
                    int newFrame = AnimPlayer.GetFrame(p.TimeAlive);
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
                    p.InstanceData.Transform = MatrixFromParticle(p);
                    p.InstanceData.Color = p.Tint;
                }
            }

            foreach(Particle p in toRemove)
            {
                Particles.Remove(p);
            }


            foreach (var sprites in Sprites)
            {
                sprites.Update(gameTime, camera, chunks.Graphics, chunks.ChunkData.MaxViewingLevel);
            }
            if (Particles.Count > 0)
                base.Update(gameTime, chunks, camera);
        }
    }

}
