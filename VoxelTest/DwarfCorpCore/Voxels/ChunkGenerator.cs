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
            CaveNoiseScale = noiseScale*2.0f;
        }

        public VoxelChunk GenerateEmptyChunk(Vector3 origin, int chunkSizeX, int chunkSizeY, int chunkSizeZ)
        {
            return new VoxelChunk(Manager, origin, 1,  Manager.ChunkData.GetChunkID(origin + new Vector3(0.5f, 0.5f, 0.5f)), chunkSizeX, chunkSizeY, chunkSizeZ); 
        }


        public void GenerateWater(VoxelChunk chunk)
        {
            int waterHeight = (int) (0.17f*chunk.SizeY);
            Voxel voxel = chunk.MakeVoxel(0, 0, 0);
            for (int x = 0; x < chunk.SizeX; x++)
            {
                for (int z = 0; z < chunk.SizeZ; z++)
                {
                    int h;
                    for (int y = 0; y < waterHeight; y++)
                    {
                        h = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);
                        int index = chunk.Data.IndexAt(x, y, z);
                        voxel.GridPosition = new Vector3(x, y, z);
                        if (voxel.IsEmpty && y >= h)
                        {
                            chunk.Data.Water[index].WaterLevel = 255;
                            chunk.Data.Water[index].Type = LiquidType.Water;
                        }
                    }


                    Vector2 vec = new Vector2(x + chunk.Origin.X, z + chunk.Origin.Z)/PlayState.WorldScale;
                    if (Overworld.GetWater(Overworld.Map, vec) != Overworld.WaterType.Volcano)
                    {
                        continue;
                    }

                    h = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);


                    if (h <= 0)
                    {
                        continue;
                    }

                    chunk.Data.Water[chunk.Data.IndexAt(x, h, z)].WaterLevel = 255;
                    chunk.Data.Water[chunk.Data.IndexAt(x, h, z)].Type = LiquidType.Lava;


                    for (int y = h - 1; y >= 0; y--)
                    {
                        voxel.Chunk = chunk;
                        voxel.GridPosition = new Vector3(x, y, z);
                        chunk.Data.Water[voxel.Index].Type = LiquidType.None;
                        chunk.Data.Water[voxel.Index].WaterLevel = 0;
                        voxel.Type = VoxelLibrary.GetVoxelType("Stone");
                        voxel.Chunk.NotifyTotalRebuild(!voxel.IsInterior);
                    }

                }
            }
        }

        public void GenerateLava(VoxelChunk chunk)
        {
            int lavaHeight = 2;
            Voxel voxel = chunk.MakeVoxel(0, 0, 0);
            for (int x = 0; x < chunk.SizeX; x++)
            {
                for (int z = 0; z < chunk.SizeZ; z++)
                {
                    for (int y = 0; y < lavaHeight; y++)
                    {
                        voxel.GridPosition = new Vector3(x, y, z);
                        if (voxel.IsEmpty && chunk.Data.Water[voxel.Index].WaterLevel == 0)
                        {
                            chunk.Data.Water[voxel.Index].WaterLevel = 255;
                            chunk.Data.Water[voxel.Index].Type = LiquidType.Lava;
                        }
                    }
                }
            }
        }

        public void GenerateOres(VoxelChunk chunk, ComponentManager components, ContentManager content,
            GraphicsDevice graphics)
        {
            Vector3 origin = chunk.Origin;
            int chunkSizeX = chunk.SizeX;
            int chunkSizeY = chunk.SizeY;
            int chunkSizeZ = chunk.SizeZ;
            Voxel v = chunk.MakeVoxel(0, 0, 0);
            for (int x = 0; x < chunkSizeX; x++)
            {
                for (int z = 0; z < chunkSizeZ; z++)
                {
                    int h = chunk.GetFilledVoxelGridHeightAt(x, chunkSizeY - 1, z);
                    for (int y = 1; y < chunkSizeY; y++)
                    {
                        foreach (
                            KeyValuePair<string, VoxelLibrary.ResourceSpawnRate> spawns in VoxelLibrary.ResourceSpawns)
                        {
                            float s = spawns.Value.VeinSize;
                            float p = spawns.Value.VeinSpawnThreshold;
                            v.GridPosition = new Vector3(x, y, z);
                            if (v.IsEmpty || y >= h - 1 || !(y < spawns.Value.MaximumHeight) ||
                                !(y > spawns.Value.MinimumHeight) ||
                                !(PlayState.Random.NextDouble() <= spawns.Value.Probability) || v.Type.Name != "Stone")
                            {
                                continue;
                            }

                            float caviness = (float) NoiseGenerator.Noise((float) (x + origin.X)*s,
                                (float) (z + origin.Z)*s,
                                (float) (y + origin.Y)*s);

                            if (caviness > p)
                            {
                                v.Type = VoxelLibrary.GetVoxelType(spawns.Key);
                            }
                            continue;
                        }
                    }
                }
            }
        }

        public void GenerateFauna(VoxelChunk chunk, ComponentManager components, ContentManager content, GraphicsDevice graphics, FactionLibrary factions)
        {
            int waterHeight = (int)(0.17 * chunk.SizeY);
            Voxel v = chunk.MakeVoxel(0, 0, 0);
            for (int x = 0; x < chunk.SizeX; x++)
            {
                for (int z = 0; z < chunk.SizeZ; z++)
                {
                    Vector2 vec = new Vector2(x + chunk.Origin.X, z + chunk.Origin.Z) / PlayState.WorldScale;
                    Overworld.Biome biome = Overworld.Map[(int)vec.X, (int)vec.Y].Biome;
                    BiomeData biomeData = BiomeLibrary.Biomes[biome];

                    int y = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);

                    if (!chunk.IsCellValid(x, (int)(y - chunk.Origin.Y), z))
                    {
                        continue;
                    }

                    v.GridPosition = new Vector3(x, y, z);

                    if (chunk.Data.Water[v.Index].WaterLevel != 0 || y <= waterHeight)
                    {
                        continue;
                    }

                    foreach (FaunaData animal in biomeData.Fauna)
                    {
                        if (y <= 0 || !(PlayState.Random.NextDouble() < animal.SpawnProbability))
                        {
                            continue;
                        }


                        EntityFactory.CreateEntity<Body>(animal.Name, chunk.Origin + new Vector3(x, y, z) + Vector3.Up*1.0f);

                        break;
                    }
                }
            }
        }

        public void GenerateVegetation(VoxelChunk chunk, ComponentManager components, ContentManager content, GraphicsDevice graphics)
        {
            int waterHeight = (int) (0.17 * chunk.SizeY);
            bool updated = false;
            Voxel v = chunk.MakeVoxel(0, 0, 0);
            Voxel vUnder = chunk.MakeVoxel(0, 0, 0);
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

                    v.GridPosition = new Vector3(x, y, z);

                    if(!v.IsEmpty || chunk.Data.Water[v.Index].WaterLevel != 0 || y <= waterHeight)
                    {
                        continue;
                    }

                    foreach(VegetationData veg in biomeData.Vegetation)
                    {
                        if(y <= 0)
                        {
                            continue;
                        }

                        if (!MathFunctions.RandEvent(veg.SpawnProbability))
                        {
                            continue;
                        }

                        if (NoiseGenerator.Noise(vec.X/veg.ClumpSize, veg.NoiseOffset, vec.Y/veg.ClumpSize) < veg.ClumpThreshold)
                        {
                            continue;
                        }

                        int yh = chunk.GetFilledVoxelGridHeightAt(x, y, z);

                        if(yh > 0)
                        {
                            vUnder.GridPosition = new Vector3(x, yh - 1, z);
                            if (!vUnder.IsEmpty && vUnder.TypeName == biomeData.GrassVoxel)
                            {
                                vUnder.Type = VoxelLibrary.GetVoxelType(biomeData.SoilVoxel);
                                updated = true;
                                float offset = veg.VerticalOffset;
                                if (vUnder.RampType != RampType.None)
                                {
                                    offset -= 0.25f;
                                }
                                float treeSize = MathFunctions.Rand()*veg.SizeVariance + veg.MeanSize;
                                EntityFactory.CreateEntity<Body>(veg.Name, chunk.Origin + new Vector3(x, y, z) + new Vector3(0, treeSize * offset, 0), Blackboard.Create("Scale", treeSize));
                            }

                        }

                        break;
                    }
                }
            }

            if (updated)
            {
                chunk.ShouldRebuild = true;
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

                        chunk.Data.Types[chunk.Data.IndexAt(x, y, z)] = 0;
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
            const float waterHeight = 0.155f;
            VoxelChunk c = new VoxelChunk(Manager, origin, 1,
                Manager.ChunkData.GetChunkID(origin + new Vector3(0.5f, 0.5f, 0.5f)), chunkSizeX, chunkSizeY, chunkSizeZ)
            {
                ShouldRebuild = true,
                ShouldRecalculateLighting = true
            };

            Voxel voxel = c.MakeVoxel(0, 0, 0);
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
                        voxel.GridPosition = new Vector3(x, y, z);
                        if(y == 0)
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType("Bedrock");
                            continue;
                        }

                        if(y <= stoneHeight && stoneHeight > 1)
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType(biomeData.SubsurfVoxel);
                            voxel.Health = VoxelLibrary.GetVoxelType(biomeData.SubsurfVoxel).StartingHealth;
                        }

                        else if((y == (int) h || y == stoneHeight) && hNorm > waterHeight)
                        {
                            if (biomeData.ClumpGrass &&
                                NoiseGenerator.Noise(pos.X/biomeData.ClumpSize, 0, pos.Y/biomeData.ClumpSize) >
                                biomeData.ClumpTreshold)
                            {
                                voxel.Type = VoxelLibrary.GetVoxelType(biomeData.GrassVoxel);
                                voxel.Health = VoxelLibrary.GetVoxelType(biomeData.GrassVoxel).StartingHealth;   
                            }
                            else if(!biomeData.ClumpGrass)
                            {
                                voxel.Type = VoxelLibrary.GetVoxelType(biomeData.GrassVoxel);
                                voxel.Health = VoxelLibrary.GetVoxelType(biomeData.GrassVoxel).StartingHealth; 
                            }
                            else
                            {
                                voxel.Type = VoxelLibrary.GetVoxelType(biomeData.SoilVoxel);
                                voxel.Health = VoxelLibrary.GetVoxelType(biomeData.SoilVoxel).StartingHealth;
                            }
                        }
                        else if(y > h && y > 0)
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType("empty");
                        }
                        else if(hNorm < waterHeight)
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType(biomeData.ShoreVoxel);
                            voxel.Health = VoxelLibrary.GetVoxelType(biomeData.ShoreVoxel).StartingHealth;
                        }
                        else
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType(biomeData.SoilVoxel);
                            voxel.Health = VoxelLibrary.GetVoxelType(biomeData.SoilVoxel).StartingHealth;
                        }
                    }
                }
            }


            GenerateOres(c, components, content, graphics);
            GenerateCaves(c);
            GenerateWater(c);

            GenerateLava(c);

   

            c.ShouldRebuildWater = true;
            return c;
        }
    }

}