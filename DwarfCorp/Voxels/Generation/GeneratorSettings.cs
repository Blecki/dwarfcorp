using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DwarfCorp.GameStates;
using LibNoise;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Math = System.Math;

namespace DwarfCorp.Generation
{
    // Todo: Why are these different settings from WorldGenerationSettings?
    public class GeneratorSettings
    {
        public OverworldGenerationSettings OverworldSettings;
        public Action<String> SetLoadingMessage;

        public Perlin NoiseGenerator;
        public LibNoise.FastRidgedMultifractal CaveNoise;
        public float NoiseScale;
        public float CaveNoiseScale;
        public float SeaLevel;

        public Point3 WorldSizeInChunks;

        public List<float> CaveFrequencies;
        public float CaveSize;
        public int HellLevel = 10;
        public int LavaLevel = 5;
        public int TreeLine = 8;

        public List<int> CaveLevels = null;
        public int CaveHeightScaleFactor = 5;
        public int MaxCaveHeight = 3;

        public WorldManager World;
        public float MaxHeight = 0.0f;

        public GeneratorSettings(int Seed, float NoiseScale, OverworldGenerationSettings OverworldSettings)
        {
            this.OverworldSettings = OverworldSettings;

            NoiseGenerator = new Perlin(Seed);
            this.NoiseScale = NoiseScale;

            CaveNoiseScale = NoiseScale * 10.0f;
            CaveSize = 0.03f;
            CaveFrequencies = new List<float>() { 0.5f, 0.7f, 0.9f, 1.0f };

            CaveNoise = new FastRidgedMultifractal(Seed)
            {
                Frequency = 0.5f,
                Lacunarity = 0.5f,
                NoiseQuality = NoiseQuality.Standard,
                OctaveCount = 1,
                Seed = Seed
            };

            CaveLevels = new List<int>();
            var caveStep = 48 / OverworldSettings.NumCaveLayers;

            for (var i = 0; i < OverworldSettings.NumCaveLayers; ++i)
                CaveLevels.Add(4 + (caveStep * i));
        }
    }
}
