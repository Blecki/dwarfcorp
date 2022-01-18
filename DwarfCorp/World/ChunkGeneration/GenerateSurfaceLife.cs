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
        public static void GenerateSurfaceLife(VoxelChunk TopChunk, ChunkGeneratorSettings Settings)
        {
            var creatureCounts = new Dictionary<string, Dictionary<string, int>>();
            var worldDepth = Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY;

            for (var x = TopChunk.Origin.X; x < TopChunk.Origin.X + VoxelConstants.ChunkSizeX; x++)
                for (var z = TopChunk.Origin.Z; z < TopChunk.Origin.Z + VoxelConstants.ChunkSizeZ; z++)
                {
                    var overworldPosition = OverworldMap.WorldToOverworld(new Vector2(x, z));

                    if (Settings.Overworld.Map.GetBiomeAt(new Vector3(x, 0, z)).HasValue(out var biome))
                    {
                        var normalizedHeight = NormalizeHeight(Settings, Settings.Overworld.Map.LinearInterpolate(overworldPosition, OverworldField.Height));
                        var height = (int)MathFunctions.Clamp(normalizedHeight * worldDepth, 0.0f, worldDepth - 2);

                        var voxel = Settings.World.ChunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(x, height, z));

                        if (!voxel.IsValid
                            || voxel.Coordinate.Y == 0
                            || voxel.Coordinate.Y >= worldDepth - Settings.TreeLine)
                            continue;

                        if (LiquidCellHelpers.AnyLiquidInVoxel(voxel))
                            continue;

                        var above = VoxelHelpers.GetVoxelAbove(voxel);
                        if (above.IsValid && (LiquidCellHelpers.AnyLiquidInVoxel(above) || !above.IsEmpty))
                            continue;

                        foreach (var animal in biome.Fauna)
                        {
                            if (MathFunctions.RandEvent(animal.SpawnProbability))
                            {
                                if (!creatureCounts.ContainsKey(biome.Name))
                                {
                                    creatureCounts[biome.Name] = new Dictionary<string, int>();
                                }
                                var dict = creatureCounts[biome.Name];
                                if (!dict.ContainsKey(animal.Name))
                                {
                                    dict[animal.Name] = 0;
                                }
                                if (dict[animal.Name] < animal.MaxPopulation)
                                {
                                    EntityFactory.CreateEntity<GameComponent>(animal.Name,
                                        voxel.WorldPosition + Vector3.Up * 1.5f);
                                }
                                break;
                            }
                        }

                        if (voxel.Type.Name != biome.SoilLayer.VoxelType)
                            continue;

                        foreach (VegetationData veg in biome.Vegetation)
                        {
                            if (voxel.GrassType == 0)
                                continue;

                            if (MathFunctions.RandEvent(veg.SpawnProbability) &&
                                Settings.NoiseGenerator.Noise(voxel.Coordinate.X / veg.ClumpSize,
                                veg.NoiseOffset, voxel.Coordinate.Z / veg.ClumpSize) >= veg.ClumpThreshold)
                            {
                                var treeSize = MathFunctions.Rand() * veg.SizeVariance + veg.MeanSize;
                                var blackboard = new Blackboard();
                                blackboard.SetData("Scale", treeSize);

                                EntityFactory.CreateEntity<Plant>(veg.Name,
                                    voxel.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f),
                                    blackboard);

                                break;
                            }
                        }
                    }
                }
        }
    }
}