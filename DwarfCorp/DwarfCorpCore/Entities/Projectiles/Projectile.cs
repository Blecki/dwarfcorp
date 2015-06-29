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
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Projectile : Physics
    {

        public Sprite Sprite { get; set; }
        public ParticleTrigger HitParticles { get; set; }
        public Health.DamageAmount Damage { get; set; }
        public Faction Faction { get; set; }
        public float DamageRadius { get; set; }
        public Animation HitAnimation { get; set; }
        public Projectile()
        {
            
        }

        public Projectile(Vector3 position, Vector3 initialVelocity, Health.DamageAmount damage, float size, string asset, string hitParticles, string hitNoise, string faction) : 
            base("Projectile", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(size, size, size), Vector3.One, 1.0f, 1.0f, 1.0f, 1.0f, new Vector3(0, -10, 0) )
        {
            Faction = PlayState.ComponentManager.Factions.Factions[faction];
            HitAnimation = null;
            IsSleeping = false;
            Velocity = initialVelocity;
            Orientation = OrientMode.LookAt;
            AddToCollisionManager = false;

            Sprite = new Sprite(PlayState.ComponentManager, "Sprite", this, Matrix.CreateRotationY((float)Math.PI * 0.5f),
                new SpriteSheet(asset), false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            Sprite.SetSingleFrameAnimation(new Point(0, 0));
            Sprite sprite2 = new Sprite(PlayState.ComponentManager, "Sprite2", Sprite, Matrix.CreateRotationX((float)Math.PI * 0.5f),
                new SpriteSheet(asset), false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite2.SetSingleFrameAnimation(new Point(0, 0));

            Damage = damage;
            HitParticles = new ParticleTrigger(hitParticles, PlayState.ComponentManager, "Hit Particles", this,
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
            bool got = false;
            foreach (var faction in Manager.Factions.Factions)
            {
                if (faction.Value.Name == Faction.Name) continue;
                else if (PlayState.Diplomacy.GetPolitics(Faction, faction.Value).GetCurrentRelationship() != Relationship.Loves)
                {
                    foreach (CreatureAI creature in faction.Value.Minions)
                    {
                        if ((creature.Position - Position).LengthSquared() < DamageRadius)
                        {
                            creature.Creature.Damage(Damage.Amount, Damage.DamageType);
                            got = true;
                            break;
                        }
                    }
                }

                if (got) break;
            }

            if (got)
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
                IndicatorManager.DrawIndicator(HitAnimation, Position, HitAnimation.FrameHZ * HitAnimation.Frames.Count, 1.0f, Vector2.Zero, Color.White, false);
            }
            base.Die();
        }


        public override void OnTerrainCollision(Voxel vox)
        {
            Matrix transform = LocalTransform;
            transform.Translation -= Velocity;
            LocalTransform = transform;

            if(!IsDead)
                Die();

            base.OnTerrainCollision(vox);
        }

        
    }

    [JsonObject(IsReference = true)]
    public class ArrowProjectile : Projectile
    {
        public ArrowProjectile()
        {
            
        }

        public ArrowProjectile(Vector3 position, Vector3 initialVelocity, string faction) :
            base(position, initialVelocity, new Health.DamageAmount() { Amount = 10.0f, DamageType = Health.DamageType.Slashing }, 0.25f, ContentPaths.Entities.Elf.Sprites.arrow, "puff", ContentPaths.Audio.hit, faction)
        {
            HitAnimation = new Animation(ContentPaths.Effects.flash, 32, 32, 0, 1, 2, 3);
        }
    }
}
