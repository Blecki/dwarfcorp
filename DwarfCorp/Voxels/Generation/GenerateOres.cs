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
        public static void _GenerateOres(VoxelChunk Chunk, GeneratorSettings Settings)
        {
            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    var overworldPosition = Overworld.WorldToOverworld(new Vector2(x + Chunk.Origin.X, z + Chunk.Origin.Z), Settings.World.WorldScale, Settings.World.WorldOrigin);

                    var normalizedHeight = NormalizeHeight(Overworld.LinearInterpolate(overworldPosition, Overworld.Map, Overworld.ScalarFieldType.Height), Settings.MaxHeight);
                    var height = MathFunctions.Clamp(normalizedHeight * Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY, 0.0f, Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY - 2);

                    for (int y = 0; y < VoxelConstants.ChunkSizeY; y++)
                    {
                        if (Chunk.Origin.Y + y > height) break;
                        if (Chunk.Origin.Y + y == 0) continue;

                        foreach (var voxelType in VoxelLibrary.GetTypes())
                        {
                            if (voxelType.SpawnClusters || voxelType.SpawnVeins) // Todo: Just use one or the other.
                            {
                                if (Chunk.Origin.Y + y < voxelType.MinSpawnHeight) continue;
                                if (Chunk.Origin.Y + y > voxelType.MaxSpawnHeight) continue;

                                var vRand = new Random(voxelType.ID);
                                var noiseVector = new Vector3(Chunk.Origin.X + x, Chunk.Origin.Y + y, Chunk.Origin.Z + z) * Settings.CaveNoiseScale * 2.0f;
                                noiseVector += new Vector3(vRand.Next(0, 64), vRand.Next(0, 64), vRand.Next(0, 64));

                                var fade = 1.0f - ((Chunk.Origin.Y + y - voxelType.MinSpawnHeight) / voxelType.MaxSpawnHeight);

                                var oreNoise = Settings.CaveNoise.GetValue(noiseVector.X, noiseVector.Y, noiseVector.Z);

                                if (Math.Abs(oreNoise) < voxelType.Rarity * fade)
                                    Chunk.Manager.CreateVoxelHandle(new GlobalVoxelCoordinate(Chunk.Origin.X + x, Chunk.Origin.Y + y, Chunk.Origin.Z + z)).RawSetType(voxelType);
                            }
                        }
                    }
                }
            }
        }

        public static  void GenerateOres(ChunkData ChunkData)
        {
            // This needs to be changed to a method to determine if any particular voxel should be grouped into an ore cluster. 

            foreach (VoxelType type in VoxelLibrary.GetTypes())
            {
                if (type.SpawnClusters || type.SpawnVeins)
                {
                    BoundingBox clusterBounds = new BoundingBox
                    {
                        Max = new Vector3(ChunkData.MapOrigin.X + ChunkData.MapDimensions.X, type.MaxSpawnHeight, ChunkData.MapOrigin.Z + ChunkData.MapDimensions.Z),
                        Min = new Vector3(ChunkData.MapOrigin.X, Math.Max(type.MinSpawnHeight, 2), ChunkData.MapOrigin.Z)
                    };

                    // Rarity is an inverse, but the max for any type is 100...
                    int numEvents = (int)MathFunctions.Rand(75 * (1.0f - type.Rarity), 100 * (1.0f - type.Rarity)); // Todo: Jesus christ, larger worlds don't have any more resources than small ones!
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

                            GenerateCluster(cluster, ChunkData);
                        }

                        if (type.SpawnVeins)
                        {
                            OreVein vein = new OreVein()
                            {
                                Length = MathFunctions.Rand(type.VeinLength * 0.75f, type.VeinLength * 1.25f),
                                Start = MathFunctions.RandVector3Box(clusterBounds),
                                Type = type
                            };

                            GenerateVein(vein, ChunkData);
                        }
                    }
                }
            }
        }
    }
}
