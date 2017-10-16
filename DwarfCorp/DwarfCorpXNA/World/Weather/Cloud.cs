using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Cloud : Fixture, IUpdateableComponent
    {
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
            base(manager, pos, new SpriteSheet(MathFunctions.RandEvent(0.5f) ? ContentPaths.Particles.cloud1 : ContentPaths.Particles.cloud2), new Point(0, 0))
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

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
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
                if (!test.IsValid || test.IsEmpty || test.WaterCell.WaterLevel > 0) continue;

                RainDrops[i].IsAlive = false;
                hitEmitter.Trigger(1, RainDrops[i].Pos + Vector3.UnitY * 0.5f, Color.White);

                if (!MathFunctions.RandEvent(0.1f)) continue;

                var above = test.IsEmpty ? test : VoxelHelpers.GetVoxelAbove(test);

                if (!above.IsValid || !above.IsEmpty) continue;
                if (stormProperties.CreatesLiquid &&
                    (above.WaterCell.WaterLevel < WaterManager.maxWaterLevel && (above.WaterCell.Type == LiquidType.Water || above.WaterCell.Type == LiquidType.None)))
                {
                    WaterCell water = above.WaterCell;
                    water.WaterLevel = (byte)Math.Min(WaterManager.maxWaterLevel, water.WaterLevel + WaterManager.rainFallAmount);
                    water.Type = stormProperties.LiquidToCreate;

                    above.WaterCell = water;
                }
                else if (stormProperties.CreatesVoxel && above.IsEmpty && above.WaterCell.WaterLevel == 0)
                {
                    above.Type = stormProperties.VoxelToCreate;
                    above.WaterCell = new WaterCell();
                    above.Health = above.Type.StartingHealth;
                }

            }



            Matrix tf = LocalTransform;
            tf.Translation += Velocity * DwarfTime.Dt;
            LocalTransform = tf;
            base.Update(gameTime, chunks, camera);
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