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
    public static partial class Generator
    {
        //public ChunkGenerator(int randomSeed, float noiseScale, WorldGenerationSettings WorldGenerationSettings)
        //{
        //    Settings = new Generation.GeneratorSettings(randomSeed, noiseScale, WorldGenerationSettings);
        //}

        public static float NormalizeHeight(float height, float maxHeight, float upperBound = 0.9f)
        {
            return height + (upperBound - maxHeight);
        }
    }
}
