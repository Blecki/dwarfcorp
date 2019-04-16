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
        public static float NormalizeHeight(float height, float maxHeight, float upperBound = 0.9f)
        {
            return height + (upperBound - maxHeight);
        }

        public static IEnumerable<VoxelChunk> EnumerateTopChunks(GeneratorSettings Settings)
        {
            return Settings.World.ChunkManager.ChunkData.GetChunkEnumerator().Where(c => c.ID.Y == Settings.WorldSizeInChunks.Y - 1);
        }
    }
}
