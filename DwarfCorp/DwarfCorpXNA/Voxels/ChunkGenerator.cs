// ChunkGenerator.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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

    public struct OreCluster
    {
        public VoxelType Type { get; set; }
        public Vector3 Size { get; set; }
        public Matrix Transform { get; set; }
    }

    public struct OreVein
    {
        public VoxelType Type { get; set; }
        public Vector3 Start { get; set; }
        public float Length { get; set; }
    }

    /// <summary>
    /// Creates randomly generated voxel chunks using data from the overworld.
    /// </summary>
    public class ChunkGenerator
    {
        public VoxelLibrary VoxLibrary { get; set; }
        public Perlin NoiseGenerator { get; set; }
        public LibNoise.FastRidgedMultifractal CaveNoise { get; set; }
        public float NoiseScale { get; set; }
        public float CaveNoiseScale { get; set; }
        public float SeaLevel { get; set; }
        public float MaxMountainHeight { get; set; }
        public LibNoise.FastRidgedMultifractal AquiferNoise { get; set; }
        public LibNoise.FastRidgedMultifractal LavaNoise { get; set; }
        public ChunkManager Manager { get; set; }
        public List<int> CaveLevels { get; set; }
        public List<float> CaveFrequencies { get; set; } 
        public List<int> AquiverLevels { get; set; }
        public List<int> LavaLevels { get; set; } 
        public float CaveSize { get; set; }
        public float AquiferSize { get; set; }
        public float LavaSize { get; set; }

        public ChunkGenerator(VoxelLibrary voxLibrary, int randomSeed, float noiseScale, float maxMountainHeight)
        {
            NoiseGenerator = new Perlin(randomSeed);
            NoiseScale = noiseScale;

            MaxMountainHeight = maxMountainHeight;
            VoxLibrary = voxLibrary;
            CaveNoiseScale = noiseScale*10.0f;
            CaveSize = 0.03f;
            CaveLevels = new List<int>(){4, 8, 11, 16};
            CaveFrequencies = new List<float>(){0.5f, 0.7f, 0.9f, 1.0f};

            CaveNoise = new FastRidgedMultifractal(randomSeed)
            {
                Frequency = 0.5f,
                Lacunarity = 0.5f,
                NoiseQuality = NoiseQuality.Standard,
                OctaveCount = 1,
                Seed = randomSeed
            };

            AquiverLevels = new List<int>() {5};

            AquiferSize = 0.02f;
            AquiferNoise = new FastRidgedMultifractal(randomSeed + 100)
            {
                Frequency = 0.25f,
                Lacunarity = 0.5f,
                NoiseQuality = NoiseQuality.Standard,
                OctaveCount = 1,
                Seed = randomSeed
            };

            LavaLevels = new List<int>() { 1, 2 };
            LavaSize = 0.01f;
            LavaNoise = new FastRidgedMultifractal(randomSeed + 200)
            {
                Frequency = 0.15f,
                Lacunarity = 0.5f,
                NoiseQuality = NoiseQuality.Standard,
                OctaveCount = 1,
                Seed = randomSeed
            };
        }

        public void GenerateCluster(OreCluster cluster, ChunkData chunks)
        {
            Voxel vox = new Voxel();
            for (float x = -cluster.Size.X*0.5f; x < cluster.Size.X*0.5f; x += 1.0f)
            {
                for (float y = -cluster.Size.Y*0.5f; y < cluster.Size.Y*0.5f; y += 1.0f)
                {
                    for (float z = -cluster.Size.Z*0.5f; z < cluster.Size.Z*0.5f; z += 1.0f)
                    {
                        float radius = (float)(Math.Pow(x/cluster.Size.X, 2.0f) + Math.Pow(y/cluster.Size.Y, 2.0f) +
                                       Math.Pow(z/cluster.Size.Z, 2.0f));

                        if (radius > 1.0f + MathFunctions.Rand(0.0f, 0.25f)) continue;
                        Vector3 locPosition = new Vector3(x, y, z);

                        Vector3 globalPosition = Vector3.Transform(locPosition, cluster.Transform);

                        if (globalPosition.Y > cluster.Type.MaxSpawnHeight ||
                            globalPosition.Y < cluster.Type.MinSpawnHeight) continue;

                        if (!chunks.GetVoxel(globalPosition, ref vox)) continue;

                        if (vox.IsEmpty) continue;

                        if (!cluster.Type.SpawnOnSurface && vox.Type.IsSurface) continue;

                        if (!MathFunctions.RandEvent(cluster.Type.SpawnProbability)) continue;

                        vox.Type = cluster.Type;
                    }
                }
            }
        }

        public void GenerateVein(OreVein vein, ChunkData chunks)
        {
            Voxel vox = new Voxel();
            Vector3 curr = vein.Start;
            Vector3 directionBias = MathFunctions.RandVector3Box(-1, 1, -0.1f, 0.1f, -1, 1);
            for (float t = 0; t < vein.Length; t++)
            {
                if (curr.Y > vein.Type.MaxSpawnHeight ||
                    curr.Y < vein.Type.MinSpawnHeight) continue;
                Vector3 p = new Vector3(curr.X , curr.Y, curr.Z);
                if (!chunks.GetVoxel(p, ref vox)) continue;

                if (vox.IsEmpty) continue;

                if (!MathFunctions.RandEvent(vein.Type.SpawnProbability)) continue;

                if (!vein.Type.SpawnOnSurface && vox.Type.IsSurface) continue;

                vox.Type = vein.Type;
                Vector3 step = directionBias + MathFunctions.RandVector3Box(-1, 1, -1, 1, -1, 1)*0.25f;
                step.Normalize();
                curr += step;
            }
        }

        public VoxelChunk GenerateEmptyChunk(Vector3 origin, int chunkSizeX, int chunkSizeY, int chunkSizeZ)
        {
            return new VoxelChunk(Manager, origin, 1,  Manager.ChunkData.GetChunkID(origin + new Vector3(0.5f, 0.5f, 0.5f)), chunkSizeX, chunkSizeY, chunkSizeZ); 
        }


        public void GenerateWater(VoxelChunk chunk)
        {
            int waterHeight = (int) (SeaLevel*chunk.SizeY) + 1;
            Voxel voxel = chunk.MakeVoxel(0, 0, 0);
            for (int x = 0; x < chunk.SizeX; x++)
            {
                for (int z = 0; z < chunk.SizeZ; z++)
                {
                    int h;
                    for (int y = 0; y <= waterHeight; y++)
                    {
                        h = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);
                        int index = chunk.Data.IndexAt(x, y, z);
                        voxel.GridPosition = new Vector3(x, y, z);
                        if (voxel.IsEmpty && y >= h)
                        {
                            chunk.Data.Water[index].WaterLevel = WaterManager.maxWaterLevel;
                            chunk.Data.Water[index].Type = LiquidType.Water;
                        }
                    }


                    Vector2 vec = new Vector2(x + chunk.Origin.X, z + chunk.Origin.Z)/DwarfGame.World.WorldScale;
                    if (Overworld.GetWater(Overworld.Map, vec) != Overworld.WaterType.Volcano)
                    {
                        continue;
                    }

                    h = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);


                    if (h <= 0)
                    {
                        continue;
                    }

                    chunk.Data.Water[chunk.Data.IndexAt(x, h, z)].WaterLevel = WaterManager.maxWaterLevel;
                    chunk.Data.Water[chunk.Data.IndexAt(x, h, z)].Type = LiquidType.Lava;
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
                            chunk.Data.Water[voxel.Index].WaterLevel = WaterManager.maxWaterLevel;
                            chunk.Data.Water[voxel.Index].Type = LiquidType.Lava;
                        }
                    }
                }
            }
        }

        public static BiomeData GetBiomeAt(Vector3 worldPosition)
        {
            Vector2 vec = new Vector2(worldPosition.X, worldPosition.Z) / DwarfGame.World.WorldScale;
            Overworld.Biome biome = Overworld.Map[(int)MathFunctions.Clamp(vec.X, 0, Overworld.Map.GetLength(0) - 1), (int)MathFunctions.Clamp(vec.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;
            return BiomeLibrary.Biomes[biome];
        }

        public static float GetValueAt(Vector3 worldPosition, Overworld.ScalarFieldType T)
        {
            Vector2 vec = new Vector2(worldPosition.X, worldPosition.Z) / DwarfGame.World.WorldScale;
            return Overworld.GetValue(Overworld.Map, new Vector2(MathFunctions.Clamp(vec.X, 0, Overworld.Map.GetLength(0) - 1),
                MathFunctions.Clamp(vec.Y, 0, Overworld.Map.GetLength(1) - 1)), T);
        }

        public void GenerateFauna(VoxelChunk chunk, ComponentManager components, ContentManager content, GraphicsDevice graphics, FactionLibrary factions)
        {
            int waterHeight = (int)(SeaLevel * chunk.SizeY);
            Voxel v = chunk.MakeVoxel(0, 0, 0);
            for (int x = 0; x < chunk.SizeX; x++)
            {
                for (int z = 0; z < chunk.SizeZ; z++)
                {
                    Vector2 vec = new Vector2(x + chunk.Origin.X, z + chunk.Origin.Z) / DwarfGame.World.WorldScale;
                    Overworld.Biome biome = Overworld.Map[(int)MathFunctions.Clamp(vec.X, 0, Overworld.Map.GetLength(0) - 1), (int)MathFunctions.Clamp(vec.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;
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
                        if (y <= 0 || !(MathFunctions.Random.NextDouble() < animal.SpawnProbability))
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
            int waterHeight = (int) (SeaLevel * chunk.SizeY);
            bool updated = false;
            Voxel v = chunk.MakeVoxel(0, 0, 0);
            Voxel vUnder = chunk.MakeVoxel(0, 0, 0);
            for(int x = 0; x < chunk.SizeX; x++)
            {
                for(int z = 0; z < chunk.SizeZ; z++)
                {
                    Vector2 vec = new Vector2(x + chunk.Origin.X, z + chunk.Origin.Z) / DwarfGame.World.WorldScale;
                    Overworld.Biome biome = Overworld.Map[(int)MathFunctions.Clamp(vec.X, 0, Overworld.Map.GetLength(0) - 1), (int)MathFunctions.Clamp(vec.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;
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

        public void GenerateLavaTubes(VoxelChunk chunk)
        {
            Vector3 origin = chunk.Origin;
            int chunkSizeX = chunk.SizeX;
            int chunkSizeY = chunk.SizeY;
            int chunkSizeZ = chunk.SizeZ;
            for (int x = 0; x < chunkSizeX; x++)
            {
                for (int z = 0; z < chunkSizeZ; z++)
                {
                    int h = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);
                    for (int i = 0; i < LavaLevels.Count; i++)
                    {
                        int y = LavaLevels[i];
                        if (y <= 0 || y >= h) continue;

                        double caveNoise = LavaNoise.GetValue((x + origin.X) * CaveNoiseScale, (y + origin.Y) * 3.0f, (z + origin.Z) * CaveNoiseScale);


                        if (caveNoise > LavaSize)
                        {
                            chunk.Data.Types[chunk.Data.IndexAt(x, y, z)] = 0;
                            chunk.Data.Water[chunk.Data.IndexAt(x, y, z)].WaterLevel = WaterManager.maxWaterLevel;
                            chunk.Data.Water[chunk.Data.IndexAt(x, y, z)].Type = LiquidType.Lava;
                        }
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
            BiomeData biome = BiomeLibrary.Biomes[Overworld.Biome.Cave];
            List<Voxel> neighbors = new List<Voxel>();
            Voxel vUnder = chunk.MakeVoxel(0, 0, 0);
            for (int x = 0; x < chunkSizeX; x++)
            {
                for (int z = 0; z < chunkSizeZ; z++)
                {
                    int h = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);
                    for (int i = 0; i < CaveLevels.Count; i++)
                    {
                        int y = CaveLevels[i];
                        if (y <= 0 || y >= h - 1) continue;
                        Vector3 vec = new Vector3(x, y, z) + chunk.Origin;
                        double caveNoise = CaveNoise.GetValue((x + origin.X) * CaveNoiseScale * CaveFrequencies[i],
                            (y + origin.Y) * CaveNoiseScale * 3.0f, (z + origin.Z) * CaveNoiseScale * CaveFrequencies[i]);

                        double heightnoise = NoiseGenerator.Noise((x + origin.X)*NoiseScale*CaveFrequencies[i],
                            (y + origin.Y) * NoiseScale * 3.0f, (z + origin.Z) * NoiseScale * CaveFrequencies[i]);

                        int caveHeight = Math.Min(Math.Max((int) (heightnoise*5), 1), 3);
                        
                        if (caveNoise > CaveSize)
                        {
                            bool waterFound = false;
                            for (int dy = 0; dy < caveHeight; dy++)
                            {
                                int index = chunk.Data.IndexAt(x, y - dy, z);
                                chunk.GetNeighborsManhattan(x, y - dy, z, neighbors);

                                if (neighbors.Any(v => v != null &&  v.WaterLevel > 0))
                                {
                                    waterFound = true;
                                }

                                if (waterFound)
                                    break;

                                chunk.Data.Types[index] = 0;
                            }

                            if (!waterFound && caveNoise > CaveSize*1.8f && y - caveHeight > 0)
                            {
                                int indexunder = chunk.Data.IndexAt(x, y - caveHeight, z);
                                chunk.Data.Types[indexunder] = (byte)VoxelLibrary.GetVoxelType(biome.GrassVoxel).ID;
                                chunk.Data.Health[indexunder] = (byte)VoxelLibrary.GetVoxelType(biome.GrassVoxel).StartingHealth;
                                chunk.Data.IsExplored[indexunder] = false;
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


                                    vUnder.GridPosition = new Vector3(x, y - 1, z);
                                    if (!vUnder.IsEmpty && vUnder.TypeName == biome.GrassVoxel)
                                    {
                                        vUnder.Type = VoxelLibrary.GetVoxelType(biome.SoilVoxel);
                                        float offset = veg.VerticalOffset;
                                        if (vUnder.RampType != RampType.None)
                                        {
                                            offset -= 0.25f;
                                        }
                                        float treeSize = MathFunctions.Rand() * veg.SizeVariance + veg.MeanSize;
                                        GameComponent entity = EntityFactory.CreateEntity<GameComponent>(veg.Name, chunk.Origin + new Vector3(x, y, z) + new Vector3(0, treeSize * offset, 0), Blackboard.Create("Scale", treeSize));
                                        entity.GetRootComponent().SetActiveRecursive(false);
                                        entity.GetRootComponent().SetVisibleRecursive(false);
                                        if (GameSettings.Default.FogofWar)
                                        {
                                            ExploredListener listener = new ExploredListener(
                                                DwarfGame.World.ComponentManager, entity, DwarfGame.World.ChunkManager, vUnder);
                                        }
                                    }
                                }
                            }

                            foreach (FaunaData animal in biome.Fauna)
                            {
                                if (y <= 0 || !(MathFunctions.Random.NextDouble() < animal.SpawnProbability))
                                {
                                    continue;
                                }


                                var entity = EntityFactory.CreateEntity<GameComponent>(animal.Name, chunk.Origin + new Vector3(x, y, z) + Vector3.Up * 1.0f);

                                if (GameSettings.Default.FogofWar)
                                {
                                    entity.GetRootComponent().SetActiveRecursive(false);
                                    entity.GetRootComponent().SetVisibleRecursive(false);
                                    ExploredListener listener = new ExploredListener(DwarfGame.World.ComponentManager,
                                        entity,
                                        DwarfGame.World.ChunkManager, chunk.MakeVoxel(x, y, z));
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void GenerateAquifers(VoxelChunk chunk)
        {
            Vector3 origin = chunk.Origin;
            int chunkSizeX = chunk.SizeX;
            int chunkSizeY = chunk.SizeY;
            int chunkSizeZ = chunk.SizeZ;
            for (int x = 0; x < chunkSizeX; x++)
            {
                for (int z = 0; z < chunkSizeZ; z++)
                {
                    int h = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);
                    for (int i = 0; i < AquiverLevels.Count; i++)
                    {
                        int y = AquiverLevels[i];
                        if (y <= 0 || y >= h) continue;

                        double caveNoise = AquiferNoise.GetValue((x + origin.X) * CaveNoiseScale, (y + origin.Y) * 3.0f, (z + origin.Z) * CaveNoiseScale);


                        if (caveNoise > AquiferSize)
                        {
                            chunk.Data.Types[chunk.Data.IndexAt(x, y, z)] = 0;
                            chunk.Data.Water[chunk.Data.IndexAt(x, y, z)].WaterLevel = WaterManager.maxWaterLevel;
                            chunk.Data.Water[chunk.Data.IndexAt(x, y, z)].Type = LiquidType.Water;
                        }
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
            float waterHeight = SeaLevel + 1.0f / chunkSizeY;
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
                    Vector2 v = new Vector2(x + origin.X, z + origin.Z) / DwarfGame.World.WorldScale;

                    Overworld.Biome biome = Overworld.Map[(int)MathFunctions.Clamp(v.X, 0, Overworld.Map.GetLength(0) - 1), (int)MathFunctions.Clamp(v.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;

                    BiomeData biomeData = BiomeLibrary.Biomes[biome];

                    Vector2 pos = new Vector2(x + origin.X, z + origin.Z) / DwarfGame.World.WorldScale;
                    float hNorm = Overworld.LinearInterpolate(pos, Overworld.Map, Overworld.ScalarFieldType.Height);
                    float h = MathFunctions.Clamp(hNorm * chunkSizeY, 0.0f, chunkSizeY - 2);
                    int stoneHeight = (int) Math.Max(h - 2, 1);


                    for(int y = 0; y < chunkSizeY; y++)
                    {
                        voxel.GridPosition = new Vector3(x, y, z);
                        if(y == 0)
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType("Bedrock");
                            voxel.Health = 8;
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
                        else if(hNorm <= waterHeight)
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


            GenerateWater(c);
            GenerateLava(c);
            GenerateCaves(c);
            //GenerateAquifers(c);
            //GenerateLavaTubes(c);

            c.ShouldRecalculateLighting = true;
            c.ShouldRebuildWater = true;
            return c;
        }
    }

}