using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static List<BiomeData> Biomes = null;
        private static bool BiomesInitialized = false;

        private static void InitializeBiomes()
        {
            if (BiomesInitialized)
                return;
            BiomesInitialized = true;

            Biomes = FileUtils.LoadJsonListFromDirectory<BiomeData>(ContentPaths.World.biomes, null, b => b.Name);

            byte id = 0;
            foreach (var biome in Biomes)
            {
                biome.Biome = id;
                id++;
            }

            Console.WriteLine("Loaded Biome Library.");
        }

        public static Dictionary<string, Color> CreateBiomeColors()
        {
            InitializeBiomes();

            Dictionary<string, Color> toReturn = new Dictionary<string, Color>();
            foreach (var biome in Biomes)
                toReturn[biome.Name] = biome.MapColor;
            return toReturn;
        }

        public static MaybeNull<BiomeData> GetBiome(String Name)
        {
            InitializeBiomes();
            return Biomes.FirstOrDefault(b => b.Name == Name);
        }

        public static BiomeData GetBiome(int Index)
        {
            InitializeBiomes();
            if (Index < 0 || Index >= Biomes.Count)
                return null;
            return Biomes[Index];
        }

        public static BiomeData GetBiomeForConditions(float Temperature, float Rainfall, float Elevation)
        {
            InitializeBiomes();

            BiomeData closest = null;
            var closestDist = float.MaxValue;

            foreach (var biome in Library.Biomes)
            {
                var dist = Math.Abs(biome.Temp - Temperature) + Math.Abs(biome.Rain - Rainfall) + Math.Abs(biome.Height - Elevation);

                if (dist < closestDist)
                {
                    closest = biome;
                    closestDist = dist;
                }
            }

            return closest;
        }

        public static Dictionary<int, String> GetBiomeTypeMap()
        {
            InitializeBiomes();

            var r = new Dictionary<int, String>();
            for (var i = 0; i < Biomes.Count; ++i)
                r.Add(i, Biomes[i].Name);
            return r;
        }
    }

}