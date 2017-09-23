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
    public class Projectile : Physics, IUpdateableComponent
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

        public Projectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, Health.DamageAmount damage, float size, string asset, string hitParticles, string hitNoise, Body target, bool animated = false, bool singleSprite = false) :
            base(manager, "Projectile", Matrix.CreateTranslation(position), new Vector3(size, size, size), Vector3.One, 1.0f, 1.0f, 1.0f, 1.0f, new Vector3(0, -10, 0))
        {
            this.AllowPhysicsSleep = false; 
            Target = target;
            HitAnimation = null;
            IsSleeping = false;
            Velocity = initialVelocity;
            Orientation = OrientMode.LookAt;
            AddToCollisionManager = false;
            CollideMode = Physics.CollisionMode.None;
            var spriteSheet = new SpriteSheet(asset);

            if (animated)
            {
                spriteSheet.FrameWidth = Math.Min(spriteSheet.FrameWidth, spriteSheet.FrameHeight);
                spriteSheet.FrameHeight = spriteSheet.FrameWidth;
            }

            Sprite = AddChild(new Sprite(Manager, "Sprite", Matrix.CreateRotationY((float)Math.PI * 0.5f),
                spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            }) as Sprite;

            if (animated)
            {
               Sprite.SetSimpleAnimation();
               Sprite.CurrentAnimation.Play();
            }
            else
            {
                Sprite.SetSingleFrameAnimation();   
            }

            if (singleSprite)
            {
                this.Sprite.OrientationType = Sprite.OrientMode.Spherical;
            }

            if (!singleSprite)
            {
                Sprite2 = Sprite.AddChild(new Sprite(Manager, "Sprite2",
                    Matrix.CreateRotationX((float)Math.PI * 0.5f),
                    spriteSheet, false)
                {
                    OrientationType = Sprite.OrientMode.Fixed
                }) as Sprite;

                if (animated)
                {
                    Sprite2.SetSimpleAnimation();
                }
                else
                {
                    Sprite2.SetSingleFrameAnimation();
                }
            }
            Damage = damage;
            HitParticles = AddChild(new ParticleTrigger(hitParticles, manager, "Hit Particles",
                Matrix.Identity, new Vector3(size * 0.5f, size * 0.5f, size * 0.5f), Vector3.Zero)
            {
                TriggerOnDeath = true,
                SoundToPlay = hitNoise,
                BoxTriggerTimes = 2
            }) as ParticleTrigger;
            DamageRadius = (float)Math.Pow(size*4, 2);
        }

        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (Target != null && (Target.Position - LocalPosition).LengthSquared() < DamageRadius)
            {
                Health health = Target.GetRoot().GetComponent<Health>();

                if (health != null)
                {
                    health.Damage(Damage.Amount, Damage.DamageType);
                    Vector3 knock = (Target.Position - Position);
                    knock.Normalize();
                    knock *= 0.2f;
                    if (Target.AnimationQueue.Count == 0)
                        Target.AnimationQueue.Add(new KnockbackAnimation(0.15f, Target.LocalTransform, knock));
                }

                if (Damage.DamageType == Health.DamageType.Fire)
                {
                    Flammable flammabe = Target.GetRoot().GetComponent<Flammable>();

                    if (flammabe != null)
                        flammabe.Heat += 50.0f;
                }

                Die();
            }
            else if (Target != null && (Target.Position.Y - LocalPosition.Y) > 1 && Velocity.Y < 0)
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
                        Manager.World.Camera.ProjectionMatrix, Manager.World.Camera.ViewMatrix, Matrix.Identity);
                    Vector3 camvelocity1 = GameState.Game.GraphicsDevice.Viewport.Project(Position + Velocity,
                        Manager.World.Camera.ProjectionMatrix, Manager.World.Camera.ViewMatrix, Matrix.Identity);
                    IndicatorManager.DrawIndicator(HitAnimation, Target.Position,
                        HitAnimation.FrameHZ*HitAnimation.Frames.Count + 1.0f, 1.0f, Vector2.Zero, Color.White, camvelocity1.X - camvelocity0.X > 0);
                }
            }
            base.Die();
        }


        public override void OnTerrainCollision(VoxelHandle vox)
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

        public FireballProjectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, Body target) :
            base(manager, position, initialVelocity, new Health.DamageAmount() { Amount = 15.0f, DamageType = Health.DamageType.Fire }, 0.25f, ContentPaths.Particles.fireball, "flame", ContentPaths.Audio.Oscar.sfx_ic_demon_fire_hit_1, target)
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

        public ArrowProjectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, Body target) :
            base(manager, position, initialVelocity, new Health.DamageAmount() { Amount = 10.0f, DamageType = Health.DamageType.Slashing }, 0.25f, ContentPaths.Entities.Elf.Sprites.arrow, "puff", ContentPaths.Audio.Oscar.sfx_ic_elf_arrow_hit, target)
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

        public WebProjectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, Body target) :
            base(manager, position, initialVelocity, new Health.DamageAmount() { Amount = 10.0f, DamageType = Health.DamageType.Acid }, 0.25f, ContentPaths.Entities.Animals.Spider.webshot, "puff", ContentPaths.Audio.whoosh, target)
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

        public BulletProjectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, Body target) :
            base(manager, position, initialVelocity, new Health.DamageAmount() { Amount = 30.0f, DamageType = Health.DamageType.Normal }, 0.25f, ContentPaths.Particles.stone_particle, null, ContentPaths.Audio.Oscar.sfx_ic_dwarf_musket_bullet_explode_1, target)
        {
            HitAnimation = new Animation(ContentPaths.Effects.explode, 32, 32, 0, 1, 2, 3, 4);
        }
    }

    public class MudProjectile : Projectile
    {
        public MudProjectile()
        {

        }

        public MudProjectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, Body target) :
            base(manager, position, initialVelocity, new Health.DamageAmount() { Amount = 30.0f, DamageType = Health.DamageType.Normal }, 0.25f, ContentPaths.Entities.mudman_projectile, "dirt_particle", ContentPaths.Audio.gravel, target, true, true)
        {
            HitAnimation = new Animation(ContentPaths.Effects.flash, 32, 32, 0, 1, 2, 3, 4);
        }
    }
}
