using System;
using System.Collections.Generic;
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using Newtonsoft.Json.Schema;
using Math = System.Math;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public static class DebugOverworlds
    {
        private static float ComputeHeight(float wx, float wy, float worldWidth, float worldHeight, float globalScale)
        {
            float x = (wx) / globalScale;
            float y = (wy) / globalScale;

            const float mountainWidth = 0.04f;
            float mountain = (float)Math.Pow(OverworldImageOperations.noise(Overworld.heightNoise, x, y, 0, mountainWidth), 1);
            const float continentSize = 0.03f;
            float continent = OverworldImageOperations.noise(Overworld.heightNoise, x, y, 10, continentSize);
            const float hillSize = 0.1f;
            float hill = OverworldImageOperations.noise(Overworld.heightNoise, x, y, 20, hillSize) * 0.02f;
            const float smallNoiseSize = 0.15f;
            float smallnoise = OverworldImageOperations.noise(Overworld.heightNoise, x, y, 100, smallNoiseSize) * 0.01f;

            var h = Math.Max(Math.Min((continent * mountain) + hill, 1), 0);
            h += smallnoise;
            h += 0.4f;
            h = Math.Max(Math.Min(h, 1), 0);
            return h;
        }

        public static Overworld CreateHillsLand()
        {
            GameStates.GameState.Game.LogSentryBreadcrumb("Overworld", "User created a hills world.");
            int size = 512;

            var r = new Overworld(size, size);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float temp = ComputeHeight(x, y, size, size, 3.0f);
                    float rain = ComputeHeight(x, y, size, size, 2.0f);
                    float height = ComputeHeight(x, y, size, size, 1.6f);
                    r.Map[x, y].Erosion = 1.0f;
                    r.Map[x, y].Weathering = 0;
                    r.Map[x, y].Faults = 1.0f;
                    r.Map[x, y].Temperature = (float)(temp * 1.0f);
                    r.Map[x, y].Rainfall = (float)(rain * 1.0f);
                    r.Map[x, y].Biome = Overworld.GetBiome(temp, rain, height).Biome;
                    r.Map[x, y].Height = height;
                }
            }

            r.Name = "hills" + MathFunctions.Random.Next(9999);

            return r;
        }

        public static Overworld CreateCliffsLand()
        {
            GameStates.GameState.Game.LogSentryBreadcrumb("Overworld", "User created a cliffs world.");
            int size = 512;

            var r = new Overworld(size, size);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float height = ComputeHeight(x * 1.0f, y * 2.0f, size, size, 1.0f);
                    float level = (int)(height/0.15f) * 0.15f + 0.08f;


                    r.Map[x, y].Height = level;
                    r.Map[x, y].Biome = BiomeLibrary.GetBiome("Deciduous Forest").Biome;
                    r.Map[x, y].Erosion = 1.0f;
                    r.Map[x, y].Weathering = 0;
                    r.Map[x, y].Faults = 1.0f;
                    r.Map[x, y].Temperature = 0.6f;
                    r.Map[x, y].Rainfall = 0.6f;
                }
            }

            r.Name = "Cliffs_" + MathFunctions.Random.Next(9999);

            return r;
        }

        public static Overworld CreateUniformLand()
        {
            GameStates.GameState.Game.LogSentryBreadcrumb("Overworld", "User created a flat world.");
            int size = 512;

            var r = new Overworld(size, size);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    r.Map[x, y].Biome = BiomeLibrary.GetBiome("Desert").Biome;
                    r.Map[x, y].Erosion = 1.0f;
                    r.Map[x, y].Weathering = 0.0f;
                    r.Map[x, y].Faults = 1.0f;
                    r.Map[x, y].Temperature = size;
                    r.Map[x, y].Rainfall = size;
                    r.Map[x, y].Height = 0.3f; //ComputeHeight(x, y, size0, size0, 5.0f, false);
                }
            }

            r.Name = "flat_" + MathFunctions.Random.Next(9999);

            return r;
        }

        public static Overworld CreateOceanLand(float seaLevel)
        {
            GameStates.GameState.Game.LogSentryBreadcrumb("Overworld", "User created an ocean world.");
            int size = 512;

            var r = new Overworld(size, size);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    r.Map[x, y].Biome = BiomeLibrary.GetBiome("Grassland").Biome;
                    r.Map[x, y].Erosion = 1.0f;
                    r.Map[x, y].Weathering = 0;
                    r.Map[x, y].Faults = 1.0f;
                    r.Map[x, y].Temperature = size;
                    r.Map[x, y].Rainfall = size;
                    r.Map[x, y].Height = 0.05f; //ComputeHeight(x, y, size0, size0, 5.0f, false);
                }
            }

            r.Name = "ocean_" + MathFunctions.Random.Next(9999);

            return r;
        }
    }
}
