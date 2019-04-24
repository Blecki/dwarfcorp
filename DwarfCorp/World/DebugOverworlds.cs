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
        public static void CreateHillsLand()
        {
            GameStates.GameState.Game.LogSentryBreadcrumb("Overworld", "User created a hills world.");
            int size = 512;
            Overworld.Map = new OverworldCell[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float temp = Overworld.ComputeHeight(x, y, size, size, 3.0f, false);
                    float rain = Overworld.ComputeHeight(x, y, size, size, 2.0f, false);
                    float height = Overworld.ComputeHeight(x, y, size, size, 1.6f, false);
                    Overworld.Map[x, y].Erosion = 1.0f;
                    Overworld.Map[x, y].Weathering = 0;
                    Overworld.Map[x, y].Faults = 1.0f;
                    Overworld.Map[x, y].Temperature = (float)(temp * 1.0f);
                    Overworld.Map[x, y].Rainfall = (float)(rain * 1.0f);
                    Overworld.Map[x, y].Biome = Overworld.GetBiome(temp, rain, height).Biome;
                    Overworld.Map[x, y].Height = height;
                }
            }

            Overworld.Name = "hills" + MathFunctions.Random.Next(9999);
        }

        public static void CreateCliffsLand()
        {
            GameStates.GameState.Game.LogSentryBreadcrumb("Overworld", "User created a cliffs world.");
            int size = 512;
            Overworld.Map = new OverworldCell[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float height = Overworld.ComputeHeight(x * 1.0f, y * 2.0f, size, size, 1.0f, false);
                    float level = (int)(height/0.15f) * 0.15f + 0.08f;


                    Overworld.Map[x, y].Height = level;
                    Overworld.Map[x, y].Biome = BiomeLibrary.GetBiome("Deciduous Forest").Biome;
                    Overworld.Map[x, y].Erosion = 1.0f;
                    Overworld.Map[x, y].Weathering = 0;
                    Overworld.Map[x, y].Faults = 1.0f;
                    Overworld.Map[x, y].Temperature = 0.6f;
                    Overworld.Map[x, y].Rainfall = 0.6f;
                }
            }

            Overworld.Name = "Cliffs_" + MathFunctions.Random.Next(9999);
        }

        public static void CreateUniformLand()
        {
            GameStates.GameState.Game.LogSentryBreadcrumb("Overworld", "User created a flat world.");
            int size = 512;
            Overworld.Map = new OverworldCell[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Overworld.Map[x, y].Biome = BiomeLibrary.GetBiome("Desert").Biome;
                    Overworld.Map[x, y].Erosion = 1.0f;
                    Overworld.Map[x, y].Weathering = 0.0f;
                    Overworld.Map[x, y].Faults = 1.0f;
                    Overworld.Map[x, y].Temperature = size;
                    Overworld.Map[x, y].Rainfall = size;
                    Overworld.Map[x, y].Height = 0.3f; //ComputeHeight(x, y, size0, size0, 5.0f, false);
                }
            }

            Overworld.Name = "flat_" + MathFunctions.Random.Next(9999);
        }

        public static void CreateOceanLand(float seaLevel)
        {
            GameStates.GameState.Game.LogSentryBreadcrumb("Overworld", "User created an ocean world.");
            int size = 512;
            Overworld.Map = new OverworldCell[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Overworld.Map[x, y].Biome = BiomeLibrary.GetBiome("Grassland").Biome;
                    Overworld.Map[x, y].Erosion = 1.0f;
                    Overworld.Map[x, y].Weathering = 0;
                    Overworld.Map[x, y].Faults = 1.0f;
                    Overworld.Map[x, y].Temperature = size;
                    Overworld.Map[x, y].Rainfall = size;
                    Overworld.Map[x, y].Height = 0.05f; //ComputeHeight(x, y, size0, size0, 5.0f, false);
                }
            }

            Overworld.Name = "ocean_" + MathFunctions.Random.Next(9999);
        }
    }
}
