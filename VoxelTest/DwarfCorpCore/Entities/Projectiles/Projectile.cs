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
            AddToOctree = false;

            Sprite = new Sprite(PlayState.ComponentManager, "Sprite", this, Matrix.CreateRotationY((float)Math.PI * 0.5f),
                TextureManager.GetTexture(asset), false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            Sprite.SetSingleFrameAnimation(new Point(0, 0));
            Sprite sprite2 = new Sprite(PlayState.ComponentManager, "Sprite2", Sprite, Matrix.CreateRotationX((float)Math.PI * 0.5f),
                TextureManager.GetTexture(asset), false)
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

        public override void Update(DwarfTime DwarfTime, ChunkManager chunks, Camera camera)
        {
            bool got = false;
            foreach (var faction in Manager.Factions.Factions)
            {
                if (faction.Value.Name == Faction.Name) continue;
                else if (Alliance.GetRelationship(faction.Value.Name, Faction.Name) != Relationship.Loves)
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

            base.Update(DwarfTime, chunks, camera);
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
