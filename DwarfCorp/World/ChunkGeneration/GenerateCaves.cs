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
    public partial class Generator
    {
        private struct CaveGenerationData
        {
            public bool CaveHere;
            public double Noise;
            public int Height;
        }

        private static CaveGenerationData GetCaveGenerationData(GlobalVoxelCoordinate At, int LayerIndex, ChunkGeneratorSettings Settings)
        {
            var frequency = LayerIndex < Settings.CaveFrequencies.Count ? Settings.CaveFrequencies[LayerIndex] : Settings.CaveFrequencies[Settings.CaveFrequencies.Count - 1];

            var noiseVector = At.ToVector3() * Settings.CaveNoiseScale * new Vector3(frequency, 3.0f, frequency);
            var noise = Settings.CaveNoise.GetValue(noiseVector.X, noiseVector.Y, noiseVector.Z);

            var heightVector = At.ToVector3() * Settings.NoiseScale * new Vector3(frequency, 3.0f, frequency);
            var height = Settings.NoiseGenerator.Noise(heightVector);

            return new CaveGenerationData
            {
                CaveHere = noise > Settings.CaveSize,
                Noise = noise,
                Height = Math.Min(Math.Max((int)(height * Settings.CaveHeightScaleFactor), 1), Settings.MaxCaveHeight)
            };
        }

        public static void GenerateCaves(VoxelChunk Chunk, ChunkGeneratorSettings Settings)
        {
            var caveBiome = Library.GetBiome("Cave");
            var hellBiome = Library.GetBiome("Hell");

            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    for (int i = 0; i < Settings.CaveLevels.Count; i++)
                    {
                        // Does layer intersect this voxel?
                        int y = Settings.CaveLevels[i];
                        if (y + Settings.MaxCaveHeight < Chunk.Origin.Y) continue;
                        if (y >= Chunk.Origin.Y + VoxelConstants.ChunkSizeY) continue;

                        var coordinate = new GlobalVoxelCoordinate(Chunk.Origin.X + x, y, Chunk.Origin.Z + z);

                        var data = GetCaveGenerationData(coordinate, i, Settings);

                        var biome = (y <= Settings.HellLevel) ? hellBiome : caveBiome;

                        if (!data.CaveHere) continue;

                        for (int dy = 0; dy < data.Height; dy++)
                        {
                            var globalY = y + dy;

                            // Prevent caves punching holes in bedrock.
                            if (globalY <= 0) continue;

                            // Check if voxel is inside chunk.
                            if (globalY <= 0 || globalY < Chunk.Origin.Y || globalY >= Chunk.Origin.Y + VoxelConstants.ChunkSizeY) continue;

                            var voxel = VoxelHandle.UnsafeCreateLocalHandle(Chunk, new LocalVoxelCoordinate(x, globalY - Chunk.Origin.Y, z));

                            // Prevent caves from breaking surface.
                            bool caveBreaksSurface = false;

                            foreach (var neighborCoordinate in VoxelHelpers.EnumerateAllNeighbors(voxel.Coordinate))
                            {
                                var v = Chunk.Manager.CreateVoxelHandle(neighborCoordinate);
                                if (!v.IsValid || (v.Sunlight))
                                {
                                    caveBreaksSurface = true;
                                    break;
                                }
                            }

                            if (caveBreaksSurface)
                                break;

                            voxel.RawSetType(Library.EmptyVoxelType);

                            if (dy == 0)
                            {
                                // Place soil voxel and grass below cave.
                                var below = VoxelHelpers.GetVoxelBelow(voxel);
                                if (below.IsValid)
                                {
                                    below.RawSetType(Library.GetVoxelType(biome.SoilLayer.VoxelType));
                                    var grassType = Library.GetGrassType(biome.GrassDecal);
                                    if (grassType != null)
                                        below.RawSetGrass(grassType.ID);
                                }

                                // Spawn flora and fauna.
                                if (data.Noise > Settings.CaveSize * 1.8f && globalY > Settings.LavaLevel)
                                {
                                    GenerateCaveFlora(below, biome, Settings);
                                    GenerateCaveFauna(below, biome, Settings);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void GenerateCaveFlora(VoxelHandle CaveFloor, BiomeData Biome, ChunkGeneratorSettings Settings)
        {
            foreach (var floraType in Biome.Vegetation)
            {
                if (!MathFunctions.RandEvent(floraType.SpawnProbability))
                    continue;

                if (Settings.NoiseGenerator.Noise(CaveFloor.Coordinate.X / floraType.ClumpSize, floraType.NoiseOffset, CaveFloor.Coordinate.Z / floraType.ClumpSize) < floraType.ClumpThreshold)
                    continue;

                var plantSize = MathFunctions.Rand() * floraType.SizeVariance + floraType.MeanSize;
                var lambdaFloraType = floraType;

                    if (!GameSettings.Default.FogofWar)
                        EntityFactory.CreateEntity<GameComponent>(
                            lambdaFloraType.Name,
                            CaveFloor.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f),
                            Blackboard.Create("Scale", plantSize));
                    else
                        Settings.World.ComponentManager.RootComponent.AddChild(new SpawnOnExploredTrigger(Settings.World.ComponentManager, CaveFloor)
                        {
                            EntityToSpawn = lambdaFloraType.Name,
                            SpawnLocation = CaveFloor.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f),
                            BlackboardData = Blackboard.Create("Scale", plantSize)
                        });

                break; // Don't risk spawning multiple plants in the same spot.
            }
        }

        public static void GenerateCaveFauna(VoxelHandle CaveFloor, BiomeData Biome, ChunkGeneratorSettings Settings)
        {
            var spawnLikelihood = (Settings.Overworld.Difficulty + 0.1f);

            foreach (var animalType in Biome.Fauna)
            {
                if (!(MathFunctions.Random.NextDouble() < animalType.SpawnProbability * spawnLikelihood))
                    continue;

                var lambdaAnimalType = animalType;

                    if (!GameSettings.Default.FogofWar)
                        EntityFactory.CreateEntity<GameComponent>(lambdaAnimalType.Name, CaveFloor.WorldPosition + new Vector3(0.5f, 1.5f, 0.5f));
                    else
                        Settings.World.ComponentManager.RootComponent.AddChild(new SpawnOnExploredTrigger(Settings.World.ComponentManager, CaveFloor)
                        {
                            EntityToSpawn = lambdaAnimalType.Name,
                            SpawnLocation = CaveFloor.WorldPosition + new Vector3(0.5f, 1.5f, 0.5f)
                        });

                break; // Prevent spawning multiple animals in same spot.
            }
        }

    }
}