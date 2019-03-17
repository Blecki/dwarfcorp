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
        // Todo: This needs to run on a single chunk at a time.
        public static void GenerateRuins(ChunkData chunks, WorldManager world, GeneratorSettings Settings)
        {
            int numRuinClusters = Math.Max(MathFunctions.RandInt(-6, 4), 0);

            for (int i = 0; i < numRuinClusters; i++)
            {
                var clusterChunk = Datastructures.SelectRandom(chunks.ChunkMap);
                var bounds = clusterChunk.GetBoundingBox();
                var centerLoc = MathFunctions.RandVector3Box(bounds);
                var clusterDensity = (MathFunctions.Rand() + 1.0f) * 40;
                int numStructures = MathFunctions.RandInt(1, 5);

                for (int j = 0; j < numStructures; j++)
                {
                    int structureWidth = MathFunctions.RandInt(4, 16);
                    int structureDepth = MathFunctions.RandInt(4, 16);
                    int wallHeight = MathFunctions.RandInt(2, 6);
                    int heightOffset = MathFunctions.RandInt(-4, 2);
                    var origin = centerLoc + MathFunctions.RandVector3Cube() * clusterDensity;

                    BiomeData biome = BiomeLibrary.Biomes[0];

                    int avgHeight = 0;
                    int numHeight = 0;
                    for (int dx = 0; dx < structureWidth; dx++)
                    {
                        for (int dz = 0; dz < structureDepth; dz++)
                        {
                            Vector3 worldPos = new Vector3(origin.X + dx, VoxelConstants.ChunkSizeY - 1, origin.Z + dz);

                            var baseVoxel = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                                chunks, GlobalVoxelCoordinate.FromVector3(worldPos)));

                            if (!baseVoxel.IsValid)
                                continue;

                            biome = Overworld.GetBiomeAt(worldPos, world.WorldScale, world.WorldOrigin);

                            var h = baseVoxel.Coordinate.Y + 1;
                            avgHeight += h;
                            numHeight++;
                        }
                    }

                    if (numHeight == 0)
                        continue;
                    avgHeight = avgHeight / numHeight;

                    bool[] doors = new bool[4];
                   
                    for (int k = 0; k < 4; k++)
                    {
                        doors[k] = MathFunctions.RandEvent(0.5f);
                    }

                    for (int dx = 0; dx < structureWidth; dx++)
                    {
                        for (int dz = 0; dz < structureDepth; dz++)
                        {
                            Vector3 worldPos = new Vector3(origin.X + dx, avgHeight + heightOffset, origin.Z + dz);

                            var baseVoxel = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(worldPos));
                            var underVoxel = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                               chunks, GlobalVoxelCoordinate.FromVector3(worldPos)));
                            float decay = Settings.NoiseGenerator.Generate(worldPos.X * 0.05f, worldPos.Y * 0.05f, worldPos.Z * 0.05f);

                            if (decay > 0.7f)
                                continue;

                            if (!baseVoxel.IsValid)
                                continue;

                            if (baseVoxel.Coordinate.Y == VoxelConstants.ChunkSizeY - 1)
                                continue;

                            if (!underVoxel.IsValid)
                                continue;

                            bool edge = (dx == 0 || dx == structureWidth - 1) || (dz == 0 || dz == structureDepth - 1);

                            if (!edge && !baseVoxel.IsEmpty)
                                continue;

                            if (edge)
                            {
                                baseVoxel.RawSetType(VoxelLibrary.GetVoxelType(biome.RuinWallType));
                            }
                            else
                            {
                                baseVoxel.RawSetType(VoxelLibrary.GetVoxelType(biome.RuinFloorType));
                            }

                            bool[] wallState = new bool[4];
                            wallState[0] = dx == 0;
                            wallState[1] = dx == structureWidth - 1;
                            wallState[2] = dz == 0;
                            wallState[3] = dz == structureDepth - 1;

                            bool[] doorState = new bool[4];
                            doorState[0] = Math.Abs(dz - structureDepth / 2) < 1;
                            doorState[1] = doorState[0];
                            doorState[2] = Math.Abs(dx - structureWidth / 2) < 1;
                            doorState[3] = doorState[2];

                            for (int dy = 1; dy < (baseVoxel.Coordinate.Y - underVoxel.Coordinate.Y); dy++)
                            {
                                var currVoxel = new VoxelHandle(chunks, underVoxel.Coordinate + new GlobalVoxelOffset(0, dy, 0));

                                if (!currVoxel.IsValid)
                                    continue;

                                if (currVoxel.Coordinate.Y == VoxelConstants.ChunkSizeY - 1)
                                    continue;
                                currVoxel.RawSetType(underVoxel.Type);
                            }

                            if (edge)
                            {
                                for (int dy = 1; dy < wallHeight * (1.0f - decay); dy++)
                                {
                                    var currVoxel = new VoxelHandle(chunks, baseVoxel.Coordinate + new GlobalVoxelOffset(0, dy, 0));

                                    if (!currVoxel.IsValid)
                                        continue;

                                    if (currVoxel.Coordinate.Y == VoxelConstants.ChunkSizeY - 1)
                                        continue;

                                    bool door = false;
                                    for (int k = 0; k < 4; k++)
                                    {
                                        if (wallState[k] && doors[k] && doorState[k])
                                        {
                                            door = true;
                                            break;
                                        }
                                    }

                                    if (door && dy < 3)
                                        continue;

                                    currVoxel.RawSetType(VoxelLibrary.GetVoxelType(biome.RuinWallType));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
