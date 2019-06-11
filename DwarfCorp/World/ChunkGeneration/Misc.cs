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
        public static float NormalizeHeight(float height)
        {
            return 0.25f + (height * 0.5f);
        }

        public static IEnumerable<VoxelChunk> EnumerateTopChunks(ChunkGeneratorSettings Settings)
        {
            return Settings.World.ChunkManager.GetChunkEnumerator().Where(c => c.ID.Y == Settings.WorldSizeInChunks.Y - 1);
        }
    }
}
