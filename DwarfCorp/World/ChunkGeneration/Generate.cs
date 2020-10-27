using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp.Generation
{
    public static partial class Generator
    {
        public static void Generate(ChunkManager ChunkData, WorldManager World, ChunkGeneratorSettings Settings, Action<String> SetLoadingMessage)
        {
            SetLoadingMessage(String.Format("{0} chunks to generate!", Settings.WorldSizeInChunks.X * Settings.WorldSizeInChunks.Y * Settings.WorldSizeInChunks.Z));
            SetLoadingMessage("");

            for (int dx = 0; dx < Settings.WorldSizeInChunks.X; dx++)
                for (int dy = 0; dy < Settings.WorldSizeInChunks.Y; dy++)
                    for (int dz = 0; dz < Settings.WorldSizeInChunks.Z; dz++)
                    {
                        SetLoadingMessage(String.Format("#Generating chunk {0} {1} {2}...", dx, dy, dz));
                        ChunkData.AddChunk(GenerateChunk(new GlobalChunkCoordinate(dx, dy, dz), Settings));
                    }

            var worldDepth = Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY;
            Settings.NormalizedSeaLevel = Math.Min((int)(worldDepth * NormalizeHeight(Settings.Overworld.GenerationSettings.SeaLevel + 1.0f / worldDepth)), worldDepth - 1);

            SetLoadingMessage("");

            foreach (var chunk in EnumerateTopChunks(Settings))
            {
                SetLoadingMessage(String.Format("#Casting light in chunk {0} {1} {2}...", chunk.ID.X, chunk.ID.Y, chunk.ID.Z));
                CastSunlight(chunk, Settings);
                GenerateRuin(chunk, Settings);
            }

            // Todo: Can these be sped up by doing multiple in one iteration of the outer x,z?
            if (!GameSettings.Current.FastGen)
            {
                SetLoadingMessage("");
                foreach (var chunk in ChunkData.ChunkMap)
                {
                    SetLoadingMessage(String.Format("#Generating caves, ore, water in chunk {0} {1} {2}...", chunk.ID.X, chunk.ID.Y, chunk.ID.Z));
                    GenerateOres(chunk, Settings);
                    GenerateCaves(chunk, Settings);
                    GenerateWater(chunk, Settings);
                    GenerateLava(chunk, Settings);
                }
            }

            SetLoadingMessage("");
            foreach (var chunk in EnumerateTopChunks(Settings))
            {
                SetLoadingMessage(String.Format("#Spawning life in chunk {0} {1} {2}...", chunk.ID.X, chunk.ID.Y, chunk.ID.Z));
                GenerateSurfaceLife(chunk, Settings);
                ChunkData.EnqueueInvalidColumn(chunk.ID.X, chunk.ID.Z);
            }
        }

        public static void GenerateDebug(ChunkManager ChunkData, WorldManager World, ChunkGeneratorSettings Settings, Action<String> SetLoadingMessage)
        {
            SetLoadingMessage(String.Format("{0} chunks to generate!", Settings.WorldSizeInChunks.X * Settings.WorldSizeInChunks.Y * Settings.WorldSizeInChunks.Z));
            SetLoadingMessage("");

            for (int dx = 0; dx < Settings.WorldSizeInChunks.X; dx++)
                for (int dy = 0; dy < Settings.WorldSizeInChunks.Y; dy++)
                    for (int dz = 0; dz < Settings.WorldSizeInChunks.Z; dz++)
                    {
                        SetLoadingMessage(String.Format("#Generating chunk {0} {1} {2}...", dx, dy, dz));
                        ChunkData.AddChunk(GenerateDebugChunk(new GlobalChunkCoordinate(dx, dy, dz), Settings));
                    }

            Settings.NormalizedSeaLevel = 0;

            foreach (var chunk in EnumerateTopChunks(Settings))
            {
                SetLoadingMessage(String.Format("#Casting light in chunk {0} {1} {2}...", chunk.ID.X, chunk.ID.Y, chunk.ID.Z));
                CastSunlight(chunk, Settings);
            }
        }
    }
}
