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
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Math = System.Math;

namespace DwarfCorp
{
    /// <summary>
    /// An OreCluster is a 3D ellipsoid of ores of a particular type.
    /// </summary>
    public struct OreCluster
    {
        /// <summary>
        /// Gets or sets the type of voxel.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public VoxelType Type { get; set; }
        /// <summary>
        /// Semi-major axes of the ellipse.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public Vector3 Size { get; set; }
        /// <summary>
        /// Rotates/translates the ellipse in the world.
        /// </summary>
        /// <value>
        /// The transform.
        /// </value>
        public Matrix Transform { get; set; }
    }

    /// <summary>
    /// An OreVein is a strip of a particular type of ore that snakes off
    /// in a random direction.
    /// </summary>
    public struct OreVein
    {
        /// <summary>
        /// Gets or sets the type of the ore.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public VoxelType Type { get; set; }
        /// <summary>
        /// Gets or sets the starting location of the ore in the world.
        /// </summary>
        /// <value>
        /// The start.
        /// </value>
        public Vector3 Start { get; set; }
        /// <summary>
        /// Gets or sets the length of the ore vein in voxels.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        public float Length { get; set; }
    }

    /// <summary>
    ///     Creates randomly generated voxel chunks using data from the Overworld.
    /// </summary>
    public class ChunkGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkGenerator"/> class.
        /// </summary>
        /// <param name="voxLibrary">The voxel library.</param>
        /// <param name="randomSeed">The random seed.</param>
        /// <param name="noiseScale">The scale of the perlin noise.</param>
        /// <param name="maxMountainHeight">Maximum height of the mountains [0 - 1]</param>
        public ChunkGenerator(VoxelLibrary voxLibrary, int randomSeed, float noiseScale, float maxMountainHeight)
        {
            NoiseGenerator = new Perlin(randomSeed);
            NoiseScale = noiseScale;

            MaxMountainHeight = maxMountainHeight;
            VoxLibrary = voxLibrary;
            CaveNoiseScale = noiseScale*10.0f;
            CaveSize = 0.03f;
            CaveLevels = new List<int> {4, 8, 11, 16};
            CaveFrequencies = new List<float> {0.5f, 0.7f, 0.9f, 1.0f};

            CaveNoise = new FastRidgedMultifractal(randomSeed)
            {
                Frequency = 0.5f,
                Lacunarity = 0.5f,
                NoiseQuality = NoiseQuality.Standard,
                OctaveCount = 1,
                Seed = randomSeed
            };

            AquiferLevels = new List<int> {5};

            AquiferSize = 0.02f;
            AquiferNoise = new FastRidgedMultifractal(randomSeed + 100)
            {
                Frequency = 0.25f,
                Lacunarity = 0.5f,
                NoiseQuality = NoiseQuality.Standard,
                OctaveCount = 1,
                Seed = randomSeed
            };

            LavaLevels = new List<int> {1, 2};
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

        /// <summary>
        /// Gets or sets the voxel library (list of all voxel types).
        /// </summary>
        /// <value>
        /// The voxel library.
        /// </value>
        public VoxelLibrary VoxLibrary { get; set; }
        /// <summary>
        /// Gets or sets the noise generator for creating Perlin noise.
        /// </summary>
        /// <value>
        /// The noise generator.
        /// </value>
        public Perlin NoiseGenerator { get; set; }
        /// <summary>
        /// Gets or sets the cave noise generator (used for populating caves)
        /// </summary>
        /// <value>
        /// The cave noise.
        /// </value>
        public FastRidgedMultifractal CaveNoise { get; set; }
        /// <summary>
        /// Gets or sets the Perlin noise scale.
        /// </summary>
        /// <value>
        /// The Perlin noise scale.
        /// </value>
        public float NoiseScale { get; set; }
        /// <summary>
        /// Gets or sets the Perlin noise scale for caves.
        /// </summary>
        /// <value>
        /// The cave noise scale.
        /// </value>
        public float CaveNoiseScale { get; set; }
        /// <summary>
        /// Gets or sets the sea level (height of the sea in proportion [0-1] of max chunk height)
        /// </summary>
        /// <value>
        /// The sea level.
        /// </value>
        public float SeaLevel { get; set; }
        /// <summary>
        /// Gets or sets the maximum height of mountains in proportion [0-1] of max chunk height)
        /// </summary>
        /// <value>
        /// The maximum height of the mountain.
        /// </value>
        public float MaxMountainHeight { get; set; }
        /// <summary>
        /// Gets or sets the Perlin noise generator for aquifers.
        /// </summary>
        /// <value>
        /// The aquifer noise.
        /// </value>
        public FastRidgedMultifractal AquiferNoise { get; set; }
        /// <summary>
        /// Gets or sets the Perlin noise generator for lava tubes.
        /// </summary>
        /// <value>
        /// The lava noise.
        /// </value>
        public FastRidgedMultifractal LavaNoise { get; set; }
        /// <summary>
        /// Gets or sets the chunk manager.
        /// </summary>
        /// <value>
        /// The chunk manager.
        /// </value>
        public ChunkManager Manager { get; set; }
        /// <summary>
        /// Gets or sets the list of Y levels on which there are cave systems.
        /// </summary>
        /// <value>
        /// The cave levels.
        /// </value>
        public List<int> CaveLevels { get; set; }
        /// <summary>
        /// Gets or sets the list of Perlin noise frequencies for each cave system.
        /// </summary>
        /// <value>
        /// The cave frequencies.
        /// </value>
        public List<float> CaveFrequencies { get; set; }
        /// <summary>
        /// Gets or sets the list of Y levels on which there are aquifers.
        /// </summary>
        /// <value>
        /// The aquifer levels.
        /// </value>
        public List<int> AquiferLevels { get; set; }
        /// <summary>
        /// Gets or sets the list of Y levels on which there are lava tubes.
        /// </summary>
        /// <value>
        /// The lava levels.
        /// </value>
        public List<int> LavaLevels { get; set; }
        /// <summary>
        /// Gets or sets the size of caves in percetage of [0-1], where 1 is maximum caviness.
        /// </summary>
        /// <value>
        /// The size of the cave.
        /// </value>
        public float CaveSize { get; set; }
        /// <summary>
        /// Gets or sets the size of the aquifers in percentage of [0-1] where 1 is maximum aquifers.
        /// </summary>
        /// <value>
        /// The size of the aquifer.
        /// </value>
        public float AquiferSize { get; set; }
        /// <summary>
        /// Gets or sets the size of the lava tubes in percentage [0-1] where 1 is maximum lava tube size.
        /// </summary>
        /// <value>
        /// The size of the lava.
        /// </value>
        public float LavaSize { get; set; }

        /// <summary>
        /// Generates an ore cluster.
        /// </summary>
        /// <param name="cluster">The ore cluster.</param>
        /// <param name="chunks">The chunks.</param>
        public void GenerateCluster(OreCluster cluster, ChunkData chunks)
        {
            var vox = new Voxel();
            for (float x = -cluster.Size.X*0.5f; x < cluster.Size.X*0.5f; x += 1.0f)
            {
                for (float y = -cluster.Size.Y*0.5f; y < cluster.Size.Y*0.5f; y += 1.0f)
                {
                    for (float z = -cluster.Size.Z*0.5f; z < cluster.Size.Z*0.5f; z += 1.0f)
                    {
                        var radius = (float) (Math.Pow(x/cluster.Size.X, 2.0f) + Math.Pow(y/cluster.Size.Y, 2.0f) +
                                              Math.Pow(z/cluster.Size.Z, 2.0f));

                        if (radius > 1.0f + MathFunctions.Rand(0.0f, 0.25f)) continue;
                        var locPosition = new Vector3(x, y, z);

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

        /// <summary>
        /// Generates an OreVein.
        /// </summary>
        /// <param name="vein">The vein.</param>
        /// <param name="chunks">The chunks.</param>
        public void GenerateVein(OreVein vein, ChunkData chunks)
        {
            var vox = new Voxel();
            Vector3 curr = vein.Start;
            Vector3 directionBias = MathFunctions.RandVector3Box(-1, 1, -0.1f, 0.1f, -1, 1);
            for (float t = 0; t < vein.Length; t++)
            {
                if (curr.Y > vein.Type.MaxSpawnHeight ||
                    curr.Y < vein.Type.MinSpawnHeight) continue;
                var p = new Vector3(curr.X, curr.Y, curr.Z);
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

        /// <summary>
        /// Generates an empty chunk.
        /// </summary>
        /// <param name="origin">The origin (leastmost coord) of the chunk.</param>
        /// <param name="chunkSizeX">The number of voxels in x.</param>
        /// <param name="chunkSizeY">The number of voxels in y.</param>
        /// <param name="chunkSizeZ">The number of voxels in z..</param>
        /// <returns>A new empty chunk at the given location.</returns>
        public VoxelChunk GenerateEmptyChunk(Vector3 origin, int chunkSizeX, int chunkSizeY, int chunkSizeZ)
        {
            return new VoxelChunk(Manager, origin, 1,
                Manager.ChunkData.GetChunkID(origin + new Vector3(0.5f, 0.5f, 0.5f)), chunkSizeX, chunkSizeY, chunkSizeZ);
        }

        /// <summary>
        /// Generates water for the given voxel chunk.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        public void GenerateWater(VoxelChunk chunk)
        {
            var waterHeight = (int) (SeaLevel*chunk.SizeY);
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
                            chunk.Data.Water[index].WaterLevel = 8;
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

                    chunk.Data.Water[chunk.Data.IndexAt(x, h, z)].WaterLevel = 8;
                    chunk.Data.Water[chunk.Data.IndexAt(x, h, z)].Type = LiquidType.Lava;
                }
            }
        }

        /// <summary>
        /// Generates lava for the given voxel chunk.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
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
                            chunk.Data.Water[voxel.Index].WaterLevel = 8;
                            chunk.Data.Water[voxel.Index].Type = LiquidType.Lava;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the biome at the specified world location.
        /// </summary>
        /// <param name="worldPosition">The world position.</param>
        /// <returns>The Biome at that location.</returns>
        public static BiomeData GetBiomeAt(Vector3 worldPosition)
        {
            Vector2 vec = new Vector2(worldPosition.X, worldPosition.Z)/PlayState.WorldScale;
            Overworld.Biome biome =
                Overworld.Map[
                    (int) MathFunctions.Clamp(vec.X, 0, Overworld.Map.GetLength(0) - 1),
                    (int) MathFunctions.Clamp(vec.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;
            return BiomeLibrary.Biomes[biome];
        }

        /// <summary>
        /// Gets the scalar field value at the given world location from the Overworld.
        /// </summary>
        /// <param name="worldPosition">The world position.</param>
        /// <param name="T">The scalar field type</param>
        /// <returns>The value of the scalar field at the world position.</returns>
        public static float GetValueAt(Vector3 worldPosition, Overworld.ScalarFieldType T)
        {
            Vector2 vec = new Vector2(worldPosition.X, worldPosition.Z)/PlayState.WorldScale;
            return Overworld.GetValue(Overworld.Map,
                new Vector2(MathFunctions.Clamp(vec.X, 0, Overworld.Map.GetLength(0) - 1),
                    MathFunctions.Clamp(vec.Y, 0, Overworld.Map.GetLength(1) - 1)), T);
        }

        /// <summary>
        /// Generates the fauna (animals) of the chunk.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        /// <param name="components">The components.</param>
        /// <param name="content">The content manager.</param>
        /// <param name="graphics">The graphics.</param>
        /// <param name="factions">The factions.</param>
        public void GenerateFauna(VoxelChunk chunk, ComponentManager components, ContentManager content,
            GraphicsDevice graphics, FactionLibrary factions)
        {
            var waterHeight = (int) (SeaLevel*chunk.SizeY);
            Voxel v = chunk.MakeVoxel(0, 0, 0);

            // For each voxel in the XZ plane.
            for (int x = 0; x < chunk.SizeX; x++)
            {
                for (int z = 0; z < chunk.SizeZ; z++)
                {
                    // Get the biome at that location.
                    Vector2 vec = new Vector2(x + chunk.Origin.X, z + chunk.Origin.Z)/PlayState.WorldScale;
                    Overworld.Biome biome =
                        Overworld.Map[
                            (int) MathFunctions.Clamp(vec.X, 0, Overworld.Map.GetLength(0) - 1),
                            (int) MathFunctions.Clamp(vec.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;
                    BiomeData biomeData = BiomeLibrary.Biomes[biome];

                    // Get the ground height at that location.
                    int y = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);

                    // If there's no ground, generate no animals.
                    if (!chunk.IsCellValid(x, (int) (y - chunk.Origin.Y), z))
                    {
                        continue;
                    }

                    // Update the voxel reference.
                    v.GridPosition = new Vector3(x, y, z);

                    // If the voxel is underwater, don't generate an animal there.
                    if (chunk.Data.Water[v.Index].WaterLevel != 0 || y <= waterHeight)
                    {
                        continue;
                    }

                    // Generate an animal with some probability.
                    foreach (FaunaData animal in biomeData.Fauna)
                    {
                        if (y <= 0 || !(PlayState.Random.NextDouble() < animal.SpawnProbability))
                        {
                            continue;
                        }

                        EntityFactory.CreateEntity<Body>(animal.Name,
                            chunk.Origin + new Vector3(x, y, z) + Vector3.Up*1.0f);

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Generates the vegetation (plants) in the chunk.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        /// <param name="components">The component manager.</param>
        /// <param name="content">The content manager</param>
        /// <param name="graphics">The graphics.</param>
        public void GenerateVegetation(VoxelChunk chunk, ComponentManager components, ContentManager content,
            GraphicsDevice graphics)
        {
            var waterHeight = (int) (SeaLevel*chunk.SizeY);
            bool updated = false;
            Voxel v = chunk.MakeVoxel(0, 0, 0);
            Voxel vUnder = chunk.MakeVoxel(0, 0, 0);

            // For each voxel in the X Z plane
            for (int x = 0; x < chunk.SizeX; x++)
            {
                for (int z = 0; z < chunk.SizeZ; z++)
                {
                    // Find the biome there.
                    Vector2 vec = new Vector2(x + chunk.Origin.X, z + chunk.Origin.Z)/PlayState.WorldScale;
                    Overworld.Biome biome =
                        Overworld.Map[
                            (int) MathFunctions.Clamp(vec.X, 0, Overworld.Map.GetLength(0) - 1),
                            (int) MathFunctions.Clamp(vec.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;
                    BiomeData biomeData = BiomeLibrary.Biomes[biome];

                    // Find the ground height at tha location.
                    int y = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);

                    // If there isn't any ground, don't generate vegetation.
                    if (!chunk.IsCellValid(x, (int) (y - chunk.Origin.Y), z))
                    {
                        continue;
                    }

                    // Update the voxel
                    v.GridPosition = new Vector3(x, y, z);

                    // If underwater, don't generate vegetatin.
                    if (!v.IsEmpty || chunk.Data.Water[v.Index].WaterLevel != 0 || y <= waterHeight)
                    {
                        continue;
                    }

                    // Generate a plant with some propability.
                    foreach (VegetationData veg in biomeData.Vegetation)
                    {
                        if (y <= 0)
                        {
                            break;
                        }

                        // Raw probability of generating vegetation.
                        if (!MathFunctions.RandEvent(veg.SpawnProbability))
                        {
                            continue;
                        }

                        // Ensure vegetation clumps together.
                        if (NoiseGenerator.Noise(vec.X/veg.ClumpSize, veg.NoiseOffset, vec.Y/veg.ClumpSize) <
                            veg.ClumpThreshold)
                        {
                            continue;
                        }

                        int yh = chunk.GetFilledVoxelGridHeightAt(x, y, z);

                        if (yh > 0)
                        {
                            vUnder.GridPosition = new Vector3(x, yh - 1, z);
                            // Only generate plants on grass.
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

                                // Finally, create the plant.
                                EntityFactory.CreateEntity<Body>(veg.Name,
                                    chunk.Origin + new Vector3(x, y, z) + new Vector3(0, treeSize*offset, 0),
                                    Blackboard.Create("Scale", treeSize));
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

        /// <summary>
        /// Generates the lava tubes for the specified chunk.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        public void GenerateLavaTubes(VoxelChunk chunk)
        {
            Vector3 origin = chunk.Origin;
            int chunkSizeX = chunk.SizeX;
            int chunkSizeZ = chunk.SizeZ;

            for (int x = 0; x < chunkSizeX; x++)
            {
                for (int z = 0; z < chunkSizeZ; z++)
                {
                    int h = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);
                    // For each lava tube system
                    foreach (int y in LavaLevels)
                    {
                        if (y <= 0 || y >= h) continue;

                        // Get a scalar value from the noise field.
                        double caveNoise = LavaNoise.GetValue((x + origin.X)*CaveNoiseScale, (y + origin.Y)*3.0f,
                            (z + origin.Z)*CaveNoiseScale);

                        // Generate a block of lava there.
                        if (caveNoise > LavaSize)
                        {
                            chunk.Data.Types[chunk.Data.IndexAt(x, y, z)] = 0;
                            chunk.Data.Water[chunk.Data.IndexAt(x, y, z)].WaterLevel = 8;
                            chunk.Data.Water[chunk.Data.IndexAt(x, y, z)].Type = LiquidType.Lava;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates the caves for the specified chunk.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        public void GenerateCaves(VoxelChunk chunk)
        {
            Vector3 origin = chunk.Origin;
            int chunkSizeX = chunk.SizeX;
            int chunkSizeZ = chunk.SizeZ;
            BiomeData biome = BiomeLibrary.Biomes[Overworld.Biome.Cave];
            var neighbors = new List<Voxel>();
            Voxel vUnder = chunk.MakeVoxel(0, 0, 0);
            for (int x = 0; x < chunkSizeX; x++)
            {
                for (int z = 0; z < chunkSizeZ; z++)
                {
                    int h = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);
                    // For each cave system level.
                    for (int i = 0; i < CaveLevels.Count; i++)
                    {
                        int y = CaveLevels[i];
                        if (y <= 0 || y >= h - 1) continue;
                        Vector3 vec = new Vector3(x, y, z) + chunk.Origin;

                        // Scalar value representing caviness
                        double caveNoise = CaveNoise.GetValue((x + origin.X)*CaveNoiseScale*CaveFrequencies[i],
                            (y + origin.Y)*CaveNoiseScale*3.0f, (z + origin.Z)*CaveNoiseScale*CaveFrequencies[i]);

                        // Scalar value representing cave height
                        double heightnoise = NoiseGenerator.Noise((x + origin.X)*NoiseScale*CaveFrequencies[i],
                            (y + origin.Y)*NoiseScale*3.0f, (z + origin.Z)*NoiseScale*CaveFrequencies[i]);

                        // Convert cave height scalar into a number of voxels between 1 and 3.
                        int caveHeight = Math.Min(Math.Max((int) (heightnoise*5), 1), 3);

                        // Only generate caves if the noise is sufficiently large.
                        if (!(caveNoise > CaveSize)) continue;

                        // Search for any water in the cave. If there is water, we don't want to put a cave there,
                        // because it would result in serious flooding that slows down the game.
                        bool waterFound = false;
                        for (int dy = 0; dy < caveHeight; dy++)
                        {
                            int index = chunk.Data.IndexAt(x, y - dy, z);
                            chunk.GetNeighborsManhattan(x, y - dy, z, neighbors);

                            if (neighbors.Any(v => v.WaterLevel > 0))
                            {
                                waterFound = true;
                            }

                            if (waterFound)
                                break;

                            chunk.Data.Types[index] = 0;
                        }

                        // Now, create a cave voxel there.
                        if (!waterFound && caveNoise > CaveSize*1.8f && y - caveHeight > 0)
                        {
                            // Place cave fungus
                            int indexunder = chunk.Data.IndexAt(x, y - caveHeight, z);
                            chunk.Data.Types[indexunder] = (byte) VoxelLibrary.GetVoxelType(biome.GrassVoxel).ID;
                            chunk.Data.Health[indexunder] =
                                (byte) VoxelLibrary.GetVoxelType(biome.GrassVoxel).StartingHealth;
                            
                            // Caves start out unexplored.
                            chunk.Data.IsExplored[indexunder] = false;

                            // Generate vegetation in the cave.
                            foreach (VegetationData veg in biome.Vegetation)
                            {
                                // Raw probability for spawning cave vegetation.
                                if (!MathFunctions.RandEvent(veg.SpawnProbability))
                                {
                                    continue;
                                }

                                // Clump cave vegetation together.
                                if (
                                    NoiseGenerator.Noise(vec.X/veg.ClumpSize, veg.NoiseOffset, vec.Y/veg.ClumpSize) <
                                    veg.ClumpThreshold)
                                {
                                    continue;
                                }


                                vUnder.GridPosition = new Vector3(x, y - 1, z);
                                if (vUnder.IsEmpty || vUnder.TypeName != biome.GrassVoxel) continue;

                                vUnder.Type = VoxelLibrary.GetVoxelType(biome.SoilVoxel);
                                float offset = veg.VerticalOffset;
                                if (vUnder.RampType != RampType.None)
                                {
                                    offset -= 0.25f;
                                }
                                float treeSize = MathFunctions.Rand()*veg.SizeVariance + veg.MeanSize;

                                // Put an entity in the cave.
                                var entity = EntityFactory.CreateEntity<GameComponent>(veg.Name,
                                    chunk.Origin + new Vector3(x, y, z) + new Vector3(0, treeSize*offset, 0),
                                    Blackboard.Create("Scale", treeSize));

                                if (GameSettings.Default.FogofWar)
                                {
                                    // It starts out invisible if it is underground and fog of war is enabled.
                                    entity.GetRootComponent().SetActiveRecursive(false);
                                    entity.GetRootComponent().SetVisibleRecursive(false);
                                    // Reveal the component if the voxel is explored.
                                    var listener = new ExploredListener(
                                        PlayState.ComponentManager, entity, PlayState.ChunkManager, vUnder);
                                }
                            }
                        }

                        // Generate cave animals.
                        foreach (FaunaData animal in biome.Fauna)
                        {
                            // Raw spawn probability
                            if (y <= 0 || !(PlayState.Random.NextDouble() < animal.SpawnProbability))
                            {
                                continue;
                            }

                            // Generate the entity.
                            var entity = EntityFactory.CreateEntity<GameComponent>(animal.Name,
                                chunk.Origin + new Vector3(x, y, z) + Vector3.Up*1.0f);

                            // If fog of war is on, make the entity invisible and only turn it on
                            // if the cave gets explored.
                            if (GameSettings.Default.FogofWar)
                            {
                                entity.GetRootComponent().SetActiveRecursive(false);
                                entity.GetRootComponent().SetVisibleRecursive(false);
                                var listener = new ExploredListener(PlayState.ComponentManager, entity,
                                    PlayState.ChunkManager, chunk.MakeVoxel(x, y, z));
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates the aquifers for the chunk.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        public void GenerateAquifers(VoxelChunk chunk)
        {
            Vector3 origin = chunk.Origin;
            int chunkSizeX = chunk.SizeX;
            int chunkSizeZ = chunk.SizeZ;
            for (int x = 0; x < chunkSizeX; x++)
            {
                for (int z = 0; z < chunkSizeZ; z++)
                {
                    int h = chunk.GetFilledVoxelGridHeightAt(x, chunk.SizeY - 1, z);
                    for (int i = 0; i < AquiferLevels.Count; i++)
                    {
                        int y = AquiferLevels[i];
                        if (y <= 0 || y >= h) continue;

                        double caveNoise = AquiferNoise.GetValue((x + origin.X)*CaveNoiseScale, (y + origin.Y)*3.0f,
                            (z + origin.Z)*CaveNoiseScale);


                        if (caveNoise > AquiferSize)
                        {
                            chunk.Data.Types[chunk.Data.IndexAt(x, y, z)] = 0;
                            chunk.Data.Water[chunk.Data.IndexAt(x, y, z)].WaterLevel = 8;
                            chunk.Data.Water[chunk.Data.IndexAt(x, y, z)].Type = LiquidType.Water;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Allocates data for the chunk.
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="chunkSizeX">The chunk size x.</param>
        /// <param name="chunkSizeY">The chunk size y.</param>
        /// <param name="chunkSizeZ">The chunk size z.</param>
        /// <returns>A 3D array containing data of type T.</returns>
        public static T[][][] Allocate<T>(int chunkSizeX, int chunkSizeY, int chunkSizeZ)
        {
            var voxels = new T[chunkSizeX][][];

            for (int x = 0; x < chunkSizeX; x++)
            {
                voxels[x] = new T[chunkSizeY][];
                for (int y = 0; y < chunkSizeY; y++)
                {
                    voxels[x][y] = new T[chunkSizeZ];
                }
            }

            return voxels;
        }

        /// <summary>
        /// Allocates a voxel grid of the given size.
        /// </summary>
        /// <param name="chunkSizeX">The chunk size x.</param>
        /// <param name="chunkSizeY">The chunk size y.</param>
        /// <param name="chunkSizeZ">The chunk size z.</param>
        /// <returns>A 3D array of voxels of the given size.</returns>
        public static Voxel[][][] Allocate(int chunkSizeX, int chunkSizeY, int chunkSizeZ)
        {
            var voxels = new Voxel[chunkSizeX][][];

            for (int x = 0; x < chunkSizeX; x++)
            {
                voxels[x] = new Voxel[chunkSizeY][];
                for (int y = 0; y < chunkSizeY; y++)
                {
                    voxels[x][y] = new Voxel[chunkSizeZ];
                }
            }

            return voxels;
        }

        /// <summary>
        /// Creates a new randomly generated chunk.
        /// </summary>
        /// <param name="origin">The origin (leastmost corner) of the chunk..</param>
        /// <param name="chunkSizeX">The number of voxels in x.</param>
        /// <param name="chunkSizeY">The number of voxels in y.</param>
        /// <param name="chunkSizeZ">The number of voxels in z.</param>
        /// <param name="components">The component manager.</param>
        /// <param name="content">The content manager.</param>
        /// <param name="graphics">The graphics.</param>
        /// <returns>A new voxel chunk of the given size.</returns>
        public VoxelChunk GenerateChunk(Vector3 origin, int chunkSizeX, int chunkSizeY, int chunkSizeZ,
            ComponentManager components, ContentManager content, GraphicsDevice graphics)
        {
            // Generate water below this height.
            float waterHeight = SeaLevel;
            // Create a new chunk.
            var c = new VoxelChunk(Manager, origin, 1,
                Manager.ChunkData.GetChunkID(origin + new Vector3(0.5f, 0.5f, 0.5f)), chunkSizeX, chunkSizeY, chunkSizeZ)
            {
                ShouldRebuild = true,
                ShouldRecalculateLighting = true
            };

            // Keep track of a voxel moving through the chunk
            Voxel voxel = c.MakeVoxel(0, 0, 0);
            
            // For each voxel in the XZ plane.
            for (int x = 0; x < chunkSizeX; x++)
            {
                for (int z = 0; z < chunkSizeZ; z++)
                {
                    // 2D vector representing the position of the voxel in the overworld.
                    Vector2 v = new Vector2(x + origin.X, z + origin.Z)/PlayState.WorldScale;

                    // The biome at v.
                    Overworld.Biome biome =
                        Overworld.Map[
                            (int) MathFunctions.Clamp(v.X, 0, Overworld.Map.GetLength(0) - 1),
                            (int) MathFunctions.Clamp(v.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;

                    // The data associated with the biome.
                    BiomeData biomeData = BiomeLibrary.Biomes[biome];

                    // The position of the voxel in the world.
                    Vector2 pos = new Vector2(x + origin.X, z + origin.Z)/PlayState.WorldScale;
                    
                    // Value between 0-1 representing he height of the ground at that point.
                    float hNorm = Overworld.LinearInterpolate(pos, Overworld.Map, Overworld.ScalarFieldType.Height);
                    
                    // The height in voxels of the ground.
                    float h = MathFunctions.Clamp(hNorm*chunkSizeY, 0.0f, chunkSizeY - 2);
                    
                    // The height of the stone layers in voxels.
                    var stoneHeight = (int) Math.Max(h - 2, 1);

                    // Now for every voxel in the Y column.
                    for (int y = 0; y < chunkSizeY; y++)
                    {
                        // Set the voxel's position.
                        voxel.GridPosition = new Vector3(x, y, z);

                        // Invincible voxel at the bottom of the world.
                        if (y == 0)
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType("Bedrock");
                            voxel.Health = 8;
                            continue;
                        }

                        // Stones.
                        if (y <= stoneHeight && stoneHeight > 1)
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType(biomeData.SubsurfVoxel);
                            voxel.Health = VoxelLibrary.GetVoxelType(biomeData.SubsurfVoxel).StartingHealth;
                        }
                        // Surface voxels.
                        else if ((y == (int) h || y == stoneHeight) && hNorm > waterHeight)
                        {
                            // Grass voxels in clumps.
                            if (biomeData.ClumpGrass &&
                                NoiseGenerator.Noise(pos.X/biomeData.ClumpSize, 0, pos.Y/biomeData.ClumpSize) >
                                biomeData.ClumpTreshold)
                            {
                                voxel.Type = VoxelLibrary.GetVoxelType(biomeData.GrassVoxel);
                                voxel.Health = VoxelLibrary.GetVoxelType(biomeData.GrassVoxel).StartingHealth;
                            }
                            // Grass voxels not in clumps.
                            else if (!biomeData.ClumpGrass)
                            {
                                voxel.Type = VoxelLibrary.GetVoxelType(biomeData.GrassVoxel);
                                voxel.Health = VoxelLibrary.GetVoxelType(biomeData.GrassVoxel).StartingHealth;
                            }
                            // Soil voxels.
                            else
                            {
                                voxel.Type = VoxelLibrary.GetVoxelType(biomeData.SoilVoxel);
                                voxel.Health = VoxelLibrary.GetVoxelType(biomeData.SoilVoxel).StartingHealth;
                            }
                        }
                        // Empty voxels.
                        else if (y > h && y > 0)
                        {
                            // Voxel is already empty!
                            //voxel.Type = VoxelLibrary.GetVoxelType("empty");
                        }
                        // Water under the sea.
                        else if (hNorm < waterHeight)
                        {
                            voxel.Type = VoxelLibrary.GetVoxelType(biomeData.ShoreVoxel);
                            voxel.Health = VoxelLibrary.GetVoxelType(biomeData.ShoreVoxel).StartingHealth;
                        }
                        // Soil (dirt) is anything that remains.
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
            // TODO(mklingen): these were complicating things in caves. 
            // try to figure out how to do this without flooding the world.
            //GenerateAquifers(c);
            //GenerateLavaTubes(c);


            c.ShouldRecalculateLighting = true;
            c.ShouldRebuildWater = true;
            return c;
        }
    }
}