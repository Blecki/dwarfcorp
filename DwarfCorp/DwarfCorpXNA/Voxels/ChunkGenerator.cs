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
        public ChunkManager Manager { get; set; }
        public List<float> CaveFrequencies { get; set; }
        public List<int> AquiverLevels { get; set; }
        public float CaveSize { get; set; }
        public float AquiferSize { get; set; }
        public int HellLevel = 10;
        public int LavaLevel = 5;

        private List<int> CaveLevels = null;

        public ChunkGenerator(VoxelLibrary voxLibrary, int randomSeed, float noiseScale)
        {
            NoiseGenerator = new Perlin(randomSeed);
            NoiseScale = noiseScale;

            VoxLibrary = voxLibrary;
            CaveNoiseScale = noiseScale * 10.0f;
            CaveSize = 0.03f;
            CaveFrequencies = new List<float>() { 0.5f, 0.7f, 0.9f, 1.0f };

            CaveNoise = new FastRidgedMultifractal(randomSeed)
            {
                Frequency = 0.5f,
                Lacunarity = 0.5f,
                NoiseQuality = NoiseQuality.Standard,
                OctaveCount = 1,
                Seed = randomSeed
            };

            AquiverLevels = new List<int>() { 5 };

            AquiferSize = 0.02f;
            AquiferNoise = new FastRidgedMultifractal(randomSeed + 100)
            {
                Frequency = 0.25f,
                Lacunarity = 0.5f,
                NoiseQuality = NoiseQuality.Standard,
                OctaveCount = 1,
                Seed = randomSeed
            };
        }

        public void GenerateCluster(OreCluster cluster, ChunkData chunks)
        {
            for (float x = -cluster.Size.X * 0.5f; x < cluster.Size.X * 0.5f; x += 1.0f)
            {
                for (float y = -cluster.Size.Y * 0.5f; y < cluster.Size.Y * 0.5f; y += 1.0f)
                {
                    for (float z = -cluster.Size.Z * 0.5f; z < cluster.Size.Z * 0.5f; z += 1.0f)
                    {
                        float radius = (float)(Math.Pow(x / cluster.Size.X, 2.0f) + Math.Pow(y / cluster.Size.Y, 2.0f) +
                                       Math.Pow(z / cluster.Size.Z, 2.0f));

                        if (radius > 1.0f + MathFunctions.Rand(0.0f, 0.25f)) continue;
                        Vector3 locPosition = new Vector3(x, y, z);

                        Vector3 globalPosition = Vector3.Transform(locPosition, cluster.Transform);

                        if (globalPosition.Y > cluster.Type.MaxSpawnHeight ||
                            globalPosition.Y < cluster.Type.MinSpawnHeight || 
                            globalPosition.Y <= 1) continue;

                        var vox = new VoxelHandle(chunks,
                            GlobalVoxelCoordinate.FromVector3(globalPosition));

                        if (!vox.IsValid || vox.IsEmpty) continue;

                        if (!cluster.Type.SpawnOnSurface && (vox.Type.IsSurface || vox.Type.IsSoil)) continue;

                        if (!MathFunctions.RandEvent(cluster.Type.SpawnProbability)) continue;

                        vox.RawSetType(cluster.Type);
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
                    curr.Y < vein.Type.MinSpawnHeight || 
                    curr.Y <= 1) continue;
                Vector3 p = new Vector3(curr.X, curr.Y, curr.Z);

                var vox = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(p));

                if (!vox.IsValid || vox.IsEmpty) continue;

                if (!MathFunctions.RandEvent(vein.Type.SpawnProbability)) continue;

                if (!vein.Type.SpawnOnSurface && (vox.Type.IsSurface || vox.Type.IsSoil)) continue;

                vox.RawSetType(vein.Type);
                Vector3 step = directionBias + MathFunctions.RandVector3Box(-1, 1, -1, 1, -1, 1) * 0.25f;
                step.Normalize();
                curr += step;
            }
        }

        public void GenerateWater(VoxelChunk chunk, float maxHeight)
        {
            int waterHeight = Math.Min((int)(VoxelConstants.ChunkSizeY * NormalizeHeight(SeaLevel + 1.0f / VoxelConstants.ChunkSizeY, maxHeight)), VoxelConstants.ChunkSizeY - 1);
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
                                vox.QuickSetLiquid(LiquidType.Water, WaterManager.maxWaterLevel);
                        }
                    }

                    Vector2 vec = Overworld.WorldToOverworld(new Vector2(x + chunk.Origin.X, z + chunk.Origin.Z), chunk.Manager.World.WorldScale, chunk.Manager.World.WorldOrigin);


                    if (topVoxel.Coordinate.Y < VoxelConstants.ChunkSizeY - 1
                        && Overworld.GetWater(Overworld.Map, vec) == Overworld.WaterType.Volcano)
                    {
                        var localCoord = topVoxel.Coordinate.GetLocalVoxelCoordinate();
                        topVoxel = new VoxelHandle(topVoxel.Chunk, new LocalVoxelCoordinate(
                            localCoord.X, localCoord.Y + 1, localCoord.Z));

                        if (topVoxel.IsEmpty)
                            topVoxel.QuickSetLiquid(LiquidType.Lava, WaterManager.maxWaterLevel);
                    }
                }
            }
        }

        public void GenerateLava(VoxelChunk chunk)
        {
            int lavaHeight = LavaLevel;

            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
            {
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    for (var y = 0; y < lavaHeight; ++y)
                    {
                        var voxel = new VoxelHandle(chunk, new LocalVoxelCoordinate(x, y, z));
                        if (voxel.IsEmpty && voxel.LiquidLevel == 0)
                            voxel.QuickSetLiquid(LiquidType.Lava, WaterManager.maxWaterLevel);
                    }
                }
            }
        }

        public void GenerateSurfaceLife(VoxelChunk Chunk, float maxHeight)
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
                            EntityFactory.CreateEntity<Body>(animal.Name,
                                topVoxel.WorldPosition + Vector3.Up *1.5f);

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
                            NoiseGenerator.Noise(topVoxel.Coordinate.X / veg.ClumpSize,
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

        public void GenerateCaves(VoxelChunk chunk, WorldManager world)
        {
            if (CaveLevels == null)
            {
                CaveLevels = new List<int>();
                var caveStep = 48 / world.GenerationSettings.NumCaveLayers;

                for (var i = 0; i < world.GenerationSettings.NumCaveLayers; ++i)
                    CaveLevels.Add(4 + (caveStep * i));
            }

            Vector3 origin = chunk.Origin;
            BiomeData biome = BiomeLibrary.GetBiome("Cave");
            var hellBiome = BiomeLibrary.GetBiome("Hell");

            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    var topVoxel = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                        chunk, new LocalVoxelCoordinate(x, VoxelConstants.ChunkSizeY - 1, z)));

                    for (int i = 0; i < CaveLevels.Count; i++)
                    {
                        int y = CaveLevels[i];
                        if (y <= 0 || y >= topVoxel.Coordinate.Y) continue;

                        var frequency = i < CaveFrequencies.Count ? CaveFrequencies[i] : CaveFrequencies[CaveFrequencies.Count - 1];
                        var caveBiome = (y <= HellLevel) ? hellBiome : biome;

                        Vector3 vec = new Vector3(x, y, z) + chunk.Origin;
                        double caveNoise = CaveNoise.GetValue((x + origin.X) * CaveNoiseScale * frequency,
                            (y + origin.Y) * CaveNoiseScale * 3.0f, (z + origin.Z) * CaveNoiseScale * frequency);

                        double heightnoise = NoiseGenerator.Noise((x + origin.X) * NoiseScale * frequency,
                            (y + origin.Y) * NoiseScale * 3.0f, (z + origin.Z) * NoiseScale * frequency);

                        int caveHeight = Math.Min(Math.Max((int)(heightnoise * 5), 1), 3);

                        if (!(caveNoise > CaveSize)) continue;

                        bool invalidCave = false;
                        for (int dy = 0; dy < caveHeight; dy++)
                        {
                            if (y - dy <= 0)
                                continue;

                            var voxel = new VoxelHandle(chunk, new LocalVoxelCoordinate(x, y - dy, z));

                            foreach (var coord in VoxelHelpers.EnumerateAllNeighbors(voxel.Coordinate))
                            {
                                VoxelHandle v = new VoxelHandle(Manager.ChunkData, coord);
                                if (v.IsValid && (v.LiquidLevel > 0 || v.Sunlight))
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

                        if (!invalidCave && caveNoise > CaveSize * 1.8f && y - caveHeight > 0 && y > LavaLevel)
                        {
                            GenerateCaveVegetation(chunk, x, y, z, caveHeight, caveBiome, vec, world, NoiseGenerator);
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

            foreach (FaunaData animal in biome.Fauna)
            {
                if (y <= 0 || !(MathFunctions.Random.NextDouble() < animal.SpawnProbability))
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

        public VoxelChunk GenerateChunk(Vector3 origin, WorldManager World, float maxHeight)
        {
            float waterHeight = NormalizeHeight(SeaLevel + 1.0f / VoxelConstants.ChunkSizeY, maxHeight);
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
                                NoiseGenerator.Noise(pos.X / biomeData.ClumpSize, 0, pos.Y / biomeData.ClumpSize) >
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

            UpdateSunlight(c);
            GenerateCaves(c, World);
            GenerateWater(c, maxHeight);
            GenerateLava(c);

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
                        v.Sunlight = true;
                        if (v.Type.ID != 0 && !v.Type.IsTransparent)
                            break;
                    }

                    for (y -= 1; y >= 0; y--)
                    {
                        var v = new VoxelHandle(Chunk, new LocalVoxelCoordinate(x, y, z));
                        v.Sunlight = false;
                    }
                }
            }
        }
    }
}
