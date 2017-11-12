using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Storm
    {
        public Vector3 WindSpeed { get; set; }
        public float Intensity { get; set; }
        public DateTime Date { get; set; }
        public bool IsInitialized { get; set; }
        public StormType TypeofStorm { get; set; }
        public List<Cloud> Clouds { get; set; }
        public struct StormProperties
        {
            public string RainEffect { get; set; }
            public string HitEffect { get; set; }
            public float RainSpeed { get; set; }
            public float RainRandom { get; set; }
            public bool CreatesLiquid { get; set; }
            public LiquidType LiquidToCreate { get; set; }
            public bool CreatesVoxel { get; set; }
            public VoxelType VoxelToCreate { get; set; }
        }

        public static Dictionary<StormType, StormProperties> Properties { get; set; }
        private static bool staticsInitialized = false;

        public static void InitializeStatics()
        {
            if (staticsInitialized)
            {
                return;
            }

            staticsInitialized = true;
            Properties = new Dictionary<StormType, StormProperties>()
                {
                    {
                        StormType.RainStorm, new StormProperties()
                        {
                            RainEffect = "rain",//particles.Effects["rain"],
                            HitEffect = "splat",//particles.Effects["splat"],
                            RainSpeed = 30,
                            CreatesLiquid = true,
                            LiquidToCreate = LiquidType.Water
                        }
                    },
                    {
                        StormType.SnowStorm, new StormProperties()
                        {
                            RainEffect = "snowflake",//particles.Effects["snowflake"],
                            HitEffect = "snow_particle",//particles.Effects["snow_particle"],
                            RainSpeed = 10,
                            RainRandom = 10f,
                            CreatesVoxel = true,
                            VoxelToCreate = VoxelLibrary.GetVoxelType("Snow")
                        }
                    }
                };
        }

        public WorldManager World { get; set; }

        public Storm(WorldManager world)
        {
            Clouds = new List<Cloud>();
            World = world;
            IsInitialized = false;
            TypeofStorm = StormType.RainStorm;

            if (!staticsInitialized)
            {
                InitializeStatics();
            }
        }

        public bool IsDone()
        {
            return IsInitialized && World.Time.CurrentDate > Date && Clouds.All(cloud => cloud.IsDead);
        }

        public void Start()
        {
            World.MakeAnnouncement("A storm is coming!", null);

            switch (TypeofStorm)
            {
                case StormType.RainStorm:
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_rain_storm_alert, 0.5f);
                    break;
                case StormType.SnowStorm:
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_snow_storm_alert, 0.5f);
                    break;
            }

            BoundingBox bounds = World.ChunkManager.Bounds;
            Vector3 extents = bounds.Extents();
            Vector3 center = bounds.Center();
            Vector3 windNormalized = WindSpeed / WindSpeed.Length();
            Vector3 offset = new Vector3(-windNormalized.X * extents.X + center.X, bounds.Max.Y + 5, -windNormalized.Z * extents.Z + center.Z);
            Vector3 perp = new Vector3(-windNormalized.Z, 0, windNormalized.X);
            int numClouds = (int)(MathFunctions.RandInt(10, 100) * Intensity);

            for (int i = 0; i < numClouds; i++)
            {
                Vector3 cloudPos = offset + perp * 5 * (i - numClouds / 2) + MathFunctions.RandVector3Cube() * 10;

                Cloud cloud = new Cloud(World.ComponentManager, Intensity, 5, offset.Y + MathFunctions.Rand(-3.0f, 3.0f), cloudPos)
                {
                    Velocity = WindSpeed,
                    TypeofStorm = TypeofStorm
                };
                Clouds.Add(cloud);
                World.ComponentManager.RootComponent.AddChild(cloud);
            }
            IsInitialized = true;
        }
    }
}