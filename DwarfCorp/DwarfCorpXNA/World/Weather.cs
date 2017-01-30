using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Weather
    {
        public enum StormType
        {
            SnowStorm,
            RainStorm
        }

        public List<Storm> Forecast { get; set; }

        public Weather()
        {
            Forecast= new List<Storm>();
        }

        public void Update()
        {
            DateTime currentDate = WorldManager.Time.CurrentDate;

            foreach (Storm storm in Forecast)
            {
                if (!storm.IsInitialized && currentDate > storm.Date)
                {
                    storm.Start();
                }
            }

            Forecast.RemoveAll(storm => storm.IsDone());

            if (Forecast.Count == 0)
            {
                Forecast = CreateForecast(5);
            }
        }

        public class Storm
        {
            public Vector3 WindSpeed { get; set; }
            public float Intensity { get; set; }
            public DateTime Date { get; set; }
            public bool IsInitialized { get; set; }
            public StormType TypeofStorm { get; set; }

            public struct StormProperties
            {
                public ParticleEffect RainEffect { get; set; }
                public ParticleEffect HitEffect { get; set; }
                public float RainSpeed { get; set; }
                public float RainRandom { get; set; }
                public bool CreatesLiquid { get; set; }
                public LiquidType LiquidToCreate { get; set; }
                public bool CreatesVoxel { get; set; }
                public VoxelType VoxelToCreate { get; set; }
            }

            public static Dictionary<StormType, StormProperties> Properties { get; set; }

            static Storm()
            {
                Properties = new Dictionary<StormType, StormProperties>()
                {
                    {
                        StormType.RainStorm, new StormProperties()
                        {
                            RainEffect = WorldManager.ParticleManager.Effects["rain"],
                            HitEffect = WorldManager.ParticleManager.Effects["splat"],
                            RainSpeed = 30,
                            CreatesLiquid = true,
                            LiquidToCreate = LiquidType.Water
                        }
                    },
                    {
                        StormType.SnowStorm, new StormProperties()
                        {
                            RainEffect = WorldManager.ParticleManager.Effects["snowflake"],
                            HitEffect = WorldManager.ParticleManager.Effects["snow_particle"],
                            RainSpeed = 10,
                            RainRandom = 10f,
                            CreatesVoxel = true,
                            VoxelToCreate = VoxelLibrary.GetVoxelType("Snow")
                        }
                    }
                };
            }

            public Storm()
            {
                IsInitialized = false;
                TypeofStorm = StormType.RainStorm;
            }

            public bool IsDone()
            {
                return IsInitialized && WorldManager.Time.CurrentDate > Date;
            }

            public void Start()
            {
                WorldManager.MakeAnnouncement("A storm is coming!", null);
                BoundingBox bounds = WorldManager.ChunkManager.Bounds;
                Vector3 extents = bounds.Extents();
                Vector3 center = bounds.Center();
                Vector3 windNormalized = WindSpeed / WindSpeed.Length();
                Vector3 offset = new Vector3(-windNormalized.X * extents.X + center.X, bounds.Max.Y + 5, -windNormalized.Z * extents.Z + center.Z);
                Vector3 perp = new Vector3(-windNormalized.Z, 0, windNormalized.X);
                int numClouds = (int)(MathFunctions.RandInt(10, 100) * Intensity);

                for (int i = 0; i < numClouds; i++)
                {
                    Vector3 cloudPos = offset + perp * 5 * (i - numClouds / 2) + MathFunctions.RandVector3Cube() * 10;

                    Cloud cloud = new Cloud(Intensity, 5, offset.Y + MathFunctions.Rand(-3.0f, 3.0f), cloudPos)
                    {
                        Velocity = WindSpeed,
                        TypeofStorm = TypeofStorm
                    };
                }
                IsInitialized = true;
            }
        }


        public static Storm CreateStorm(Vector3 windSpeed, float intensity)
        {
            windSpeed.Y = 0;
            Storm storm = new Storm()
            {
                WindSpeed = windSpeed,
                Intensity = intensity,
            };
            storm.Start();
            return storm;
        }

        public static List<Storm> CreateForecast(int days)
        {
            List<Storm> foreCast = new List<Storm>();
            DateTime date = WorldManager.Time.CurrentDate;
            for (int i = 0; i < days; i++)
            {
                // Each day, a storm could originate from a randomly selected biome
                Vector3 randomSample = MathFunctions.RandVector3Box(WorldManager.ChunkManager.Bounds);
                float rain = ChunkGenerator.GetValueAt(randomSample, Overworld.ScalarFieldType.Rainfall);
                float temperature = ChunkGenerator.GetValueAt(randomSample, Overworld.ScalarFieldType.Temperature);
                // Generate storms according to the rainfall in the biome. Up to 4 storms per day.
                int numStorms = (int) MathFunctions.Rand(0, rain*4);

                // Space out the storms by a few hours
                int stormHour = MathFunctions.RandInt(0, 6);
                for (int j = 0; j < numStorms; j++)
                {
                    bool isSnow = MathFunctions.RandEvent(1.0f - temperature);
                    Storm storm = new Storm
                    {
                        WindSpeed = MathFunctions.RandVector3Cube()*5,
                        Intensity = MathFunctions.Rand(rain, rain*2),
                        Date = date + new TimeSpan(i, stormHour, 0, 0),
                        TypeofStorm = isSnow ? StormType.SnowStorm : StormType.RainStorm
                    };
                    storm.WindSpeed = new Vector3(storm.WindSpeed.X, 0, storm.WindSpeed.Z);
                    stormHour += MathFunctions.RandInt(1, 12);
                    foreCast.Add(storm);
                }
            }
            return foreCast;
        }


        public class Cloud : Fixture
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

            public Cloud(float raininess, int maxRain, float height, Vector3 pos) :
                base(pos, new SpriteSheet(ContentPaths.Particles.stormclouds), new Point(0, 0), WorldManager.ComponentManager.RootComponent)
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

                Voxel test = new Voxel();
                Storm.StormProperties stormProperties = Storm.Properties[TypeofStorm];
                for (int i = 0; i < MaxRainDrops; i++)
                {
                    if (!RainDrops[i].IsAlive) continue;

                    RainDrops[i].Pos += RainDrops[i].Vel*DwarfTime.Dt;

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
                        RainDrops[i].Particle = stormProperties.RainEffect.Emitters[0].CreateParticle(RainDrops[i].Pos,
                            RainDrops[i].Vel, Color.White);
                    }
                    else if (RainDrops[i].IsAlive && RainDrops[i].Particle != null)
                    {
                        RainDrops[i].Particle.Position = RainDrops[i].Pos;
                        RainDrops[i].Particle.Velocity = RainDrops[i].Vel;
                    }

                    if (!WorldManager.ChunkManager.ChunkData.GetVoxel(RainDrops[i].Pos, ref test)) continue;
                    if (test == null || test.IsEmpty || test.WaterLevel > 0) continue;

                    RainDrops[i].IsAlive = false;
                    stormProperties.HitEffect.Trigger(1, RainDrops[i].Pos + Vector3.UnitY * 0.5f, Color.White);

                    if (!MathFunctions.RandEvent(0.1f)) continue;

                    Voxel above = test.IsEmpty ? test : test.GetVoxelAbove();

                    if (above == null) continue;
                    if (stormProperties.CreatesLiquid && 
                        (above.WaterLevel < 8 && (above.Water.Type == LiquidType.Water || above.Water.Type == LiquidType.None)))
                    {
                        WaterCell water = above.Water;
                        water.WaterLevel++;
                        water.Type = stormProperties.LiquidToCreate;
                                   
                        above.Water = water;
                        above.Chunk.ShouldRebuildWater = true;
                    }
                    else if (stormProperties.CreatesVoxel && above.IsEmpty && above.WaterLevel == 0)
                    {
                        above.Type = stormProperties.VoxelToCreate;
                        above.Water = new WaterCell();
                        above.Health = above.Type.StartingHealth;
                        above.Chunk.NotifyTotalRebuild(!above.IsInterior);
                    }

                }



                Matrix tf = LocalTransform;
                tf.Translation += Velocity*DwarfTime.Dt;
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
}
