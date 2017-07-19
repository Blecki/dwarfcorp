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
        public static float WorldScale { get; set; }

        public ChunkGenerator(VoxelLibrary voxLibrary, int randomSeed, float noiseScale, float worldScale)
        {
            WorldScale = worldScale;
            NoiseGenerator = new Perlin(randomSeed);
            NoiseScale = noiseScale;

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

                        var vox = new TemporaryVoxelHandle(chunks,
                            GlobalVoxelCoordinate.FromVector3(globalPosition));

                        if (!vox.IsValid || vox.IsEmpty) continue;

                        if (!cluster.Type.SpawnOnSurface && vox.Type.IsSurface) continue;

                        if (!MathFunctions.RandEvent(cluster.Type.SpawnProbability)) continue;

                        vox.Type = cluster.Type;
                    }
                }
            }
        }

        public void GenerateVein(OreVein vein, ChunkData chunks)
        {
            Vector3 curr = vein.Start;
            Vector3 directionBias = MathFunctions.RandVector3Box(-1, 1, -0.1f, 0.1f, -1, 1);
            for (float t = 0; t < vein.Length; t++)
            {
                if (curr.Y > vein.Type.MaxSpawnHeight ||
                    curr.Y < vein.Type.MinSpawnHeight) continue;
                Vector3 p = new Vector3(curr.X , curr.Y, curr.Z);

                var vox = new TemporaryVoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(p));

                if (!vox.IsValid || vox.IsEmpty) continue;

                if (!MathFunctions.RandEvent(vein.Type.SpawnProbability)) continue;

                if (!vein.Type.SpawnOnSurface && vox.Type.IsSurface) continue;

                vox.Type = vein.Type;
                Vector3 step = directionBias + MathFunctions.RandVector3Box(-1, 1, -1, 1, -1, 1)*0.25f;
                step.Normalize();
                curr += step;
            }
        }

        public void GenerateWater(VoxelChunk chunk)
        {
            int waterHeight = (int) (SeaLevel*VoxelConstants.ChunkSizeY) + 1;

            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
            {
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    var topVoxel = VoxelHelpers.FindFirstVoxelBelow(new TemporaryVoxelHandle(
                        chunk, new LocalVoxelCoordinate(x, VoxelConstants.ChunkSizeY - 1, z)));

                    for (var y = 0; y <= waterHeight; ++y)
                    {
                        var vox = new TemporaryVoxelHandle(chunk, new LocalVoxelCoordinate(x, y, z));
                        if (vox.IsEmpty && y > topVoxel.Coordinate.Y)
                        {
                            vox.WaterCell = new WaterCell
                            {
                                Type = LiquidType.Water,
                                WaterLevel = WaterManager.maxWaterLevel
                            };
                        }
                    }

                    Vector2 vec = new Vector2(x + chunk.Origin.X, z + chunk.Origin.Z) / WorldScale;


                    if (topVoxel.Coordinate.Y < VoxelConstants.ChunkSizeY - 1
                        && Overworld.GetWater(Overworld.Map, vec) == Overworld.WaterType.Volcano)
                    {
                        var localCoord = topVoxel.Coordinate.GetLocalVoxelCoordinate();
                        topVoxel = new TemporaryVoxelHandle(topVoxel.Chunk, new LocalVoxelCoordinate(
                            localCoord.X, localCoord.Y + 1, localCoord.Z));

                        if (topVoxel.IsEmpty)
                        { 
                            topVoxel.WaterCell = new WaterCell
                            {
                                Type = LiquidType.Lava,
                                WaterLevel = WaterManager.maxWaterLevel
                            };
                        }
                    }
                }
            }
        }

        public void GenerateLava(VoxelChunk chunk)
        {
            int lavaHeight = 2;

            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
            {
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    for (var y = 0; y < lavaHeight; ++y)
                    {
                        var voxel = new TemporaryVoxelHandle(chunk, new LocalVoxelCoordinate(x, y, z));
                        if (voxel.IsEmpty && voxel.WaterCell.WaterLevel == 0)
                            voxel.WaterCell = new WaterCell
                            {
                                Type = LiquidType.Lava,
                                WaterLevel = WaterManager.maxWaterLevel
                            };
                    }
                }
            }
        }

        public static BiomeData GetBiomeAt(Vector2 worldPosition)
        {
            var vec = worldPosition / WorldScale;
            var biome = Overworld.Map[(int)MathFunctions.Clamp(vec.X, 0, Overworld.Map.GetLength(0) - 1), (int)MathFunctions.Clamp(vec.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;
            return BiomeLibrary.Biomes[biome];
        }

        public static float GetValueAt(Vector3 worldPosition, Overworld.ScalarFieldType T)
        {
            Vector2 vec = new Vector2(worldPosition.X, worldPosition.Z) / WorldScale;
            return Overworld.GetValue(Overworld.Map, new Vector2(MathFunctions.Clamp(vec.X, 0, Overworld.Map.GetLength(0) - 1),
                MathFunctions.Clamp(vec.Y, 0, Overworld.Map.GetLength(1) - 1)), T);
        }

        public void GenerateSurfaceLife(VoxelChunk Chunk)
        {
            var waterHeight = (int)(SeaLevel * VoxelConstants.ChunkSizeY);

            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
            {
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    var biomeData = GetBiomeAt(new Vector2(x + Chunk.Origin.X, z + Chunk.Origin.Z));
                    var topVoxel = VoxelHelpers.FindFirstVoxelBelow(new TemporaryVoxelHandle(
                        Chunk, new LocalVoxelCoordinate(x, VoxelConstants.ChunkSizeY - 1, z)));

                    if (!topVoxel.IsValid 
                        || topVoxel.WaterCell.WaterLevel != 0 
                        || topVoxel.Coordinate.Y == 0
                        || topVoxel.Coordinate.Y >= 60) // Lift to some kind of generator settings?
                        continue;
                    
                    foreach (var animal in biomeData.Fauna)
                    {
                        if (MathFunctions.RandEvent(animal.SpawnProbability))
                        {
                            EntityFactory.CreateEntity<Body>(animal.Name, 
                                topVoxel.WorldPosition + Vector3.Up);

                            break;
                        }
                    }

                    if (topVoxel.Type.Name != biomeData.GrassLayer.VoxelType)
                        continue;

                    foreach (VegetationData veg in biomeData.Vegetation)
                    {
                        if (MathFunctions.RandEvent(veg.SpawnProbability) &&
                            NoiseGenerator.Noise(topVoxel.Coordinate.X / veg.ClumpSize,
                            veg.NoiseOffset, topVoxel.Coordinate.Y / veg.ClumpSize) >= veg.ClumpThreshold)
                        {
                            topVoxel.Type = VoxelLibrary.GetVoxelType(biomeData.SoilLayer.VoxelType);

                            float offset = veg.VerticalOffset;

                            // Vegetation doesn't shift with ramps - is this accomplishing anything?
                            if (topVoxel.RampType != RampType.None)
                                offset -= 0.25f;

                            var treeSize = MathFunctions.Rand() * veg.SizeVariance + veg.MeanSize;
                            EntityFactory.CreateEntity<Body>(veg.Name,
                                topVoxel.WorldPosition + (Vector3.Up * treeSize * offset) + Vector3.Up,
                                Blackboard.Create("Scale", treeSize));

                            break;
                        }
                    }
                }
            }
        }

        public void GenerateCaves(VoxelChunk chunk, WorldManager world)
        {
            Vector3 origin = chunk.Origin;
            BiomeData biome = BiomeLibrary.Biomes[Overworld.Biome.Cave];

            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    var topVoxel = VoxelHelpers.FindFirstVoxelBelow(new TemporaryVoxelHandle(
                        chunk, new LocalVoxelCoordinate(x, VoxelConstants.ChunkSizeY - 1, z)));

                    for (int i = 0; i < CaveLevels.Count; i++)
                    {
                        int y = CaveLevels[i];
                        if (y <= 0 || y >= topVoxel.Coordinate.Y) continue;
                        Vector3 vec = new Vector3(x, y, z) + chunk.Origin;
                        double caveNoise = CaveNoise.GetValue((x + origin.X) * CaveNoiseScale * CaveFrequencies[i],
                            (y + origin.Y) * CaveNoiseScale * 3.0f, (z + origin.Z) * CaveNoiseScale * CaveFrequencies[i]);

                        double heightnoise = NoiseGenerator.Noise((x + origin.X)*NoiseScale*CaveFrequencies[i],
                            (y + origin.Y) * NoiseScale * 3.0f, (z + origin.Z) * NoiseScale * CaveFrequencies[i]);

                        int caveHeight = Math.Min(Math.Max((int) (heightnoise*5), 1), 3);

                        if (!(caveNoise > CaveSize)) continue;

                        bool waterFound = false;
                        for (int dy = 0; dy < caveHeight; dy++)
                        {
                            // x, y, z are in local chunk space.
                            int index = VoxelConstants.DataIndexOf(new LocalVoxelCoordinate(x, y - dy, z));

                            waterFound = Neighbors.EnumerateManhattanNeighbors(chunk.ID + new LocalVoxelCoordinate(x, y - dy, z))
                                .Select(c => new TemporaryVoxelHandle(Manager.ChunkData, c))
                                .Any(v => v.IsValid && v.WaterCell.WaterLevel > 0);
                            
                            if (waterFound)
                                break;

                            chunk.Data.Types[index] = 0;
                        }

                        if (!waterFound && caveNoise > CaveSize*1.8f && y - caveHeight > 0)
                        {
                            GenerateCaveVegetation(chunk, x, y, z, caveHeight, biome, vec, world, NoiseGenerator);
                        }

                        GenerateCaveFauna(chunk, world, biome, y, x, z);
                    }
                }
            }
        }

        private static void GenerateCaveFauna(VoxelChunk chunk, WorldManager world, BiomeData biome, int y, int x, int z)
        {
            foreach (FaunaData animal in biome.Fauna)
            {
                if (y <= 0 || !(MathFunctions.Random.NextDouble() < animal.SpawnProbability))
                {
                    continue;
                }

                EntityFactory.DoLazy(() =>
                {
                    var entity = EntityFactory.CreateEntity<GameComponent>(animal.Name,
                        chunk.Origin + new Vector3(x, y, z) + Vector3.Up*1.0f);

                    if (GameSettings.Default.FogofWar)
                    {
                        entity.GetRoot().SetFlagRecursive(GameComponent.Flag.Active, false);
                        entity.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, false);
                        entity.AddChild(new ExploredListener
                            (world.ComponentManager,
                                world.ChunkManager, new TemporaryVoxelHandle(chunk, new LocalVoxelCoordinate(x, y, z))));                    }
                });
                break;
            }
        }

        public static void GenerateCaveVegetation(VoxelChunk chunk, int x, int y, int z, int caveHeight, BiomeData biome, Vector3 vec, WorldManager world, Perlin NoiseGenerator)
        {
            var vUnder = new TemporaryVoxelHandle(chunk, new LocalVoxelCoordinate(x, y - 1, z));

            int indexunder = VoxelConstants.DataIndexOf(new LocalVoxelCoordinate(x, y - caveHeight, z));
            chunk.Data.Types[indexunder] = (byte)VoxelLibrary.GetVoxelType(biome.GrassLayer.VoxelType).ID;
            chunk.Data.Health[indexunder] = (byte)VoxelLibrary.GetVoxelType(biome.GrassLayer.VoxelType).StartingHealth;
            
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


                if (!vUnder.IsEmpty && vUnder.Type.Name == biome.GrassLayer.VoxelType)
                {
                    vUnder.Type = VoxelLibrary.GetVoxelType(biome.SoilLayer.VoxelType);
                    float offset = veg.VerticalOffset;
                    if (vUnder.RampType != RampType.None)
                    {
                        offset -= 0.25f;
                    }
                    float treeSize = MathFunctions.Rand() * veg.SizeVariance + veg.MeanSize;

                    EntityFactory.DoLazy(() =>
                    {
                        GameComponent entity = EntityFactory.CreateEntity<GameComponent>(veg.Name,
                            chunk.Origin + new Vector3(x, y, z) + new Vector3(0, treeSize*offset, 0),
                            Blackboard.Create("Scale", treeSize));
                        entity.GetRoot().SetFlagRecursive(GameComponent.Flag.Active, false);
                        entity.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, false);
                        if (GameSettings.Default.FogofWar)
                            entity.AddChild(new ExploredListener(
                                world.ComponentManager, world.ChunkManager, vUnder));
                    });
                }
            }
        }

        public VoxelChunk GenerateChunk(Vector3 origin, WorldManager World)
        {
            float waterHeight = SeaLevel + 1.0f / VoxelConstants.ChunkSizeY;
            VoxelChunk c = new VoxelChunk(Manager, origin, GlobalVoxelCoordinate.FromVector3(origin).GetGlobalChunkCoordinate())
            {
                ShouldRebuild = true,
                ShouldRecalculateLighting = true
            };

            for(int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for(int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    Vector2 v = new Vector2(x + origin.X, z + origin.Z) / WorldScale;

                    Overworld.Biome biome = Overworld.Map[(int)MathFunctions.Clamp(v.X, 0, Overworld.Map.GetLength(0) - 1), (int)MathFunctions.Clamp(v.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;

                    BiomeData biomeData = BiomeLibrary.Biomes[biome];

                    Vector2 pos = new Vector2(x + origin.X, z + origin.Z) / WorldScale;
                    float hNorm = Overworld.LinearInterpolate(pos, Overworld.Map, Overworld.ScalarFieldType.Height);
                    float h = MathFunctions.Clamp(hNorm * VoxelConstants.ChunkSizeY, 0.0f, VoxelConstants.ChunkSizeY - 2);
                    int stoneHeight = (int)(MathFunctions.Clamp((int)(h - (biomeData.SoilLayer.Depth + (Math.Sin(v.X) + Math.Cos(v.Y)))), 1, h));

                    int currentSubsurfaceLayer = 0;
                    int depthWithinSubsurface = 0;
                    for(int y = VoxelConstants.ChunkSizeY - 1; y >= 0; y--)
                    {
                        var voxel = new TemporaryVoxelHandle(c, new LocalVoxelCoordinate(x, y, z));

                        if(y == 0)
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType("Bedrock");
                            voxel.Health = 8;
                            continue;
                        }

                        if(y <= stoneHeight && stoneHeight > 1)
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType(biomeData.SubsurfaceLayers[currentSubsurfaceLayer].VoxelType);
                            voxel.Health = VoxelLibrary.GetVoxelType(biomeData.SubsurfaceLayers[currentSubsurfaceLayer].VoxelType).StartingHealth;
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

                        else if((y == (int) h || y == stoneHeight) && hNorm > waterHeight)
                        {
                            if (biomeData.ClumpGrass &&
                                NoiseGenerator.Noise(pos.X/biomeData.ClumpSize, 0, pos.Y/biomeData.ClumpSize) >
                                biomeData.ClumpTreshold)
                            {
                                voxel.Type = VoxelLibrary.GetVoxelType(biomeData.GrassLayer.VoxelType);
                                voxel.Health = VoxelLibrary.GetVoxelType(biomeData.GrassLayer.VoxelType).StartingHealth;   
                            }
                            else if(!biomeData.ClumpGrass)
                            {
                                voxel.Type = VoxelLibrary.GetVoxelType(biomeData.GrassLayer.VoxelType);
                                voxel.Health = VoxelLibrary.GetVoxelType(biomeData.GrassLayer.VoxelType).StartingHealth; 
                            }
                            else
                            {
                                voxel.Type = VoxelLibrary.GetVoxelType(biomeData.SoilLayer.VoxelType);
                                voxel.Health = VoxelLibrary.GetVoxelType(biomeData.SoilLayer.VoxelType).StartingHealth;
                            }
                        }
                        else if(y > h && y > 0)
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType(0);
                        }
                        else if(hNorm <= waterHeight)
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType(biomeData.ShoreVoxel);
                            voxel.Health = VoxelLibrary.GetVoxelType(biomeData.ShoreVoxel).StartingHealth;
                        }
                        else
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType(biomeData.SoilLayer.VoxelType);
                            voxel.Health = VoxelLibrary.GetVoxelType(biomeData.SoilLayer.VoxelType).StartingHealth;
                        }
                    }
                }
            }

            GenerateWater(c);
            GenerateLava(c);
            GenerateCaves(c, World);

            c.ShouldRecalculateLighting = true;
            c.ShouldRebuildWater = true;
            return c;
        }
    }

}
