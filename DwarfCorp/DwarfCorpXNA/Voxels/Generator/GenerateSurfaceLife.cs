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
        public static void GenerateSurfaceLife(Dictionary<string, Dictionary<string, int>> creatureCounts, VoxelChunk Chunk, float maxHeight, GeneratorSettings Settings)
        {
            //int waterHeight = (int)(VoxelConstants.ChunkSizeY * NormalizeHeight(SeaLevel + 1.0f / VoxelConstants.ChunkSizeY, maxHeight));
            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
            {
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    var biomeData = Overworld.GetBiomeAt(new Vector3(x + Chunk.Origin.X, 0, z + Chunk.Origin.Z), Chunk.Manager.World.WorldScale, Chunk.Manager.World.WorldOrigin);
                    var topVoxel = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                        Chunk, new LocalVoxelCoordinate(x, VoxelConstants.ChunkSizeY - 1, z)));

                    if (!topVoxel.IsValid
                        || topVoxel.Coordinate.Y == 0
                        || topVoxel.Coordinate.Y >= 60) // Lift to some kind of generator settings?
                        continue;
                    var above = VoxelHelpers.GetVoxelAbove(topVoxel);
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
                                EntityFactory.CreateEntity<Body>(animal.Name,
                                    topVoxel.WorldPosition + Vector3.Up * 1.5f);
                            }
                            break;
                        }
                    }

                    if (topVoxel.Type.Name != biomeData.SoilLayer.VoxelType)
                        continue;

                    foreach (VegetationData veg in biomeData.Vegetation)
                    {
                        if (topVoxel.GrassType == 0)
                            continue;

                        if (MathFunctions.RandEvent(veg.SpawnProbability) &&
                            Settings.NoiseGenerator.Noise(topVoxel.Coordinate.X / veg.ClumpSize,
                            veg.NoiseOffset, topVoxel.Coordinate.Z / veg.ClumpSize) >= veg.ClumpThreshold)
                        {
                            topVoxel.RawSetType(VoxelLibrary.GetVoxelType(biomeData.SoilLayer.VoxelType));

                            var treeSize = MathFunctions.Rand() * veg.SizeVariance + veg.MeanSize;
                            var tree = EntityFactory.CreateEntity<Plant>(veg.Name,
                                topVoxel.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f),
                                Blackboard.Create("Scale", treeSize));

                            break;
                        }
                    }
                }
            }
        }
    }
}