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
        public static void GenerateSurfaceLife(WorldManager World, ChunkManager ChunkManager, Point3 WorldSize, float maxHeight, GeneratorSettings Settings)
        {
            var creatureCounts = new Dictionary<string, Dictionary<string, int>>();
            var worldDepth = WorldSize.Y * VoxelConstants.ChunkSizeY;

            for (var x = 0; x < WorldSize.X * VoxelConstants.ChunkSizeX; x++)
                for (var z = 0; z < WorldSize.Z * VoxelConstants.ChunkSizeZ; z++)
                {
                    var overworldPosition = Overworld.WorldToOverworld(new Vector2(x, z), World.WorldScale, World.WorldOrigin);
                    var biome = Overworld.Map[(int)MathFunctions.Clamp(overworldPosition.X, 0, Overworld.Map.GetLength(0) - 1), (int)MathFunctions.Clamp(overworldPosition.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;
                    var biomeData = BiomeLibrary.Biomes[biome];

                    var normalizedHeight = ChunkGenerator.NormalizeHeight(Overworld.LinearInterpolate(overworldPosition, Overworld.Map, Overworld.ScalarFieldType.Height), maxHeight);
                    var height = (int)MathFunctions.Clamp(normalizedHeight * worldDepth, 0.0f, worldDepth - 2);

                    var voxel = ChunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(x, height, z));

                    if (!voxel.IsValid
                        || voxel.Coordinate.Y == 0
                        || voxel.Coordinate.Y >= worldDepth - Settings.TreeLine)
                        continue;

                    if (voxel.LiquidLevel != 0)
                        continue;

                    var above = VoxelHelpers.GetVoxelAbove(voxel);
                    if (above.IsValid && above.LiquidLevel != 0)
                        continue;

                    foreach (var animal in biomeData.Fauna)
                    {
                        if (MathFunctions.RandEvent(animal.SpawnProbability))
                        {
                            if (!creatureCounts.ContainsKey(biomeData.Name))
                            {
                                creatureCounts[biomeData.Name] = new Dictionary<string, int>();
                            }
                            var dict = creatureCounts[biomeData.Name];
                            if (!dict.ContainsKey(animal.Name))
                            {
                                dict[animal.Name] = 0;
                            }
                            if (dict[animal.Name] < animal.MaxPopulation)
                            {
                                if (Settings.RevealSurface)
                                {
                                    EntityFactory.CreateEntity<Body>(animal.Name,
                                        voxel.WorldPosition + Vector3.Up * 1.5f);
                                }
                                else
                                {
                                    var lambdaAnimal = animal;
                                    World.ComponentManager.RootComponent.AddChild(new ExploredListener(World.ComponentManager, voxel)
                                    {
                                        EntityToSpawn = lambdaAnimal.Name,
                                        SpawnLocation = voxel.WorldPosition + new Vector3(0.5f, 1.5f, 0.5f)
                                    });
                                }
                            }
                            break;
                        }
                    }

                    if (voxel.Type.Name != biomeData.SoilLayer.VoxelType)
                        continue;

                    foreach (VegetationData veg in biomeData.Vegetation)
                    {
                        if (voxel.GrassType == 0)
                            continue;

                        if (MathFunctions.RandEvent(veg.SpawnProbability) &&
                            Settings.NoiseGenerator.Noise(voxel.Coordinate.X / veg.ClumpSize,
                            veg.NoiseOffset, voxel.Coordinate.Z / veg.ClumpSize) >= veg.ClumpThreshold)
                        {
                            voxel.RawSetType(VoxelLibrary.GetVoxelType(biomeData.SoilLayer.VoxelType));

                            var treeSize = MathFunctions.Rand() * veg.SizeVariance + veg.MeanSize;
                            if (Settings.RevealSurface)
                            {
                                EntityFactory.CreateEntity<Plant>(veg.Name,
                                voxel.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f),
                                Blackboard.Create("Scale", treeSize));
                            }
                            else
                            {
                                var lambdaFloraType = veg;
                                World.ComponentManager.RootComponent.AddChild(new ExploredListener(World.ComponentManager, voxel)
                                {
                                    EntityToSpawn = lambdaFloraType.Name,
                                    SpawnLocation = voxel.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f),
                                    BlackboardData = Blackboard.Create("Scale", treeSize)
                                });
                            }

                            break;
                        }
                    }
                }
        }
    }
}