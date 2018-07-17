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
    public class Projectile : Physics
    {
        public Tinter Sprite { get; set; }
        public Tinter Sprite2 { get; set; }
        public ParticleTrigger HitParticles { get; set; }
        public Health.DamageAmount Damage { get; set; }
        public Body Target { get; set; }
        public float DamageRadius { get; set; }

        [JsonIgnore]
        public Animation HitAnimation { get; set; }

        public Projectile()
        {
            
        }

        public Projectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, Health.DamageAmount damage, float size, string asset, string hitParticles, string hitNoise, Body target, bool animated = false, bool singleSprite = false) :
            base(manager, "Projectile", Matrix.CreateTranslation(position), new Vector3(size, size, size), Vector3.One, 1.0f, 1.0f, 1.0f, 1.0f, new Vector3(0, -10, 0), OrientMode.Fixed, false)
        {
            this.AllowPhysicsSleep = false; 
            Target = target;
            HitAnimation = null;
            IsSleeping = false;
            Velocity = initialVelocity;
            Orientation = OrientMode.LookAt;
            CollideMode = Physics.CollisionMode.None;
            var spriteSheet = new SpriteSheet(asset);

            if (animated)
            {
                spriteSheet.FrameWidth = Math.Min(spriteSheet.FrameWidth, spriteSheet.FrameHeight);
                spriteSheet.FrameHeight = spriteSheet.FrameWidth;
            }

            if (animated)
            {
                Sprite = AddChild(new AnimatedSprite(Manager, "Sprite", Matrix.CreateRotationY((float)Math.PI * 0.5f),
                    false)
                {
                    OrientationType = AnimatedSprite.OrientMode.Fixed
                }) as AnimatedSprite;
                (Sprite as AnimatedSprite).AddAnimation(AnimationLibrary.CreateSimpleAnimation(asset, true));

                if (singleSprite)
                {
                    (Sprite as AnimatedSprite).OrientationType = AnimatedSprite.OrientMode.Spherical;
                }
            }
            else
            {
                Sprite = AddChild(new SimpleSprite(Manager, "Sprite", Matrix.CreateRotationY((float)Math.PI * 0.5f), spriteSheet, new Point(0, 0))
                {
                    OrientationType = SimpleSprite.OrientMode.Fixed
                }) as SimpleSprite;
                (Sprite as SimpleSprite).AutoSetWorldSize();
                if (singleSprite)
                {
                    (Sprite as SimpleSprite).OrientationType = SimpleSprite.OrientMode.Spherical;
                }
            }

            if (!singleSprite)
            {
                if (animated)
                {
                    Sprite2 = Sprite.AddChild(new AnimatedSprite(Manager, "Sprite2",
                        Matrix.CreateRotationY((float)Math.PI * 0.5f) * Matrix.CreateRotationZ((float)Math.PI * 0.5f),
                        false)
                    {
                        OrientationType = AnimatedSprite.OrientMode.Fixed
                    }) as AnimatedSprite;

                    (Sprite2 as AnimatedSprite).AddAnimation(AnimationLibrary.CreateSimpleAnimation(asset, true));
                }
                else
                {
                    Sprite2 = AddChild(new SimpleSprite(Manager, "Sprite", Matrix.CreateRotationY((float)Math.PI * 0.5f) * Matrix.CreateRotationZ((float)Math.PI * 0.5f), spriteSheet, new Point(0, 0))
                    {
                        OrientationType = SimpleSprite.OrientMode.Fixed
                    }) as SimpleSprite;
                    (Sprite2 as SimpleSprite).AutoSetWorldSize();
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

            if (Target == null)
            {
                if (LocalPosition.Y < 0)
                    Die();

                if (CurrentVoxel.IsValid && !CurrentVoxel.IsEmpty)
                    Die();
            }

            base.Update(gameTime, chunks, camera);
        }

        public override void Die()
        {
            if (HitAnimation != null)
            {
                if (Sprite is AnimatedSprite)
                {
                    (Sprite as AnimatedSprite).AnimPlayer.Reset();
                    (Sprite as AnimatedSprite).AnimPlayer.Play(HitAnimation);
                }

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
}
