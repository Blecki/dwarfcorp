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
        public float NoiseScale { get; set; }
        public float CaveNoiseScale { get; set; }
        public float SeaLevel { get; set; }
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

                        if (!cluster.Type.SpawnInSoil && vox.Type.IsSoil) continue;

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

                if (!vein.Type.SpawnInSoil && vox.Type.IsSoil) continue;

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
            int waterHeight = (int) (SeaLevel*chunk.SizeY);
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

                    /*
                    for (int y = h - 1; y >= 0; y--)
                    {
                        voxel.Chunk = chunk;
                        voxel.GridPosition = new Vector3(x, y, z);
                        chunk.Data.Water[voxel.Index].Type = LiquidType.None;
                        chunk.Data.Water[voxel.Index].WaterLevel = 0;
                        voxel.Type = VoxelLibrary.GetVoxelType("Stone");
                        voxel.Chunk.NotifyTotalRebuild(!voxel.IsInterior);
                    }
                     */

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

        /*
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
                            if (v.IsEmpty || y >= h - 1 || !(y - h/2 < spawns.Value.MaximumHeight) ||
                                !(y - h/2 > spawns.Value.MinimumHeight) ||
                                !(PlayState.Random.NextDouble() <= spawns.Value.Probability) || v.Type.Name != "Stone")
                            {
                                continue;
                            }

                            float caviness = (float) NoiseGenerator.Noise((float) (x + origin.X)*s,
                                (float) (z + origin.Z)*s,
                                (float) (y + origin.Y + h)*s);

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
         */

        public void GenerateFauna(VoxelChunk chunk, ComponentManager components, ContentManager content, GraphicsDevice graphics, FactionLibrary factions)
        {
            int waterHeight = (int)(SeaLevel * chunk.SizeY);
            Voxel v = chunk.MakeVoxel(0, 0, 0);
            for (int x = 0; x < chunk.SizeX; x++)
            {
                for (int z = 0; z < chunk.SizeZ; z++)
                {
                    Vector2 vec = new Vector2(x + chunk.Origin.X, z + chunk.Origin.Z) / PlayState.WorldScale;
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
            int waterHeight = (int) (SeaLevel * chunk.SizeY);
            bool updated = false;
            Voxel v = chunk.MakeVoxel(0, 0, 0);
            Voxel vUnder = chunk.MakeVoxel(0, 0, 0);
            for(int x = 0; x < chunk.SizeX; x++)
            {
                for(int z = 0; z < chunk.SizeZ; z++)
                {
                    Vector2 vec = new Vector2(x + chunk.Origin.X, z + chunk.Origin.Z) / PlayState.WorldScale;
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
            float waterHeight = SeaLevel;
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

                    Overworld.Biome biome = Overworld.Map[(int)MathFunctions.Clamp(v.X, 0, Overworld.Map.GetLength(0) - 1), (int)MathFunctions.Clamp(v.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;

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
                            voxel.Health = 255;
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


            GenerateCaves(c);
            GenerateWater(c);

            GenerateLava(c);

   

            c.ShouldRebuildWater = true;
            return c;
        }
    }

}