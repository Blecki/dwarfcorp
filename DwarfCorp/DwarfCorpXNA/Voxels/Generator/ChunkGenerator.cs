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

namespace DwarfCorp
{
    /// <summary>
    /// Creates randomly generated voxel chunks using data from the overworld.
    /// </summary>
    public partial class ChunkGenerator
    {
        public Generation.GeneratorSettings Settings;
        public ChunkManager Manager { get; set; }

        public ChunkGenerator(int randomSeed, float noiseScale, WorldGenerationSettings WorldGenerationSettings)
        {
            Settings = new Generation.GeneratorSettings(randomSeed, noiseScale, WorldGenerationSettings);
        }

        private struct CaveGenerationData
        {
            public bool Exists;
            public double Noise;
            public int Height;
        }

        private const int CaveHeightScaleFactor = 5;
        private const int MaxCaveHeight = 3;

        private CaveGenerationData GetCaveGenerationData(GlobalVoxelCoordinate At, int LayerIndex)
        {
            var frequency = LayerIndex < Settings.CaveFrequencies.Count ? Settings.CaveFrequencies[LayerIndex] : Settings.CaveFrequencies[Settings.CaveFrequencies.Count - 1];

            var noiseVector = At.ToVector3() * Settings.CaveNoiseScale * new Vector3(frequency, 3.0f, frequency);
            var noise = Settings.CaveNoise.GetValue(noiseVector.X, noiseVector.Y, noiseVector.Z);

            var heightVector = At.ToVector3() * Settings.NoiseScale * new Vector3(frequency, 3.0f, frequency);
            var height = Settings.NoiseGenerator.Noise(heightVector);
            
            return new CaveGenerationData
            {
                Exists = noise > Settings.CaveSize,
                Noise = noise,
                Height = Math.Min(Math.Max((int)(height * CaveHeightScaleFactor), 1), MaxCaveHeight)
            };
        }

