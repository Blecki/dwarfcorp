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

namespace DwarfCorp
{
    public partial class ChunkGenerator
    {
        // Todo: Why isn't this part of the chunk generator?
        public void GenerateOres(ChunkData ChunkData)
        {
            foreach (VoxelType type in VoxelLibrary.GetTypes())
            {
                if (type.SpawnClusters || type.SpawnVeins)
                {
                    BoundingBox clusterBounds = new BoundingBox
                    {
                        Max = new Vector3(ChunkData.MapOrigin.X + ChunkData.MapDimensions.X, type.MaxSpawnHeight, ChunkData.MapOrigin.Z + ChunkData.MapDimensions.Z),
                        Min = new Vector3(ChunkData.MapOrigin.X, Math.Max(type.MinSpawnHeight, 2), ChunkData.MapOrigin.Z)
                    };

                    int numEvents = (int)MathFunctions.Rand(75 * (1.0f - type.Rarity), 100 * (1.0f - type.Rarity));
                    for (int i = 0; i < numEvents; i++)
                    {
                        if (type.SpawnClusters)
                        {
                            OreCluster cluster = new OreCluster()
                            {
                                Size =
                                    new Vector3(MathFunctions.Rand(type.ClusterSize * 0.25f, type.ClusterSize),
                                        MathFunctions.Rand(type.ClusterSize * 0.25f, type.ClusterSize),
                                        MathFunctions.Rand(type.ClusterSize * 0.25f, type.ClusterSize)),
                                Transform = MathFunctions.RandomTransform(clusterBounds),
                                Type = type
                            };

                            Generation.Generator.GenerateCluster(cluster, ChunkData);
                        }

                        if (type.SpawnVeins)
                        {
                            OreVein vein = new OreVein()
                            {
                                Length = MathFunctions.Rand(type.VeinLength * 0.75f, type.VeinLength * 1.25f),
                                Start = MathFunctions.RandVector3Box(clusterBounds),
                                Type = type
                            };

                            Generation.Generator.GenerateVein(vein, ChunkData);
                        }
                    }
                }
            }
        }

        // Todo: Move to ChunkGenerator
        // Todo: Thread it, so that multiple cores can create larger worlds faster.
        public void GenerateInitialChunks(Rectangle spawnRect, ChunkData ChunkData, WorldManager World, Point3 WorldSizeInChunks, Action<String> SetLoadingMessage)
        {
            var initialChunkCoordinates = new List<GlobalChunkCoordinate>();

            for (int dx = 0; dx < WorldSizeInChunks.X; dx++)
                for (int dy = 0; dy < WorldSizeInChunks.Y; dy++)
                    for (int dz = 0; dz < WorldSizeInChunks.Z; dz++)
                        initialChunkCoordinates.Add(new GlobalChunkCoordinate(dx, dy, dz));

            float maxHeight = Math.Max(Overworld.GetMaxHeight(spawnRect), 0.17f);
            SetLoadingMessage(String.Format("{0} chunks to generate!", initialChunkCoordinates.Count));
            SetLoadingMessage("");
            foreach (var ID in initialChunkCoordinates)
            {
                SetLoadingMessage(String.Format("#Chunk {0} {1} {2}...", ID.X, ID.Y, ID.Z));
                ChunkData.AddChunk(GenerateChunk(ID, World, maxHeight, WorldSizeInChunks));
            }

            SetLoadingMessage("Cascading sunlight...");
            UpdateSunlight(World.ChunkManager, WorldSizeInChunks, Settings, SetLoadingMessage);

            SetLoadingMessage("Requiring minerals...");
            GenerateOres(ChunkData);

            SetLoadingMessage("Discovering lost civilizations...");
            Generation.Generator.GenerateRuins(ChunkData, World, Settings, WorldSizeInChunks);

            var worldDepth = WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY;
            var waterHeight = Math.Min((int)(worldDepth * NormalizeHeight(Settings.SeaLevel + 1.0f / worldDepth, maxHeight)), worldDepth - 1);

            // This is critical at the beginning to allow trees to spawn on ramps correctly,
            // and also to ensure no inconsistencies in chunk geometry due to ramps.
            foreach (var chunk in ChunkData.ChunkMap)
            {
                SetLoadingMessage(String.Format("#Exploring caves in chunk {0} {1} {2}...", chunk.ID.X, chunk.ID.Y, chunk.ID.Z));
                GenerateCaves(chunk, World);
                GenerateWater(chunk, waterHeight);
                GenerateLava(chunk);

                for (var i = 0; i < VoxelConstants.ChunkSizeY; ++i)
                    chunk.InvalidateSlice(i);
            }

            if (MathFunctions.RandEvent(0.01f))
                SetLoadingMessage("Spawning way to many rabbits...");
            else
                SetLoadingMessage("Spawning surface life...");
            Generation.Generator.GenerateSurfaceLife(World, World.ChunkManager, WorldSizeInChunks, maxHeight, Settings);
        }

        private static void UpdateSunlight(ChunkManager ChunkManager, Point3 WorldSize, Generation.GeneratorSettings Settings, Action<String> SetLoadingMessage)
        {
            var totalRays = WorldSize.X * WorldSize.Z * VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeZ;
            SetLoadingMessage(String.Format("{0} rays of sunshine to propogate.", totalRays));
            SetLoadingMessage("");
            for (var x = 0; x < WorldSize.X * VoxelConstants.ChunkSizeX; x++)
            {
                for (var z = 0; z < WorldSize.Z * VoxelConstants.ChunkSizeZ; z++)
                    for (var y = (WorldSize.Y * VoxelConstants.ChunkSizeY) - 1; y >= 0; y--)
                    {
                        var v = ChunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(x, y, z));
                        if (!v.IsValid) break;
                        v.Sunlight = true;
                        if (Settings.RevealSurface) v.RawSetIsExplored();
                        if (v.Type.ID != 0 && !v.Type.IsTransparent)
                            break;
                    }
                SetLoadingMessage(String.Format("#{0} of {1} rays...", (x + 1) * WorldSize.Z * VoxelConstants.ChunkSizeZ, totalRays));
            }
        }
    }
}
