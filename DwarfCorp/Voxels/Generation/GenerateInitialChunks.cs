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
        public static void Generate(Rectangle spawnRect, ChunkData ChunkData, WorldManager World, GeneratorSettings Settings, Action<String> SetLoadingMessage)
        {
            var initialChunkCoordinates = new List<GlobalChunkCoordinate>();

            for (int dx = 0; dx < Settings.WorldSizeInChunks.X; dx++)
                for (int dy = 0; dy < Settings.WorldSizeInChunks.Y; dy++)
                    for (int dz = 0; dz < Settings.WorldSizeInChunks.Z; dz++)
                        initialChunkCoordinates.Add(new GlobalChunkCoordinate(dx, dy, dz));

            Settings.MaxHeight = Math.Max(Overworld.GetMaxHeight(spawnRect), 0.17f);
            SetLoadingMessage(String.Format("{0} chunks to generate!", initialChunkCoordinates.Count));
            SetLoadingMessage("");
            foreach (var ID in initialChunkCoordinates)
            {
                SetLoadingMessage(String.Format("#Chunk {0} {1} {2}...", ID.X, ID.Y, ID.Z));
                ChunkData.AddChunk(GenerateChunk(ID, Settings));
            }

            SetLoadingMessage("Cascading sunlight...");
            Generation.Generator.CastSunlight(World.ChunkManager, Settings);

            SetLoadingMessage("Requiring minerals...");
            Generation.Generator.GenerateOres(ChunkData);

            SetLoadingMessage("Discovering lost civilizations...");
            Generation.Generator.GenerateRuins(ChunkData, World, Settings, Settings.WorldSizeInChunks);

            var worldDepth = Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY;
            var waterHeight = Math.Min((int)(worldDepth * NormalizeHeight(Settings.SeaLevel + 1.0f / worldDepth, Settings.MaxHeight)), worldDepth - 1);

            // This is critical at the beginning to allow trees to spawn on ramps correctly,
            // and also to ensure no inconsistencies in chunk geometry due to ramps.
            foreach (var chunk in ChunkData.ChunkMap)
            {
                SetLoadingMessage(String.Format("#Exploring caves in chunk {0} {1} {2}...", chunk.ID.X, chunk.ID.Y, chunk.ID.Z));
                Generation.Generator.GenerateCaves(chunk, Settings);
                Generation.Generator.GenerateWater(chunk, waterHeight);
                Generation.Generator.GenerateLava(chunk, Settings);

                for (var i = 0; i < VoxelConstants.ChunkSizeY; ++i)
                    chunk.InvalidateSlice(i);
            }

            if (MathFunctions.RandEvent(0.01f))
                SetLoadingMessage("Spawning way to many rabbits...");
            else
                SetLoadingMessage("Spawning surface life...");
            Generation.Generator.GenerateSurfaceLife(Settings);
        }        
    }
}
