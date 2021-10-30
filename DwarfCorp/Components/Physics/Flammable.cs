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
                    global::System.Diagnostics.Debug.Assert(_health != null, "Flammable could not find a Health component.");
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
            Heat = 0.0f;
            Flashpoint = 100.0f;
            Damage = 5.0f;
            CheckLavaTimer = new Timer(1.0f + MathFunctions.Rand(1.0f, 2.0f), false, Timer.TimerMode.Real);
            SoundTimer = new Timer(1.0f + MathFunctions.Rand(0.5f, 1.0f), false, Timer.TimerMode.Real);
            DamageTimer = new Timer(1.0f + MathFunctions.Rand(0.5f, 0.75f), false, Timer.TimerMode.Game);
        }


        public void CheckSurroundings(GameComponent Body, DwarfTime gameTime, ChunkManager chunks)
        {
            if (Heat > Flashpoint)
            {
                var insideBodies = World.EnumerateIntersectingRootObjects(Body.GetBoundingBox());

                foreach (var body in insideBodies.Where(b => b != Parent && b.Active))
                    if (body.GetComponent<Flammable>().HasValue(out var flames))
                        flames.Heat += 100;

                SoundManager.PlaySound(ContentPaths.Audio.fire, Body.Position, true);
            }

            float expansion = Heat > Flashpoint ? 1.0f : 0.0f;

            foreach (var coordinate in VoxelHelpers.EnumerateCoordinatesInBoundingBox(Body.BoundingBox.Expand(expansion)))
            {
                var voxel = new VoxelHandle(chunks, coordinate);
                if (!voxel.IsValid) continue;

                if (Heat > Flashpoint && MathFunctions.RandEvent(0.5f))
                {
                    if (voxel.Type.IsFlammable)
                    {
                        if (MathFunctions.RandEvent(0.1f))
                        {
                            var existingItems = World.EnumerateIntersectingRootObjects(voxel.GetBoundingBox().Expand(-0.1f));
                            if (!existingItems.Any(e => e is Fire))
                                EntityFactory.CreateEntity<Fire>("Fire", voxel.GetBoundingBox().Center());
                            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_lava_spread, voxel.GetBoundingBox().Center(), true, 1.0f);
                            VoxelHelpers.KillVoxel(World, voxel);
                        }
                    }

                    if (voxel.GrassType != 0x0)
                    {
                        if (MathFunctions.RandEvent(0.1f))
                        {
                            var box = voxel.GetBoundingBox().Expand(-0.1f);
                            box.Min += Vector3.One; // Todo: Why shifting one on every axis?
                            box.Max += Vector3.One;
                            if (!World.EnumerateIntersectingRootObjects(box).Any(e => e is Fire))
                                EntityFactory.CreateEntity<Fire>("Fire", box.Center());
                        }

                        if (MathFunctions.RandEvent(0.5f))
                        {
                            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_lava_spread, voxel.GetBoundingBox().Center(), true, 1.0f);
                            voxel.GrassType = 0x0;

                        }
                    }
                }

                if (voxel.LiquidLevel <= 0 || voxel.LiquidType == 0) continue;
                if (Library.GetLiquid(voxel.LiquidType).HasValue(out var liquid))
                    Heat += liquid.TemperatureIncrease;
            }
        }

        public int GetNumTrigger(GameComponent Body)
        {
            return
                (int)
                    MathFunctions.Clamp((int) (Math.Abs(1*Body.BoundingBox.Max.Y - Body.BoundingBox.Min.Y)), 1,
                        3);
        }

        private void CreateFlameSprite(Vector3 pos)
        {
            var tf = Matrix.CreateTranslation(pos - (Parent as GameComponent).Position);
            SoundManager.PlaySound(ContentPaths.Audio.fire, pos, true, 1.0f);
            var sprite = Parent.AddChild(new AnimatedSprite(Manager, "Flame", tf)
            {
                OrientationType = AnimatedSprite.OrientMode.Spherical,
                LightsWithVoxels = false
            }) as AnimatedSprite;
            var frames = new List<Point>() { new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(3, 0) };
            frames.Shuffle();
            var spriteSheet = new SpriteSheet("Particles\\moreflames", 32);
            var animation = Library.CreateAnimation(frames, "Flames");
            animation.FrameHZ = MathFunctions.Rand(8.0f, 20.0f);
            animation.Loops = true;
            sprite.AddAnimation(animation);
            sprite.SetCurrentAnimation("Flames", true);
            sprite.SetFlag(Flag.ShouldSerialize, false);
            sprite.SpriteSheet = spriteSheet;
            sprite.AnimPlayer.Play(animation);
            FlameSprites.Add(sprite);
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (!Active) return;
            base.Update(gameTime, chunks, camera);
            var body = Parent as GameComponent;
            global::System.Diagnostics.Debug.Assert(body != null);

            DamageTimer.Update(gameTime);
            CheckLavaTimer.Update(gameTime);
            SoundTimer.Update(gameTime);

            if (CheckLavaTimer.HasTriggered)
                CheckSurroundings(body, gameTime, chunks);

            Heat *= 0.999f;

            if(Heat > Flashpoint)
            {
                if(DamageTimer.HasTriggered && Health != null)
                    Health.Damage(gameTime, Damage, Health.DamageType.Fire);

                if(SoundTimer.HasTriggered)
                    SoundManager.PlaySound(ContentPaths.Audio.fire, body.Position, true, 1.0f);

                double totalSize = (body.BoundingBox.Max - body.BoundingBox.Min).Length();
                int numFlames = (int) (totalSize / 4.0f) + 1;

                for (int i = FlameSprites.Count; i < numFlames; i++)
                    CreateFlameSprite(MathFunctions.RandVector3Box(body.BoundingBox));

                if (MathFunctions.RandEvent(0.06f))
                    foreach (var sprite in FlameSprites)
                    {
                        Manager.World.ParticleManager.Trigger("smoke", sprite.Position + Vector3.Up * 0.5f, Color.Black, 1);
                        Manager.World.ParticleManager.Trigger("flame", sprite.Position + Vector3.Up * 0.5f, Color.Black, 1);
                    }

                if (MathFunctions.RandEvent(0.01f))
                {
                    foreach (var sprite in FlameSprites)
                        sprite.Die();

                    FlameSprites.Clear();
                }

                if (Parent.GetComponent<InstanceMesh>().HasValue(out var mesh))
                    mesh.VertexColorTint = Color.DarkGray;
            }
            else
            {
                foreach (var sprite in FlameSprites)
                    sprite.Die();

                FlameSprites.Clear();
            }
        }
    }
}