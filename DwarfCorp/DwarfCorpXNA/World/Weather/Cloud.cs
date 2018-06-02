using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Cloud : SimpleSprite, IUpdateableComponent
    {
        [EntityFactory("Snow Cloud")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Cloud(Manager, 0.1f, 50, 40, Position) { TypeofStorm = StormType.SnowStorm };
        }

        [EntityFactory("Rain Cloud")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Cloud(Manager, 0.1f, 50, 40, Position) { TypeofStorm = StormType.RainStorm };
        }

        [EntityFactory("Storm")]
        private static GameComponent __factory2(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {

            Weather.CreateForecast(Manager.World.Time.CurrentDate, Manager.World.ChunkManager.Bounds, Manager.World, 3);
            Weather.CreateStorm(MathFunctions.RandVector3Cube() * 10, MathFunctions.Rand(0.05f, 1.0f), Manager.World);
            return new Cloud(Manager, 0.1f, 50, 40, Position);
        }

        public float Raininess { get; set; }
        public float Height { get; set; }
        public int MaxRainDrops { get; set; }
        public Vector3 Velocity { get; set; }

        public struct Rain
        {
            public Vector3 Pos;
            public Vector3 Vel;
            public bool IsAlive;
            public Particle Particle;
        }

        public Rain[] RainDrops { get; set; }
        public StormType TypeofStorm { get; set; }

        public Cloud()
        {
            MaxRainDrops = 0;
            RainDrops = null;
            TypeofStorm = StormType.RainStorm;
        }

        public Cloud(ComponentManager manager, float raininess, int maxRain, float height, Vector3 pos) :
            base(manager, "Cloud", Matrix.CreateTranslation(pos), new SpriteSheet(MathFunctions.RandEvent(0.5f) ? ContentPaths.Particles.cloud1 : ContentPaths.Particles.cloud2), new Point(0, 0))
        {
            Matrix tf = LocalTransform;
            tf.Translation = new Vector3(pos.X, height, pos.Z);
            LocalTransform = tf;
            Raininess = raininess;
            MaxRainDrops = maxRain;
            Height = height;
            RainDrops = new Rain[MaxRainDrops];
            Velocity = new Vector3(1, 0, 0);
        }

        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            Storm.InitializeStatics();
            BoundingBox box = chunks.Bounds;
            box.Expand(10.0f);

            if (GlobalTransform.Translation.X < box.Min.X ||
                GlobalTransform.Translation.X > box.Max.X ||
                GlobalTransform.Translation.Z < box.Min.Z ||
                GlobalTransform.Translation.Z > box.Max.Z)
            {
                Die();
            }


            bool generateRainDrop = MathFunctions.RandEvent(Raininess);

            if (generateRainDrop)
                for (int i = 0; i < MaxRainDrops; i++)
                {
                    if (!RainDrops[i].IsAlive)
                    {
                        RainDrops[i].IsAlive = true;
                        RainDrops[i].Pos = MathFunctions.RandVector3Box(BoundingBox);
                        RainDrops[i].Pos = new Vector3(RainDrops[i].Pos.X, BoundingBox.Min.Y - 1, RainDrops[i].Pos.Z);
                        RainDrops[i].Vel = Vector3.Down * Storm.Properties[TypeofStorm].RainSpeed + Velocity;
                        break;
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
                    RainDrops[i].Particle = null;
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

                var test = new VoxelHandle(chunks.ChunkData,
                    GlobalVoxelCoordinate.FromVector3(RainDrops[i].Pos));
                if (!test.IsValid || test.IsEmpty || test.LiquidLevel > 0) continue;

                RainDrops[i].IsAlive = false;
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
                        test.GrassType = GrassLibrary.GetGrassType("snow").ID;
                    else
                    {
                        var existingGrass = GrassLibrary.GetGrassType((byte)test.GrassType);
                        if (!String.IsNullOrEmpty(existingGrass.BecomeWhenSnowedOn))
                            test.GrassType = GrassLibrary.GetGrassType(existingGrass.BecomeWhenSnowedOn).ID;
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
                }
            }

            base.Delete();
        }
    }
}