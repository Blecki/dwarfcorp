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
    /// <summary>
    /// Component causes the object its attached to to become flammable. Flammable objects have "heat"
    /// when the heat is above a "flashpoint" they get damaged until they are destroyed, and emit flames.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Flammable : GameComponent
    {
        private Health _health = null;
        [JsonIgnore]
        public Health Health
        {
            get
            {
                if (_health == null)
                {
                    _health = Parent.EnumerateAll().Where(c => c is Health).FirstOrDefault() as Health;
                    System.Diagnostics.Debug.Assert(_health != null, "Flammable could not find a Health component.");
                }

                return _health;
            }
        }

        [JsonIgnore]
        public bool IsOnFire
        {
            get
            {
                return Heat >= Flashpoint;
            }
        }

        public float Heat { get; set; }
        public float Flashpoint { get; set; }
        public float Damage { get; set; }

        public Timer CheckLavaTimer { get; set; }
        public Timer SoundTimer { get; set; }
        public Timer DamageTimer { get; set; }

        [JsonIgnore]
        private List<AnimatedSprite> FlameSprites = new List<AnimatedSprite>();

        public Flammable()
        {
        }

        public Flammable(ComponentManager manager, string name) :
            base(name, manager)
        {
            FlameSprites = new List<AnimatedSprite>();
            UpdateRate = 10;
            Heat = 0.0f;
            Flashpoint = 100.0f;
            Damage = 5.0f;
            CheckLavaTimer = new Timer(1.0f + MathFunctions.Rand(1.0f, 2.0f), false, Timer.TimerMode.Real);
            SoundTimer = new Timer(1.0f + MathFunctions.Rand(0.5f, 1.0f), false, Timer.TimerMode.Real);
            DamageTimer = new Timer(1.0f + MathFunctions.Rand(0.5f, 0.75f), false, Timer.TimerMode.Game);
        }


        public void CheckSurroundings(Body Body, DwarfTime gameTime, ChunkManager chunks)
        {
            if (Heat > Flashpoint)
            {
                HashSet<Body> insideBodies = new HashSet<Body>();
                World.OctTree.EnumerateItems(Body.GetBoundingBox(), insideBodies);

                foreach (var body in insideBodies.Where(b => b != Parent && b.Active && b.Parent == Manager.RootComponent))
                {
                    var flames = body.GetComponent<Flammable>();
                    if (flames != null)
                    {
                        flames.Heat += 100;
                    }
                }
                SoundManager.PlaySound(ContentPaths.Audio.fire, Body.Position, true);
            }

            float expansion = Heat > Flashpoint ? 1.0f : 0.0f;

            foreach (var coordinate in VoxelHelpers.EnumerateCoordinatesInBoundingBox(Body.BoundingBox.Expand(expansion)))
            {
                var voxel = new VoxelHandle(chunks.ChunkData, coordinate);
                if (!voxel.IsValid) continue;

                if (Heat > Flashpoint && MathFunctions.RandEvent(0.5f))
                {
                    if (voxel.Type.IsFlammable)
                    {
                        if (MathFunctions.RandEvent(0.1f))
                        {
                            HashSet<Body> existingItems = new HashSet<Body>();
                            World.OctTree.EnumerateItems(voxel.GetBoundingBox().Expand(-0.1f), existingItems);

                            if (!existingItems.Any(e => e is Fire))
                            {
                                EntityFactory.CreateEntity<Fire>("Fire", voxel.GetBoundingBox().Center());
                            }
                            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_lava_spread, voxel.GetBoundingBox().Center(), true, 1.0f);
                            World.ChunkManager.KillVoxel(voxel);
                        }
                    }

                    if (voxel.GrassType != 0x0)
                    {
                        if (MathFunctions.RandEvent(0.1f))
                        {
                            HashSet<Body> existingItems = new HashSet<Body>();
                            var box = voxel.GetBoundingBox().Expand(-0.1f);
                            box.Min += Vector3.One;
                            box.Max += Vector3.One;
                            World.OctTree.EnumerateItems(box, existingItems);
                            if (!existingItems.Any(e => e is Fire))
                            {
                                EntityFactory.CreateEntity<Fire>("Fire", box.Center());
                            }
                        }
                        if (MathFunctions.RandEvent(0.5f))
                        {
                            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_lava_spread, voxel.GetBoundingBox().Center(), true, 1.0f);
                            voxel.GrassType = 0x0;

                        }
                    }
                }

                if (voxel.LiquidLevel <= 0) continue;
                if (voxel.LiquidType == LiquidType.Lava)
                    Heat += 100.0f;
                else if (voxel.LiquidType == LiquidType.Water)
                    Heat = Heat * 0.25f;
            }
        }

        public int GetNumTrigger(Body Body)
        {
            return
                (int)
                    MathFunctions.Clamp((int) (Math.Abs(1*Body.BoundingBox.Max.Y - Body.BoundingBox.Min.Y)), 1,
                        3);
        }

        private void CreateFlameSprite(Vector3 pos)
        {
            var tf = Matrix.CreateTranslation(pos - (Parent as Body).Position);
            SoundManager.PlaySound(ContentPaths.Audio.fire, pos, true, 1.0f);
            var sprite = Parent.AddChild(new AnimatedSprite(Manager, "Flame", tf)
            {
                OrientationType = AnimatedSprite.OrientMode.Spherical,
                LightsWithVoxels = false
            }) as AnimatedSprite;
            var frames = new List<Point>() { new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(3, 0) };
            frames.Shuffle();
            var animation = AnimationLibrary.CreateAnimation(new SpriteSheet(ContentPaths.Particles.more_flames, 32),
                frames, "Flames");
            animation.FrameHZ = MathFunctions.Rand(8.0f, 20.0f);
            animation.Loops = true;
            sprite.AddAnimation(animation);
            sprite.SetCurrentAnimation("Flames", true);
            sprite.SetFlag(Flag.ShouldSerialize, false);
            sprite.AnimPlayer.Play(animation);
            FlameSprites.Add(sprite);
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (!Active) return;
            base.Update(gameTime, chunks, camera);
            var body = Parent as Body;
            System.Diagnostics.Debug.Assert(body != null);

            DamageTimer.Update(gameTime);
            CheckLavaTimer.Update(gameTime);
            SoundTimer.Update(gameTime);
            if(CheckLavaTimer.HasTriggered)
            {
                CheckSurroundings(body, gameTime, chunks);
            }
            Heat *= 0.999f;

            if(Heat > Flashpoint)
            {
                UpdateRate = 1;
                if(DamageTimer.HasTriggered)
                    Health.Damage(Damage, Health.DamageType.Fire);

                if(SoundTimer.HasTriggered)
                    SoundManager.PlaySound(ContentPaths.Audio.fire, body.Position, true, 1.0f);
                double totalSize = (body.BoundingBox.Max - body.BoundingBox.Min).Length();
                int numFlames = (int) (totalSize / 4.0f) + 1;
                for (int i = FlameSprites.Count; i < numFlames; i++)
                {
                    CreateFlameSprite(MathFunctions.RandVector3Box(body.BoundingBox));
                }

                if (MathFunctions.RandEvent(0.06f))
                {
                    foreach (var sprite in FlameSprites)
                    {
                        Manager.World.ParticleManager.Trigger("smoke", sprite.Position + Vector3.Up * 0.5f, Color.Black, 1);
                        Manager.World.ParticleManager.Trigger("flame", sprite.Position + Vector3.Up * 0.5f, Color.Black, 1);
                    }
                }

                if (MathFunctions.RandEvent(0.01f))
                {
                    foreach (var sprite in FlameSprites)
                    {
                        sprite.Die();
                    }
                    FlameSprites.Clear();
                }
                var mesh = Parent.GetComponent<InstanceMesh>();

                if (mesh != null)
                {
                    mesh.VertexColorTint = Color.DarkGray;
                }
            }
            else
            {
                foreach (var sprite in FlameSprites)
                {
                    sprite.Die();
                }
                FlameSprites.Clear();
                UpdateRate = 10;
            }
        }
    }

    /// <summary>
    /// Standalone fire entity for spreading in the world.
    /// </summary>
    [JsonObject(IsReference =true)]
    public class Fire : Body
    {
        public Timer LifeTimer = new Timer(5.0f, true);

        [EntityFactory("Fire")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Fire(Manager, Position);
        }

        public Fire() :
            base()
        {
            LifeTimer = new Timer(MathFunctions.Rand(4.0f, 10.0f), true);
        }

        public Fire(ComponentManager manager, Vector3 pos) :
            base(manager, "Fire", Matrix.CreateTranslation(pos), Vector3.One, Vector3.Zero)
        {
            CollisionType = CollisionType.Static;
            Tags.Add("Fire");
            AddChild(new Flammable(manager, "Flammable")
            {
                Heat = 999,
            });
            AddChild(new Health(manager, "Health", 10.0f, 0.0f, 10.0f));
        }

        public override void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            base.Update(Time, Chunks, Camera);

            LifeTimer.Update(Time);
            
            if (LifeTimer.HasTriggered)
            {
                Die();
            }
        }
    }
}