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
    public class GeneratorSettings
    {
        public Perlin NoiseGenerator;
        public LibNoise.FastRidgedMultifractal CaveNoise;
        public float NoiseScale;
        public float CaveNoiseScale;
        public float SeaLevel;

        public List<float> CaveFrequencies;
        public float CaveSize;
        public int HellLevel = 10;
        public int LavaLevel = 5;

        public List<int> CaveLevels = null;

        public GeneratorSettings(int randomSeed, float noiseScale, WorldGenerationSettings WorldGenerationSettings)
        {
            NoiseGenerator = new Perlin(randomSeed);
            NoiseScale = noiseScale;

            CaveNoiseScale = noiseScale * 10.0f;
            CaveSize = 0.03f;
            CaveFrequencies = new List<float>() { 0.5f, 0.7f, 0.9f, 1.0f };

            CaveNoise = new FastRidgedMultifractal(randomSeed)
            {
                Frequency = 0.5f,
                Lacunarity = 0.5f,
                NoiseQuality = NoiseQuality.Standard,
                OctaveCount = 1,
                Seed = randomSeed
            };

            CaveLevels = new List<int>();
            var caveStep = 48 / WorldGenerationSettings.NumCaveLayers;

            for (var i = 0; i < WorldGenerationSettings.NumCaveLayers; ++i)
                CaveLevels.Add(4 + (caveStep * i));
        }
    }
}
