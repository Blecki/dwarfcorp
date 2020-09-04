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
        public GameComponent Target { get; set; }
        public float DamageRadius { get; set; }

        [JsonIgnore] public Library.SimpleAnimationTuple HitAnimation { get; set; }

        public Projectile()
        {
            
        }

        public Projectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, Health.DamageAmount damage, float size, string asset, string hitParticles, string hitNoise, GameComponent target, bool animated = false, bool singleSprite = false) :
            base(manager, "Projectile", Matrix.CreateTranslation(position), new Vector3(size, size, size), Vector3.One, 1.0f, 1.0f, 1.0f, 1.0f, new Vector3(0, -10, 0), OrientMode.Fixed)
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

            // Todo: Needs the cosmetic children treatement.
            if (animated)
            {
                Sprite = AddChild(new AnimatedSprite(Manager, "Sprite", Matrix.CreateRotationY((float)Math.PI * 0.5f))
                {
                    OrientationType = AnimatedSprite.OrientMode.Fixed
                }) as Tinter;

                var anim = Library.CreateSimpleAnimation(asset);
                anim.Animation.Loops = true;
                (Sprite as AnimatedSprite).AddAnimation(anim.Animation);
                (Sprite as AnimatedSprite).SpriteSheet = anim.SpriteSheet;

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

            Sprite.SetFlag(Flag.ShouldSerialize, false);

            if (!singleSprite)
            {
                if (animated)
                {
                    Sprite2 = Sprite.AddChild(new AnimatedSprite(Manager, "Sprite2",
                        Matrix.CreateRotationY((float)Math.PI * 0.5f) * Matrix.CreateRotationZ((float)Math.PI * 0.5f))
                    {
                        OrientationType = AnimatedSprite.OrientMode.Fixed
                    }) as AnimatedSprite;

                    var anim = Library.CreateSimpleAnimation(asset);
                    anim.Animation.Loops = true;
                    (Sprite2 as AnimatedSprite).AddAnimation(anim.Animation);
                    (Sprite2 as AnimatedSprite).SpriteSheet = anim.SpriteSheet;
                }
                else
                {
                    Sprite2 = AddChild(new SimpleSprite(Manager, "Sprite", Matrix.CreateRotationY((float)Math.PI * 0.5f) * Matrix.CreateRotationZ((float)Math.PI * 0.5f), spriteSheet, new Point(0, 0))
                    {
                        OrientationType = SimpleSprite.OrientMode.Fixed
                    }) as SimpleSprite;
                    (Sprite2 as SimpleSprite).AutoSetWorldSize();
                }
                Sprite2.SetFlag(Flag.ShouldSerialize, false);
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

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (Target != null && (Target.Position - LocalPosition).LengthSquared() < DamageRadius)
            {
                if (Target.GetRoot().GetComponent<Health>().HasValue(out var health))
                {
                    health.Damage(Damage.Amount, Damage.DamageType);
                    var knock = (Target.Position - Position);
                    knock.Normalize();
                    knock *= 0.2f;
                    if (Target.AnimationQueue.Count == 0)
                        Target.AnimationQueue.Add(new KnockbackAnimation(0.15f, Target.LocalTransform, knock));
                }

                if (Damage.DamageType == Health.DamageType.Fire)
                    if (Target.GetRoot().GetComponent<Flammable>().HasValue(out var flammable))
                        flammable.Heat += 50.0f;

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
                    (Sprite as AnimatedSprite).AnimPlayer.Play(HitAnimation.Animation);
                    (Sprite as AnimatedSprite).SpriteSheet = HitAnimation.SpriteSheet;
                }

                if (Target != null)
                {
                    Vector3 camvelocity0 = GameState.Game.GraphicsDevice.Viewport.Project( Position,
                        Manager.World.Renderer.Camera.ProjectionMatrix, Manager.World.Renderer.Camera.ViewMatrix, Matrix.Identity);
                    Vector3 camvelocity1 = GameState.Game.GraphicsDevice.Viewport.Project(Position + Velocity,
                        Manager.World.Renderer.Camera.ProjectionMatrix, Manager.World.Renderer.Camera.ViewMatrix, Matrix.Identity);
                    IndicatorManager.DrawIndicator(HitAnimation.SpriteSheet, HitAnimation.Animation, Target.Position,
                        HitAnimation.Animation.FrameHZ*HitAnimation.Animation.Frames.Count + 1.0f, 1.0f, Vector2.Zero, Color.White, camvelocity1.X - camvelocity0.X > 0);
                }
            }
            base.Die();
        }


        public override void OnTerrainCollision(VoxelHandle vox)
        {
            if (Target == null || Target.IsDead)
            {
                var transform = LocalTransform;
                transform.Translation -= Velocity;
                LocalTransform = transform;

                if (!IsDead)
                    Die();
            }

            base.OnTerrainCollision(vox);
        }        
    }
}
