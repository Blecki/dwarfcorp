// Projectile.cs
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
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Projectile : Physics
    {

        public Sprite Sprite { get; set; }
        public Sprite Sprite2 { get; set; }
        public ParticleTrigger HitParticles { get; set; }
        public Health.DamageAmount Damage { get; set; }
        public Body Target { get; set; }
        public float DamageRadius { get; set; }
        public Animation HitAnimation { get; set; }
        public Projectile()
        {
            
        }

        public Projectile(Vector3 position, Vector3 initialVelocity, Health.DamageAmount damage, float size, string asset, string hitParticles, string hitNoise, Body target) : 
            base("Projectile", WorldManager.ComponentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(size, size, size), Vector3.One, 1.0f, 1.0f, 1.0f, 1.0f, new Vector3(0, -10, 0) )
        {
            Target = target;
            HitAnimation = null;
            IsSleeping = false;
            Velocity = initialVelocity;
            Orientation = OrientMode.LookAt;
            AddToCollisionManager = false;
            CollideMode = Physics.CollisionMode.None;
            Sprite = new Sprite(WorldManager.ComponentManager, "Sprite", this, Matrix.CreateRotationY((float)Math.PI * 0.5f),
                new SpriteSheet(asset), false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            Sprite.SetSingleFrameAnimation(new Point(0, 0));
            Sprite2 = new Sprite(WorldManager.ComponentManager, "Sprite2", Sprite, Matrix.CreateRotationX((float)Math.PI * 0.5f),
                new SpriteSheet(asset), false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            Sprite2.SetSingleFrameAnimation(new Point(0, 0));

            Damage = damage;
            HitParticles = new ParticleTrigger(hitParticles, WorldManager.ComponentManager, "Hit Particles", this,
                Matrix.Identity, new Vector3(size * 0.5f, size * 0.5f, size * 0.5f), Vector3.Zero)
            {
                TriggerOnDeath = true,
                SoundToPlay = hitNoise,
                BoxTriggerTimes = 2
            };
            DamageRadius = (float)Math.Pow(size*4, 2);
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (Target != null && (Target.Position - Position).LengthSquared() < DamageRadius)
            {
                Health health = Target.GetComponent<Health>();

                if (health != null)
                {
                    health.Damage(Damage.Amount, Damage.DamageType);
                }

                if (Damage.DamageType == Health.DamageType.Fire)
                {
                    Flammable flammabe = Target.GetComponent<Flammable>();

                    if (flammabe != null)
                        flammabe.Heat += 50.0f;
                }

                Die();
            }
            else if (Target != null && (Target.Position.Y - Position.Y) > 1 && Velocity.Y < 0)
            {
                Die();
            }

            base.Update(gameTime, chunks, camera);
        }

        public override void Die()
        {
            if (HitAnimation != null)
            {
                HitAnimation.Reset();
                HitAnimation.Play();
                if (Target != null)
                {
                    Vector3 camvelocity0 = GameState.Game.GraphicsDevice.Viewport.Project( Position,
                        WorldManager.Camera.ProjectionMatrix, WorldManager.Camera.ViewMatrix, Matrix.Identity);
                    Vector3 camvelocity1 = GameState.Game.GraphicsDevice.Viewport.Project(Position + Velocity,
                        WorldManager.Camera.ProjectionMatrix, WorldManager.Camera.ViewMatrix, Matrix.Identity);
                    IndicatorManager.DrawIndicator(HitAnimation, Target.Position,
                        HitAnimation.FrameHZ*HitAnimation.Frames.Count + 1.0f, 1.0f, Vector2.Zero, Color.White, camvelocity1.X - camvelocity0.X > 0);
                }
            }
            base.Die();
        }


        public override void OnTerrainCollision(Voxel vox)
        {
            if (Target == null || Target.IsDead)
            {
                Matrix transform = LocalTransform;
                transform.Translation -= Velocity;
                LocalTransform = transform;

                if (!IsDead)
                    Die();
            }

            base.OnTerrainCollision(vox);
        }

        
    }

    [JsonObject(IsReference = true)]
    public class FireballProjectile : Projectile
    {
        public FireballProjectile()
        {
            
        }

        public FireballProjectile(Vector3 position, Vector3 initialVelocity, Body target) :
            base(position, initialVelocity, new Health.DamageAmount() { Amount = 15.0f, DamageType = Health.DamageType.Fire }, 0.25f, ContentPaths.Particles.fireball, "flame", ContentPaths.Audio.fire, target)
        {
            HitAnimation = new Animation(ContentPaths.Effects.pierce, 32, 32, 0, 1, 2, 3);
            Sprite.LightsWithVoxels = false;
            Sprite2.LightsWithVoxels = false;
        }
    }

    [JsonObject(IsReference = true)]
    public class ArrowProjectile : Projectile
    {
        public ArrowProjectile()
        {
            
        }

        public ArrowProjectile(Vector3 position, Vector3 initialVelocity, Body target) :
            base(position, initialVelocity, new Health.DamageAmount() { Amount = 10.0f, DamageType = Health.DamageType.Slashing }, 0.25f, ContentPaths.Entities.Elf.Sprites.arrow, "puff", ContentPaths.Audio.hit, target)
        {
            HitAnimation = new Animation(ContentPaths.Effects.pierce, 32, 32, 0, 1, 2);
        }
    }

    [JsonObject(IsReference = true)]
    public class WebProjectile : Projectile
    {
        public WebProjectile()
        {

        }

        public WebProjectile(Vector3 position, Vector3 initialVelocity, Body target) :
            base(position, initialVelocity, new Health.DamageAmount() { Amount = 10.0f, DamageType = Health.DamageType.Acid }, 0.25f, ContentPaths.Entities.Animals.Spider.webshot, "puff", ContentPaths.Audio.whoosh, target)
        {
            HitAnimation = new Animation(ContentPaths.Entities.Animals.Spider.webstick, 32, 32, 0);
        }
    }

    [JsonObject(IsReference = true)]
    public class BulletProjectile : Projectile
    {
        public BulletProjectile()
        {

        }

        public BulletProjectile(Vector3 position, Vector3 initialVelocity, Body target) :
            base(position, initialVelocity, new Health.DamageAmount() { Amount = 30.0f, DamageType = Health.DamageType.Normal }, 0.25f, ContentPaths.Particles.stone_particle, "puff", ContentPaths.Audio.explode, target)
        {
            HitAnimation = new Animation(ContentPaths.Effects.explode, 32, 32, 0, 1, 2, 3, 4);
        }
    }
}
