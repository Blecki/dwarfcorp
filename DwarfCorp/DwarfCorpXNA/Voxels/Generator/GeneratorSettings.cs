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
        public LibNoise.FastRidgedMultifractal AquiferNoise;

        public List<float> CaveFrequencies;
        public List<int> AquiferLevels;
        public float CaveSize;
        public float AquiferSize;
        public int HellLevel = 10;
        public int LavaLevel = 5;

        public List<int> CaveLevels = null;
    }
}