        public void GenerateCaves(VoxelChunk chunk, WorldManager world)
        {
            var origin = chunk.Origin;
            var biome = BiomeLibrary.GetBiome("Cave");
            var hellBiome = BiomeLibrary.GetBiome("Hell");

            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    for (int i = 0; i < Settings.CaveLevels.Count; i++)
                    {
                        // Does layer intersect this voxel?
                        int y = Settings.CaveLevels[i];
                        if (y + MaxCaveHeight < origin.Y) continue;
                        if (y >= origin.Y + VoxelConstants.ChunkSizeY) continue; 

                        var coordinate = new GlobalVoxelCoordinate(origin.X + x, y, origin.Z + z);

                        var data = GetCaveGenerationData(coordinate, i);

                        var caveBiome = (y <= Settings.HellLevel) ? hellBiome : biome;

                        if (!data.Exists) continue;

                        bool invalidCave = false;
                        for (int dy = 0; dy < data.Height; dy++)
                        {
                            var globalY = y + dy;

                            // Prevent caves punching holes in bedrock.
                            if (globalY <= 0) continue;

                            // Check if voxel is inside chunk.
                            if (globalY <= 0 || globalY < origin.Y || globalY >= origin.Y + VoxelConstants.ChunkSizeY) continue;

                            var voxel = VoxelHandle.UnsafeCreateLocalHandle(chunk, new LocalVoxelCoordinate(x, globalY - origin.Y, z));

                            foreach (var neighborCoordinate in VoxelHelpers.EnumerateAllNeighbors(voxel.Coordinate))
                            {
                                var v = Manager.CreateVoxelHandle(neighborCoordinate);
                                if (!v.IsValid || (v.Sunlight))
                                {
                                    invalidCave = true;
                                    break;
                                }
                            }

                            if (invalidCave)
                                break;

                            if (dy == 0)
                            {
                                // Place soil voxel and grass below cave.

                                var below = VoxelHelpers.GetVoxelBelow(voxel);
                                if (below.IsValid)
                                {
                                    below.RawSetType(VoxelLibrary.GetVoxelType(caveBiome.SoilLayer.VoxelType));
                                    var grassType = GrassLibrary.GetGrassType(caveBiome.GrassDecal);
                                    if (grassType != null)
                                        below.RawSetGrass(grassType.ID);
                                }

                                // Spawn vegetation.
                                if (data.Noise > Settings.CaveSize * 1.8f && globalY > Settings.LavaLevel)
                                {
                                    GenerateCaveFlora(below, caveBiome, Settings.NoiseGenerator, world);
                                }
                            }

                            voxel.RawSetType(VoxelLibrary.emptyType);
                        }                        
                    }
                }
            }
        }

        private static void GenerateCaveFlora(VoxelHandle CaveFloor, BiomeData Biome, Perlin NoiseGenerator, WorldManager WorldManager)
        {
            foreach (var floraType in Biome.Vegetation)
            {
                if (!MathFunctions.RandEvent(floraType.SpawnProbability))
                    continue;

                if (NoiseGenerator.Noise(CaveFloor.Coordinate.X / floraType.ClumpSize, floraType.NoiseOffset, CaveFloor.Coordinate.Z / floraType.ClumpSize) < floraType.ClumpThreshold)
                    continue;

                CaveFloor.RawSetGrass(0); // I preferred when grass existed under trees.

                var plantSize = MathFunctions.Rand() * floraType.SizeVariance + floraType.MeanSize;

                WorldManager.DoLazy(() =>
                {
                    if (!GameSettings.Default.FogofWar)
                        EntityFactory.CreateEntity<GameComponent>(
                            floraType.Name,
                            CaveFloor.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f), // Todo: Is this the correct offset?
                            Blackboard.Create("Scale", plantSize));
                    else
                        WorldManager.ComponentManager.RootComponent.AddChild(new ExploredListener(WorldManager.ComponentManager, CaveFloor)
                        {
                            EntityToSpawn = floraType.Name,
                            SpawnLocation = CaveFloor.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f),
                            BlackboardData = Blackboard.Create("Scale", plantSize)
                        });
                });

                break; // Don't risk spawning multiple plants in the same spot.
            }
        }

        public static void GenerateCaveVegetation(VoxelChunk chunk, int x, int y, int z, int caveHeight, BiomeData biome, Vector3 vec, WorldManager world, Perlin NoiseGenerator)
        {
            var vUnder = new VoxelHandle(chunk, new LocalVoxelCoordinate(x, y - 1, z));
            var wayUnder = new VoxelHandle(chunk, new LocalVoxelCoordinate(x, y - caveHeight, z));

            foreach (VegetationData veg in biome.Vegetation)
            {
                if (!MathFunctions.RandEvent(veg.SpawnProbability))
                {
                    continue;
                }

                if (NoiseGenerator.Noise(vec.X / veg.ClumpSize, veg.NoiseOffset, vec.Y / veg.ClumpSize) < veg.ClumpThreshold)
                {
                    continue;
                }

                if (!vUnder.IsEmpty && vUnder.Type.Name == biome.SoilLayer.VoxelType)
                {
                    vUnder.RawSetType(VoxelLibrary.GetVoxelType(biome.SoilLayer.VoxelType));
                    vUnder.RawSetGrass(0);
                    float treeSize = MathFunctions.Rand() * veg.SizeVariance + veg.MeanSize;

                    WorldManager.DoLazy(() =>
                    {
                        if (!GameSettings.Default.FogofWar)
                        {
                            GameComponent entity = EntityFactory.CreateEntity<GameComponent>(veg.Name,
                                vUnder.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f),
                                Blackboard.Create("Scale", treeSize));
                        }
                        else
                        {
                            world.ComponentManager.RootComponent.AddChild(new ExploredListener(
                                world.ComponentManager, vUnder)
                            {
                                EntityToSpawn = veg.Name,
                                SpawnLocation = vUnder.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f),
                                BlackboardData = Blackboard.Create("Scale", treeSize)
                            });
                        }
                    });
                }
            }

            float spawnLikelihood = (world.InitialEmbark.Difficulty + 0.1f);
            foreach (FaunaData animal in biome.Fauna)
            {
                if (y <= 0 || !(MathFunctions.Random.NextDouble() < animal.SpawnProbability * spawnLikelihood))
                    continue;

                FaunaData animal1 = animal;
                WorldManager.DoLazy(() =>
                {
                    if (!GameSettings.Default.FogofWar)
                    {
                        var entity = EntityFactory.CreateEntity<GameComponent>(animal1.Name,
                        wayUnder.WorldPosition + Vector3.Up * 1.5f);

                    }
                    else
                    {
                        world.ComponentManager.RootComponent.AddChild(new ExploredListener
                            (world.ComponentManager, new VoxelHandle(chunk, wayUnder.Coordinate.GetLocalVoxelCoordinate()))
                        {
                            EntityToSpawn = animal1.Name,
                            SpawnLocation = wayUnder.WorldPosition + Vector3.Up * 1.5f
                        });
                    }
                });
                break;
            }
        }

        public static float NormalizeHeight(float height, float maxHeight, float upperBound = 0.9f)
        {
            return height + (upperBound - maxHeight);
        }

        public void GenerateChunkData(VoxelChunk c, WorldManager world, float maxHeight)
        {
            UpdateSunlight(c);
            GenerateCaves(c, world);
            GenerateWater(c, maxHeight);
            GenerateLava(c);
        }

        public VoxelChunk GenerateChunk(GlobalChunkCoordinate ID, WorldManager World, float maxHeight)
        {
            var origin = new GlobalVoxelCoordinate(ID, new LocalVoxelCoordinate(0, 0, 0));
            float waterHeight = NormalizeHeight(Settings.SeaLevel + 1.0f / VoxelConstants.WorldSizeY, maxHeight);
            VoxelChunk c = new VoxelChunk(Manager, ID);

            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    Vector2 v = Overworld.WorldToOverworld(new Vector2(x + origin.X, z + origin.Z), World.WorldScale, World.WorldOrigin);

                    var biome = Overworld.Map[(int)MathFunctions.Clamp(v.X, 0, Overworld.Map.GetLength(0) - 1), (int)MathFunctions.Clamp(v.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;

                    BiomeData biomeData = BiomeLibrary.Biomes[biome];

                    Vector2 pos = Overworld.WorldToOverworld(new Vector2(x + origin.X, z + origin.Z), World.WorldScale, World.WorldOrigin);
                    float hNorm = NormalizeHeight(Overworld.LinearInterpolate(pos, Overworld.Map, Overworld.ScalarFieldType.Height), maxHeight);
                    float h = MathFunctions.Clamp(hNorm * VoxelConstants.ChunkSizeY, 0.0f, VoxelConstants.WorldSizeY - 2);
                    int stoneHeight = (int)(MathFunctions.Clamp((int)(h - (biomeData.SoilLayer.Depth + (Math.Sin(v.X) + Math.Cos(v.Y)))), 1, h));

                    int currentSubsurfaceLayer = 0;
                    int depthWithinSubsurface = 0;
                    for (int y = VoxelConstants.ChunkSizeY - 1; y >= 0; y--)
                    {
                        var voxel = VoxelHandle.UnsafeCreateLocalHandle(c, new LocalVoxelCoordinate(x, y, z));

                        if (y == 0)
                        {
                            voxel.RawSetType(VoxelLibrary.GetVoxelType("Bedrock"));
                            continue;
                        }

                        if (y <= stoneHeight && stoneHeight > 1)
                        {
                            voxel.RawSetType(VoxelLibrary.GetVoxelType(biomeData.SubsurfaceLayers[currentSubsurfaceLayer].VoxelType));
                            depthWithinSubsurface++;
                            if (depthWithinSubsurface > biomeData.SubsurfaceLayers[currentSubsurfaceLayer].Depth)
                            {
                                depthWithinSubsurface = 0;
                                currentSubsurfaceLayer++;
                                if (currentSubsurfaceLayer > biomeData.SubsurfaceLayers.Count - 1)
                                    currentSubsurfaceLayer = biomeData.SubsurfaceLayers.Count - 1;
                            }
                        }

                        else if ((y == (int)h || y == stoneHeight) && hNorm > waterHeight)
                        {
                            if (biomeData.ClumpGrass &&
                                Settings.NoiseGenerator.Noise(pos.X / biomeData.ClumpSize, 0, pos.Y / biomeData.ClumpSize) >
                                biomeData.ClumpTreshold)
                            {
                                voxel.RawSetType(VoxelLibrary.GetVoxelType(biomeData.SoilLayer.VoxelType));
                                if (!String.IsNullOrEmpty(biomeData.GrassDecal))
                                {
                                    var decal = GrassLibrary.GetGrassType(biomeData.GrassDecal);
                                    voxel.RawSetGrass(decal.ID);
                                }
                            }
                            else if (!biomeData.ClumpGrass)
                            {
                                voxel.RawSetType(VoxelLibrary.GetVoxelType(biomeData.SoilLayer.VoxelType));
                                if (!String.IsNullOrEmpty(biomeData.GrassDecal))
                                {
                                    var decal = GrassLibrary.GetGrassType(biomeData.GrassDecal);
                                    voxel.RawSetGrass(decal.ID);
                                }
                            }
                            else
                            {
                                voxel.RawSetType(VoxelLibrary.GetVoxelType(biomeData.SoilLayer.VoxelType));
                            }
                        }
                        else if (y > h && y > 0)
                        {
                            voxel.RawSetType(VoxelLibrary.emptyType);
                        }
                        else if (hNorm <= waterHeight)
                        {
                            voxel.RawSetType(VoxelLibrary.GetVoxelType(biomeData.ShoreVoxel));
                        }
                        else
                        {
                            voxel.RawSetType(VoxelLibrary.GetVoxelType(biomeData.SoilLayer.VoxelType));
                        }
                    }
                }
            }

            return c;
        }

        private static void UpdateSunlight(VoxelChunk Chunk)
        {
            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    var y = VoxelConstants.ChunkSizeY - 1;

                    for (; y >= 0; y--)
                    {
                        var v = new VoxelHandle(Chunk, new LocalVoxelCoordinate(x, y, z));
                        if (!v.IsValid)
                            continue;

                        v.Sunlight = true;
                        if (v.Type.ID != 0 && !v.Type.IsTransparent)
                            break;
                    }

                    for (y -= 1; y >= 0; y--)
                    {
                        var v = new VoxelHandle(Chunk, new LocalVoxelCoordinate(x, y, z));
                        if (!v.IsValid)
                            continue;
                        v.Sunlight = false;
                    }
                }
            }
        }
    }
}
