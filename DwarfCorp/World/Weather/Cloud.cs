using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Cloud : SimpleSprite
    {
        [EntityFactory("Snow Cloud")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Cloud(Manager, 0.1f, 50, 40, Position, 0.0f) { TypeofStorm = StormType.SnowStorm };
        }

        [EntityFactory("Rain Cloud")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Cloud(Manager, 0.1f, 50, 40, Position, 0.25f) { TypeofStorm = StormType.RainStorm };
        }

        [EntityFactory("Storm")]
        private static GameComponent __factory2(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {

            Weather.CreateForecast(Manager.World.Time.CurrentDate, Manager.World.ChunkManager.Bounds, Manager.World, 3);
            Weather.CreateStorm(MathFunctions.RandVector3Cube() * 10, MathFunctions.Rand(0.05f, 1.0f), Manager.World);
            return new Cloud(Manager, 0.1f, 50, 40, Position, 0.0f);
        }

        public float Raininess { get; set; }
        public float Height { get; set; }
        public int MaxRainDrops { get; set; }
        public Vector3 Velocity { get; set; }
        public float LightningChance { get; set; }

        public struct Rain
        {
            public Vector3 Pos;
            public Vector3 Vel;
            public bool IsAlive;
            public Particle Particle;
        }

        [JsonIgnore]
        public Rain[] RainDrops { get; set; }
        public StormType TypeofStorm { get; set; }

        [OnDeserialized]
        public void OnDeserializing(StreamingContext ctx)
        {
            RainDrops = new Rain[MaxRainDrops];
        }


        public Cloud()
        {
            LightningChance = 0.0f;
            MaxRainDrops = 0;
            RainDrops = null;
            TypeofStorm = StormType.RainStorm;
        }

        public Cloud(ComponentManager manager, float raininess, int maxRain, float height, Vector3 pos, float lightning) :
            base(manager, "Cloud", Matrix.CreateTranslation(pos), new SpriteSheet(MathFunctions.RandEvent(0.5f) ? ContentPaths.Particles.cloud1 : ContentPaths.Particles.cloud2), new Point(0, 0))
        {
            LightningChance = lightning;
            Matrix tf = LocalTransform;
            tf.Translation = new Vector3(pos.X, height, pos.Z);
            LocalTransform = tf;
            Raininess = raininess;
            MaxRainDrops = maxRain;
            Height = height;
            RainDrops = new Rain[MaxRainDrops];
            Velocity = new Vector3(1, 0, 0);
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (GameSettings.Current.DisableWeather)
            {
                Die();
                return;
            }

            Storm.InitializeStatics();
            BoundingBox box = chunks.Bounds;
            box.Expand(10.0f);

            if (GlobalTransform.Translation.X < box.Min.X ||
                GlobalTransform.Translation.X > box.Max.X ||
                GlobalTransform.Translation.Z < box.Min.Z ||
                GlobalTransform.Translation.Z > box.Max.Z)
            {
                Die();
                return;
            }


            bool generateRainDrop = MathFunctions.RandEvent(Raininess * 0.75f);

            if (generateRainDrop)
                for (int i = 0; i < MaxRainDrops; i++)
                {
                    if (!RainDrops[i].IsAlive)
                    {
                        RainDrops[i].IsAlive = true;
                        if (RainDrops[i].Particle != null)
                        {
                            RainDrops[i].Particle.LifeRemaining = 60.0f;
                            RainDrops[i].Particle.TimeAlive = 0.0f;
                        }
                        RainDrops[i].Pos = MathFunctions.RandVector3Box(BoundingBox.Expand(5));
                        RainDrops[i].Pos = new Vector3(RainDrops[i].Pos.X, BoundingBox.Min.Y - 1, RainDrops[i].Pos.Z);
                        RainDrops[i].Vel = Vector3.Down * Storm.Properties[TypeofStorm].RainSpeed + Velocity;
                        break;
                    }
                }

            bool generateLightning = LightningChance > 0.0f && MathFunctions.RandEvent((float)(LightningChance * 0.001f));

            if (generateLightning)
            {
                var below = VoxelHelpers.FindFirstVoxelBelowIncludingWater(new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(new Vector3(Position.X, Math.Min(World.WorldSizeInVoxels.Y - 1, Position.Y), Position.Z))));
                if (below.IsValid && !below.IsEmpty)
                {
                    var above = VoxelHelpers.GetVoxelAbove(below);
                    if (above.IsValid)
                    {
                        EntityFactory.CreateEntity<Fire>("Fire", above.GetBoundingBox().Center());
                        List<Vector3> lightningStrikes = new List<Vector3>();
                        List<Color> colors = new List<Color>();
                        var c = above.GetBoundingBox().Center();
                        for (float t = 0; t < 1.0f; t += 0.25f)
                        {
                            var p = c * t + Position * (1.0f - t);
                            lightningStrikes.Add(p + MathFunctions.RandVector3Box(-5, 5, 0, 0.1f, -5, 5));
                            colors.Add(Color.White);
                        }
                        lightningStrikes.Add(c);
                        colors.Add(Color.White);
                        Drawer3D.DrawLineList(lightningStrikes, colors, 0.3f);
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_rain_storm_alert, MathFunctions.Rand(0.001f, 0.05f), MathFunctions.Rand(0.5f, 1.0f));
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_trap_destroyed, c, false, 1.0f, MathFunctions.Rand(-0.5f, 0.5f));
                        World.ParticleManager.Trigger("explode", c, Color.White, 10);
                    }
                }
            }

            Storm.StormProperties stormProperties = Storm.Properties[TypeofStorm];
            var rainEmitter = World.ParticleManager.Effects[stormProperties.RainEffect];
            var hitEmitter = World.ParticleManager.Effects[stormProperties.HitEffect];
            for (int i = 0; i < MaxRainDrops; i++)
            {
                if (!RainDrops[i].IsAlive) continue;

                RainDrops[i].Pos += RainDrops[i].Vel * DwarfTime.Dt;

                if (stormProperties.RainRandom > 0)
                {
                    RainDrops[i].Vel.X += MathFunctions.Rand(-1, 1) * stormProperties.RainRandom * DwarfTime.Dt;
                    RainDrops[i].Vel.Z += MathFunctions.Rand(-1, 1) * stormProperties.RainRandom * DwarfTime.Dt;
                }

                if (RainDrops[i].Pos.Y < 0)
                {
                    RainDrops[i].IsAlive = false;
                }

                if (!RainDrops[i].IsAlive && RainDrops[i].Particle != null)
                {
                    RainDrops[i].Particle.LifeRemaining = -1;
                }
                else if (RainDrops[i].IsAlive && RainDrops[i].Particle == null)
                {
                    RainDrops[i].Particle = rainEmitter.Emitters[0].CreateParticle(RainDrops[i].Pos,
                        RainDrops[i].Vel, Color.White);
                }
                else if (RainDrops[i].IsAlive && RainDrops[i].Particle != null)
                {
                    RainDrops[i].Particle.Position = RainDrops[i].Pos;
                    RainDrops[i].Particle.Velocity = RainDrops[i].Vel;
                }

                var test = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(RainDrops[i].Pos));
                if (!test.IsValid || (test.IsEmpty && test.LiquidLevel == 0)) continue;

                RainDrops[i].IsAlive = false;

                var hitBodies = World.EnumerateIntersectingRootObjects(new BoundingBox(RainDrops[i].Pos - Vector3.One, RainDrops[i].Pos + Vector3.One));

                foreach (var body in hitBodies)
                {
                    if (body.Parent != Manager.RootComponent)
                        continue;

                    if (body.GetRoot().GetComponent<Flammable>().HasValue(out var flames))
                        flames.Heat *= 0.25f;

                    if (body.GetRoot().GetComponent<Seedling>().HasValue(out var seeds))
                    {
                        if (TypeofStorm == StormType.RainStorm)
                            seeds.GrowthTime += MathFunctions.Rand(1.0f, 12.0f);
                        else if (MathFunctions.RandEvent(0.01f))
                            seeds.GetRoot().Die();
                    }
                }

                hitEmitter.Trigger(1, RainDrops[i].Pos + Vector3.UnitY * 0.5f, Color.White);

                //if (!MathFunctions.RandEvent(0.1f)) continue;

                var above = test.IsEmpty ? test : VoxelHelpers.GetVoxelAbove(test);

                if (!above.IsValid || !above.IsEmpty) continue;
                if (TypeofStorm == StormType.RainStorm &&
                    (above.LiquidLevel < WaterManager.maxWaterLevel && (above.LiquidType == LiquidType.Water)))
                {
                    above.LiquidLevel = (byte)Math.Min(WaterManager.maxWaterLevel, above.LiquidLevel + WaterManager.rainFallAmount);
                    above.LiquidType = stormProperties.LiquidToCreate;
                }
                else if (TypeofStorm == StormType.SnowStorm && above.IsEmpty && above.LiquidLevel == 0)
                {
                    if (test.GrassType == 0)
                    {
                        test.GrassType = Library.GetGrassType("snow").ID;
                        test.GrassDecay = Library.GetGrassType("snow").InitialDecayValue;
                    }
                    else
                    {
                        var existingGrass = Library.GetGrassType((byte)test.GrassType);
                        if (!String.IsNullOrEmpty(existingGrass.BecomeWhenSnowedOn))
                        {
                            var newGrass = Library.GetGrassType(existingGrass.BecomeWhenSnowedOn);
                            test.GrassType = newGrass.ID;
                            test.GrassDecay = newGrass.InitialDecayValue;
                        }
                    }
                }
            }

            Matrix tf = LocalTransform;
            tf.Translation += Velocity * DwarfTime.Dt;
            LocalTransform = tf;
        }

        public override void Die()
        {
            foreach (Rain raindrop in RainDrops)
            {
                if (raindrop.Particle != null)
                {
                    raindrop.Particle.LifeRemaining = -1;
                    raindrop.Particle.Position = Vector3.One * 9999;
                }
            }

            base.Die();
        }

        public override void Delete()
        {
            foreach (Rain raindrop in RainDrops)
            {
                if (raindrop.Particle != null)
                {
                    raindrop.Particle.LifeRemaining = -1;
                    raindrop.Particle.Position = Vector3.One * 9999;
                }
            }

            base.Delete();
        }
    }
}