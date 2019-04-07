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
        public void GenerateOres(ChunkData ChunkData, BoundingBox Bounds)
        {
            foreach (VoxelType type in VoxelLibrary.GetTypes())
            {
                if (type.SpawnClusters || type.SpawnVeins)
                {
                    int numEvents = (int)MathFunctions.Rand(75 * (1.0f - type.Rarity), 100 * (1.0f - type.Rarity));
                    for (int i = 0; i < numEvents; i++)
                    {
                        BoundingBox clusterBounds = new BoundingBox
                        {
                            Max = new Vector3(Bounds.Max.X, type.MaxSpawnHeight, Bounds.Max.Z),
                            Min = new Vector3(Bounds.Min.X, Math.Max(type.MinSpawnHeight, 2), Bounds.Min.Z)
                        };

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
        public void GenerateInitialChunks(Rectangle spawnRect, ChunkData ChunkData, WorldManager World, Point3 WorldSize, BoundingBox Bounds)
        {
            var initialChunkCoordinates = new List<GlobalChunkCoordinate>();

            for (int dx = 0; dx < WorldSize.X; dx++)
                for (int dz = 0; dz < WorldSize.Z; dz++)
                    initialChunkCoordinates.Add(new GlobalChunkCoordinate(dx, 0, dz)); // Todo: Add Z dimension here.

            float maxHeight = Math.Max(Overworld.GetMaxHeight(spawnRect), 0.17f);
            foreach (var ID in initialChunkCoordinates)
                ChunkData.AddChunk(GenerateChunk(ID, World, maxHeight));

            UpdateSunlight(World.ChunkManager, WorldSize);
            GenerateOres(ChunkData, Bounds);
            Generation.Generator.GenerateRuins(ChunkData, World, Settings);

            // This is critical at the beginning to allow trees to spawn on ramps correctly,
            // and also to ensure no inconsistencies in chunk geometry due to ramps.
            foreach (var chunk in ChunkData.ChunkMap)
            {
                GenerateCaves(chunk, World);
                GenerateWater(chunk, maxHeight);
                GenerateLava(chunk);

                for (var i = 0; i < VoxelConstants.ChunkSizeY; ++i)
                    chunk.InvalidateSlice(i);
            }
        }

        private static void UpdateSunlight(ChunkManager ChunkManager, Point3 WorldSize)
        {
            for (var x = 0; x < WorldSize.X * VoxelConstants.ChunkSizeX; x++)
                for (var z = 0; z < WorldSize.Z * VoxelConstants.ChunkSizeZ; z++)
                    for (var y = VoxelConstants.WorldSizeY - 1; y >= 0; y--)
                    {
                        var v = ChunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(x, y, z));
                        if (!v.IsValid) break;
                        v.Sunlight = true;
                        if (v.Type.ID != 0 && !v.Type.IsTransparent)
                            break;
                    }
        }
    }
}
