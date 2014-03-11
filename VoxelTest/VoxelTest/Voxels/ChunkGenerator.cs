using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    /// <summary>
    /// Creates randomly generated voxel chunks using data from the overworld.
    /// </summary>
    public class ChunkGenerator
    {
        public VoxelLibrary VoxLibrary { get; set; }
        public Perlin NoiseGenerator { get; set; }
        public float NoiseScale { get; set; }
        public float CaveNoiseScale { get; set; }

        public float MaxMountainHeight { get; set; }
        public ChunkManager Manager { get; set; }

        public ChunkGenerator(VoxelLibrary voxLibrary, int randomSeed, float noiseScale, float maxMountainHeight)
        {
            NoiseGenerator = new Perlin(randomSeed);
            NoiseScale = noiseScale;

            MaxMountainHeight = maxMountainHeight;
            VoxLibrary = voxLibrary;
            CaveNoiseScale = noiseScale * 2.0f;
        }

        public VoxelChunk GenerateEmptyChunk(Vector3 origin, int chunkSizeX, int chunkSizeY, int chunkSizeZ)
        {
            Voxel[][][] voxels = new Voxel[chunkSizeX][][];
            voxels[0] = new Voxel[chunkSizeY][];

            for(int x = 0; x < chunkSizeX; x++)
            {
                voxels[x] = new Voxel[chunkSizeY][];
                for(int y = 0; y < chunkSizeY; y++)
                {
                    voxels[x][y] = new Voxel[chunkSizeZ];
                    for(int z = 0; z < chunkSizeZ; z++)
                    {
                        voxels[x][y][z] = null;
                    }
                }
            }

            VoxelChunk c = new VoxelChunk(origin, Manager, voxels, Manager.ChunkData.GetChunkID(origin + new Vector3(0.5f, 0.5f, 0.5f)), 2);


            return c;
        }


        public void GenerateWater(VoxelChunk chunk)
        {
            int waterHeight = (int) (0.17f * chunk.SizeY);
            for(int x = 0; x < chunk.SizeX; x++)
            {
                for(int z = 0; z < chunk.SizeZ; z++)
                {
                    int h;
                    for(int y = 0; y < waterHeight; y++)
                    {
                        h = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);

                        if(chunk.VoxelGrid[x][y][z] == null && y >= h)
                        {
                            chunk.Water[x][y][z].WaterLevel = 255;
                            chunk.Water[x][y][z].Type = LiquidType.Water;
                        }
                    }


                    Vector2 vec = new Vector2(x + chunk.Origin.X, z + chunk.Origin.Z) / PlayState.WorldScale;
                    if(Overworld.GetWater(Overworld.Map, vec) != Overworld.WaterType.Volcano)
                    {
                        continue;
                    }

                    h = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);


                    if(h <= 0)
                    {
                        continue;
                    }

                    chunk.Water[x][h][z].WaterLevel = 255;
                    chunk.Water[x][h][z].Type = LiquidType.Lava;


                    for(int y = h - 1; y >= 0; y--)
                    {
                        Voxel v = new Voxel(new Vector3(x, y, z) + chunk.Origin, VoxelLibrary.GetVoxelType("Stone"), VoxelLibrary.GetPrimitive("Stone"), true);
                        chunk.VoxelGrid[x][y][z] = v;
                        chunk.Water[x][y][z].Type = LiquidType.None;
                        chunk.Water[x][y][z].WaterLevel = 0;
                        v.Chunk = chunk;
                        v.Chunk.NotifyTotalRebuild(!v.IsInterior);
                    }

                    /*
                    if (Overworld.GetWater(Overworld.Map, vec) == Overworld.WaterType.River || Overworld.GetWater(Overworld.Map, vec) == Overworld.WaterType.Lake)
                    {
                        int h = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);

                        for (int dh = h; dh >= waterHeight; dh--)
                        {
                            chunk.VoxelGrid[x][dh][z] = null;
                        }


                        chunk.Water[x][waterHeight][z].WaterLevel = 255;
                        chunk.Water[x][waterHeight][z].Type = LiquidType.Water;
                    }
                     */
                }
            }
        }

        public void GenerateLava(VoxelChunk chunk)
        {
            int lavaHeight = 2;
            for(int x = 0; x < chunk.SizeX; x++)
            {
                for(int z = 0; z < chunk.SizeZ; z++)
                {
                    for(int y = 0; y < lavaHeight; y++)
                    {
                        if(chunk.VoxelGrid[x][y][z] == null && chunk.Water[x][y][z].WaterLevel == 0)
                        {
                            chunk.Water[x][y][z].WaterLevel = 255;
                            chunk.Water[x][y][z].Type = LiquidType.Lava;
                        }
                    }
                }
            }
        }

        public void GenerateOres(VoxelChunk chunk, ComponentManager components, ContentManager content, GraphicsDevice graphics)
        {
            Vector3 origin = chunk.Origin;
            int chunkSizeX = chunk.SizeX;
            int chunkSizeY = chunk.SizeY;
            int chunkSizeZ = chunk.SizeZ;
            for(int x = 0; x < chunkSizeX; x++)
            {
                for(int z = 0; z < chunkSizeZ; z++)
                {
                    int h = chunk.GetFilledVoxelGridHeightAt(x, chunkSizeY - 1, z);
                    for(int y = 1; y < chunkSizeY; y++)
                    {
                        foreach(KeyValuePair<string, VoxelLibrary.ResourceSpawnRate> spawns in VoxelLibrary.ResourceSpawns)
                        {
                            float s = spawns.Value.VeinSize;
                            float p = spawns.Value.VeinSpawnThreshold;

                            Voxel v = chunk.VoxelGrid[x][y][z];
                            if(v == null || y >= h - 1 || !(y < spawns.Value.MaximumHeight) || !(y > spawns.Value.MinimumHeight) || !(PlayState.Random.NextDouble() <= spawns.Value.Probability) || v.Type.Name != "Stone")
                            {
                                continue;
                            }

                            float caviness = (float) NoiseGenerator.Noise((float) (x + origin.X) * s,
                                (float) (z + origin.Z) * s,
                                (float) (y + origin.Y) * s);

                            if(caviness > p)
                            {
                                v.Type = VoxelLibrary.GetVoxelType(spawns.Key);
                                v.Primitive = VoxelLibrary.GetPrimitive(spawns.Key);
                            }
                            continue;
                        }
                    }
                }
            }
        }

        public void GenerateVegetation(VoxelChunk chunk, ComponentManager components, ContentManager content, GraphicsDevice graphics)
        {
            int waterHeight = (int) (0.17 * chunk.SizeY);
            for(int x = 0; x < chunk.SizeX; x++)
            {
                for(int z = 0; z < chunk.SizeZ; z++)
                {
                    Vector2 vec = new Vector2(x + chunk.Origin.X, z + chunk.Origin.Z) / PlayState.WorldScale;
                    Overworld.Biome biome = Overworld.Map[(int) vec.X, (int) vec.Y].Biome;
                    BiomeData biomeData = BiomeLibrary.Biomes[biome];

                    int y = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);

                    if(!chunk.IsCellValid(x, (int) (y - chunk.Origin.Y), z))
                    {
                        continue;
                    }

                    Voxel v = chunk.VoxelGrid[x][y][z];

                    if(v != null || chunk.Water[x][y][z].WaterLevel != 0 || y <= waterHeight)
                    {
                        continue;
                    }

                    foreach(VegetationData veg in biomeData.Vegetation)
                    {
                        if(y <= 0 || !(PlayState.Random.NextDouble() < veg.SpawnProbability))
                        {
                            continue;
                        }

                        float treeSize = (float) PlayState.Random.NextDouble() * veg.SizeVariance + veg.MeanSize;
                        EntityFactory.GenerateVegetation(veg.Name, treeSize, veg.VerticalOffset, chunk.Origin + new Vector3(x, y, z), components, content, graphics);


                        break;
                    }
                }
            }
        }


        public void GenerateCaves(VoxelChunk chunk)
        {
            Vector3 origin = chunk.Origin;
            int chunkSizeX = chunk.SizeX;
            int chunkSizeY = chunk.SizeY;
            int chunkSizeZ = chunk.SizeZ;
            for(int x = 0; x < chunkSizeX; x++)
            {
                for(int z = 0; z < chunkSizeZ; z++)
                {
                    int h = chunk.GetFilledVoxelGridHeightAt(x, chunkSizeY - 1, z);
                    for(int y = 1; y < chunkSizeY; y++)
                    {
                        if(y >= h - 5)
                        {
                            continue;
                        }

                        float caviness = (float) NoiseGenerator.Noise((float) (x + origin.X) * CaveNoiseScale, (float) (z + origin.Z) * CaveNoiseScale, (float) (y + origin.Y) * CaveNoiseScale);
                        if(!(caviness > 0.9f))
                        {
                            continue;
                        }

                        chunk.VoxelGrid[x][y][z] = null;
                    }
                }
            }
        }

        public static T[][][] Allocate<T>(int chunkSizeX, int chunkSizeY, int chunkSizeZ)
        {
            T[][][] voxels = new T[chunkSizeX][][];

            for(int x = 0; x < chunkSizeX; x++)
            {
                voxels[x] = new T[chunkSizeY][];
                for(int y = 0; y < chunkSizeY; y++)
                {
                    voxels[x][y] = new T[chunkSizeZ];
                }
            }

            return voxels;
        }

        public static Voxel[][][] Allocate(int chunkSizeX, int chunkSizeY, int chunkSizeZ)
        {
            Voxel[][][] voxels = new Voxel[chunkSizeX][][];

            for(int x = 0; x < chunkSizeX; x++)
            {
                voxels[x] = new Voxel[chunkSizeY][];
                for(int y = 0; y < chunkSizeY; y++)
                {
                    voxels[x][y] = new Voxel[chunkSizeZ];
                }
            }

            return voxels;
        }

        public VoxelChunk GenerateChunk(Vector3 origin, int chunkSizeX, int chunkSizeY, int chunkSizeZ, ComponentManager components, ContentManager content, GraphicsDevice graphics)
        {
            Voxel[][][] voxels = Allocate(chunkSizeX, chunkSizeY, chunkSizeZ);
            const float waterHeight = 0.155f;

            for(int x = 0; x < chunkSizeX; x++)
            {
                for(int z = 0; z < chunkSizeZ; z++)
                {
                    Vector2 v = new Vector2(x + origin.X, z + origin.Z) / PlayState.WorldScale;

                    Overworld.Biome biome = Overworld.Map[(int) v.X, (int) v.Y].Biome;

                    BiomeData biomeData = BiomeLibrary.Biomes[biome];

                    Vector2 pos = new Vector2(x + origin.X, z + origin.Z) / PlayState.WorldScale;
                    float hNorm = Overworld.GetValue(Overworld.Map, pos, Overworld.ScalarFieldType.Height);
                    float h = MathFunctions.Clamp(hNorm * chunkSizeY, 0.0f, chunkSizeY - 2);
                    int stoneHeight = (int) Math.Max(h - 2, 1);


                    for(int y = 0; y < chunkSizeY; y++)
                    {
                        if(y == 0)
                        {
                            voxels[x][y][z] = new Voxel(new Vector3((x + origin.X), (y + origin.Y), (z + origin.Z)),
                                VoxelLibrary.GetVoxelType("Bedrock"),
                                VoxelLibrary.GetPrimitive("Bedrock"), true);
                            continue;
                        }


                        if(y <= stoneHeight && stoneHeight > 1)
                        {
                            voxels[x][y][z] = new Voxel(new Vector3((x + origin.X), (y + origin.Y), (z + origin.Z)),
                                VoxelLibrary.GetVoxelType(biomeData.SubsurfVoxel),
                                VoxelLibrary.GetPrimitive(biomeData.SubsurfVoxel), true);
                            voxels[x][y][z].Health = VoxelLibrary.GetVoxelType(biomeData.SubsurfVoxel).StartingHealth;
                        }

                        else if((y == (int) h || y == stoneHeight) && hNorm > waterHeight)
                        {
                            voxels[x][y][z] = new Voxel(new Vector3((x + origin.X), (y + origin.Y),
                                (z + origin.Z)),
                                VoxelLibrary.GetVoxelType(biomeData.GrassVoxel),
                                VoxelLibrary.GetPrimitive(biomeData.GrassVoxel),
                                true)
                            {
                                Health = VoxelLibrary.GetVoxelType(biomeData.GrassVoxel).StartingHealth
                            };
                        }
                        else if(y > h && y > 0)
                        {
                            voxels[x][y][z] = null;
                        }
                        else if(hNorm < waterHeight)
                        {
                            voxels[x][y][z] = new Voxel(
                                new Vector3((x + origin.X), (y + origin.Y), (z + origin.Z)),
                                VoxelLibrary.GetVoxelType(biomeData.ShoreVoxel),
                                VoxelLibrary.GetPrimitive(biomeData.ShoreVoxel),
                                true)
                            {
                                Health = VoxelLibrary.GetVoxelType(biomeData.ShoreVoxel).StartingHealth
                            };
                        }
                        else
                        {
                            voxels[x][y][z] = new Voxel(
                                new Vector3((x + origin.X), (y + origin.Y), (z + origin.Z)),
                                VoxelLibrary.GetVoxelType(biomeData.SoilVoxel),
                                VoxelLibrary.GetPrimitive(biomeData.SoilVoxel), true)
                            {
                                Health = VoxelLibrary.GetVoxelType(biomeData.SoilVoxel).StartingHealth
                            };
                        }
                    }
                }
            }

            VoxelChunk c = new VoxelChunk(origin, Manager, voxels, Manager.ChunkData.GetChunkID(origin + new Vector3(0.5f, 0.5f, 0.5f)), 1)
            {
                ShouldRebuild = true,
                ShouldRecalculateLighting = true
            };


            GenerateOres(c, components, content, graphics);
            GenerateCaves(c);
            GenerateWater(c);

            GenerateLava(c);

            GenerateVegetation(c, components, content, graphics);


            c.ShouldRebuildWater = true;
            return c;
        }
    }

}