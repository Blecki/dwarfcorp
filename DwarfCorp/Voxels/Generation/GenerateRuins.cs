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
        public static void GenerateRuin(VoxelChunk Chunk, GeneratorSettings Settings)
        {
            var noiseVector = Chunk.Origin.ToVector3() * Settings.CaveNoiseScale;
            var ruinsNoise = Settings.CaveNoise.GetValue(noiseVector.X, noiseVector.Y, noiseVector.Z);
            // Todo: Don't actually generate ruins on every chunk, holy S.

            int structureWidth = MathFunctions.RandInt(4, 16);
            int structureDepth = MathFunctions.RandInt(4, 16);
            int wallHeight = MathFunctions.RandInt(2, 6);
            int heightOffset = MathFunctions.RandInt(-4, 2);
            var origin = Chunk.Origin;

            BiomeData biome = BiomeLibrary.Biomes[0];

            // Todo: Lift; use for balloon port too.
            int avgHeight = 0;
            int numHeight = 0;
            for (int dx = 0; dx < structureWidth; dx++)
            {
                for (int dz = 0; dz < structureDepth; dz++)
                {
                    var worldPos = new Vector3(origin.X + dx, (Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY) - 1, origin.Z + dz);

                    var baseVoxel = VoxelHelpers.FindFirstVoxelBelow(Settings.World.ChunkManager.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(worldPos)));

                    if (!baseVoxel.IsValid) continue;

                    biome = Overworld.GetBiomeAt(worldPos, Settings.World.WorldScale, Settings.World.WorldOrigin);

                    var h = baseVoxel.Coordinate.Y + 1;
                    avgHeight += h;
                    numHeight++;
                }
            }

            if (numHeight == 0)
                return;

            avgHeight = avgHeight / numHeight;

            bool[] doors = new bool[4];

            for (int k = 0; k < 4; k++)
                doors[k] = MathFunctions.RandEvent(0.5f);

            for (int dx = 0; dx < structureWidth; dx++)
            {
                for (int dz = 0; dz < structureDepth; dz++)
                {
                    var worldPos = new Vector3(origin.X + dx, avgHeight + heightOffset, origin.Z + dz);

                    var baseVoxel = Settings.World.ChunkManager.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(worldPos));
                    var underVoxel = VoxelHelpers.FindFirstVoxelBelow(Settings.World.ChunkManager.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(worldPos)));
                    var decay = Settings.NoiseGenerator.Generate(worldPos.X * 0.05f, worldPos.Y * 0.05f, worldPos.Z * 0.05f);

                    if (decay > 0.7f)
                        continue;

                    if (!baseVoxel.IsValid)
                        continue;

                    if (baseVoxel.Coordinate.Y == (Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY) - 1)
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
                        var currVoxel = Settings.World.ChunkManager.CreateVoxelHandle(underVoxel.Coordinate + new GlobalVoxelOffset(0, dy, 0));

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
                            var currVoxel = Settings.World.ChunkManager.CreateVoxelHandle(baseVoxel.Coordinate + new GlobalVoxelOffset(0, dy, 0));

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
