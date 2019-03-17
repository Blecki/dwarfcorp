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

        public void GenerateWater(VoxelChunk chunk, float maxHeight)
        {
            int waterHeight = Math.Min((int)(VoxelConstants.ChunkSizeY * NormalizeHeight(Settings.SeaLevel + 1.0f / VoxelConstants.ChunkSizeY, maxHeight)), VoxelConstants.ChunkSizeY - 1);
            var iceID = VoxelLibrary.GetVoxelType("Ice");
            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
            {
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    var biome = Overworld.GetBiomeAt(new Vector3(x, 0, z) + chunk.Origin, chunk.Manager.World.WorldScale, chunk.Manager.World.WorldOrigin);
                    var topVoxel = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                        chunk, new LocalVoxelCoordinate(x, VoxelConstants.ChunkSizeY - 1, z)));

                    for (var y = 0; y <= waterHeight; ++y)
                    {
                        var vox = new VoxelHandle(chunk, new LocalVoxelCoordinate(x, y, z));
                        if (vox.IsEmpty && y > topVoxel.Coordinate.Y)
                        {
                            if (biome.WaterSurfaceIce && y == waterHeight)
                                vox.RawSetType(iceID);
                            else
                                vox.QuickSetLiquid(biome.WaterIsLava ? LiquidType.Lava : LiquidType.Water, WaterManager.maxWaterLevel);
                        }
                    }
                }
            }
        }

        public void GenerateLava(VoxelChunk chunk)
        {
            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
            {
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    for (var y = 0; y < Settings.LavaLevel; ++y)
                    {
                        var voxel = new VoxelHandle(chunk, new LocalVoxelCoordinate(x, y, z));
                        if (voxel.IsEmpty && voxel.LiquidLevel == 0)
                            voxel.QuickSetLiquid(LiquidType.Lava, WaterManager.maxWaterLevel);
                    }
                }
            }
        }

        public void GenerateCaves(VoxelChunk chunk, WorldManager world)
        {
            Vector3 origin = chunk.Origin;
            BiomeData biome = BiomeLibrary.GetBiome("Cave");
            var hellBiome = BiomeLibrary.GetBiome("Hell");

            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    var topVoxel = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                        chunk, new LocalVoxelCoordinate(x, VoxelConstants.ChunkSizeY - 1, z)));

                    for (int i = 0; i < Settings.CaveLevels.Count; i++)
                    {
                        int y = Settings.CaveLevels[i];
                        if (y <= 0 || y >= topVoxel.Coordinate.Y) continue;

                        var frequency = i < Settings.CaveFrequencies.Count ? Settings.CaveFrequencies[i] : Settings.CaveFrequencies[Settings.CaveFrequencies.Count - 1];
                        var caveBiome = (y <= Settings.HellLevel) ? hellBiome : biome;

                        Vector3 vec = new Vector3(x, y, z) + chunk.Origin;
                        double caveNoise = Settings.CaveNoise.GetValue((x + origin.X) * Settings.CaveNoiseScale * frequency,
                            (y + origin.Y) * Settings.CaveNoiseScale * 3.0f, (z + origin.Z) * Settings.CaveNoiseScale * frequency);

                        double heightnoise = Settings.NoiseGenerator.Noise((x + origin.X) * Settings.NoiseScale * frequency,
                            (y + origin.Y) * Settings.NoiseScale * 3.0f, (z + origin.Z) * Settings.NoiseScale * frequency);

                        int caveHeight = Math.Min(Math.Max((int)(heightnoise * 5), 1), 3);

                        if (!(caveNoise > Settings.CaveSize)) continue;

                        bool invalidCave = false;
                        for (int dy = 0; dy < caveHeight; dy++)
                        {
                            if (y - dy <= 0)
                                continue;

                            var voxel = new VoxelHandle(chunk, new LocalVoxelCoordinate(x, y - dy, z));

                            foreach (var coord in VoxelHelpers.EnumerateAllNeighbors(voxel.Coordinate))
                            {
                                VoxelHandle v = new VoxelHandle(Manager.ChunkData, coord);
                                if (!v.IsValid || (v.Sunlight))
                                {
                                    invalidCave = true;
                                    break;
                                }
                            }

                            if (!invalidCave)
                                voxel.RawSetType(VoxelLibrary.emptyType);
                            else
                            {
                                break;
                            }
                        }

                        if (!invalidCave && caveNoise > Settings.CaveSize * 1.8f && y - caveHeight > 0 && y > Settings.LavaLevel)
                        {
                            GenerateCaveVegetation(chunk, x, y, z, caveHeight, caveBiome, vec, world, Settings.NoiseGenerator);
                        }
                    }
                }
            }

            /*
            // Second pass sets the caves to empty as needed
            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int y = 0; y < VoxelConstants.ChunkSizeY; y++)
                {
                    for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                    {
                        VoxelHandle handle = new VoxelHandle(chunk, new LocalVoxelCoordinate(x, y, z));
                        if (handle.Type == magicCube)
                        {
                            handle.RawSetType(VoxelLibrary.emptyType);
                        }
                    }
                }
            }
             */
        }

        public static void GenerateCaveVegetation(VoxelChunk chunk, int x, int y, int z, int caveHeight, BiomeData biome, Vector3 vec, WorldManager world, Perlin NoiseGenerator)
        {
            var vUnder = new VoxelHandle(chunk, new LocalVoxelCoordinate(x, y - 1, z));
            var wayUnder = new VoxelHandle(chunk, new LocalVoxelCoordinate(x, y - caveHeight, z));

            wayUnder.RawSetType(VoxelLibrary.GetVoxelType(biome.SoilLayer.VoxelType));

            var grassType = GrassLibrary.GetGrassType(biome.GrassDecal);
            if (grassType != null)
                wayUnder.RawSetGrass(grassType.ID);

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

        public VoxelChunk GenerateChunk(Vector3 origin, WorldManager World, float maxHeight)
        {
            float waterHeight = NormalizeHeight(Settings.SeaLevel + 1.0f / VoxelConstants.ChunkSizeY, maxHeight);
            VoxelChunk c = new VoxelChunk(Manager, origin, GlobalVoxelCoordinate.FromVector3(origin).GetGlobalChunkCoordinate());

            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    Vector2 v = Overworld.WorldToOverworld(new Vector2(x + origin.X, z + origin.Z), World.WorldScale, World.WorldOrigin);

                    var biome = Overworld.Map[(int)MathFunctions.Clamp(v.X, 0, Overworld.Map.GetLength(0) - 1), (int)MathFunctions.Clamp(v.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;

                    BiomeData biomeData = BiomeLibrary.Biomes[biome];

                    Vector2 pos = Overworld.WorldToOverworld(new Vector2(x + origin.X, z + origin.Z), World.WorldScale, World.WorldOrigin);
                    float hNorm = NormalizeHeight(Overworld.LinearInterpolate(pos, Overworld.Map, Overworld.ScalarFieldType.Height), maxHeight);
                    float h = MathFunctions.Clamp(hNorm * VoxelConstants.ChunkSizeY, 0.0f, VoxelConstants.ChunkSizeY - 2);
                    int stoneHeight = (int)(MathFunctions.Clamp((int)(h - (biomeData.SoilLayer.Depth + (Math.Sin(v.X) + Math.Cos(v.Y)))), 1, h));

                    int currentSubsurfaceLayer = 0;
                    int depthWithinSubsurface = 0;
                    for (int y = VoxelConstants.ChunkSizeY - 1; y >= 0; y--)
                    {
                        var voxel = new VoxelHandle(c, new LocalVoxelCoordinate(x, y, z));

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
                                {
                                    currentSubsurfaceLayer = biomeData.SubsurfaceLayers.Count - 1;
                                }
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
