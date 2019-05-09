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
            if (Math.Abs(ruinsNoise) > GameSettings.Default.GenerationRuinsRate) return;

            int structureWidth = MathFunctions.RandInt(4, 16);
            int structureDepth = MathFunctions.RandInt(4, 16);
            int wallHeight = MathFunctions.RandInt(2, 6);
            int heightOffset = MathFunctions.RandInt(-4, 2);

            var biome = Overworld.GetBiomeAt(Settings.OverworldSettings.Overworld.Map, Chunk.Origin.ToVector3(), Settings.World.WorldScale, Settings.World.WorldOrigin);
            var avgHeight = GetAverageHeight(Chunk.Origin.X, Chunk.Origin.Z, structureWidth, structureDepth, Settings);

            bool[] doors = new bool[4];

            for (int k = 0; k < 4; k++)
                doors[k] = MathFunctions.RandEvent(0.5f);

            for (int dx = 0; dx < structureWidth; dx++)
            {
                for (int dz = 0; dz < structureDepth; dz++)
                {
                    var worldPos = new Vector3(Chunk.Origin.X + dx, avgHeight + heightOffset, Chunk.Origin.Z + dz);

                    var baseVoxel = Settings.World.ChunkManager.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(worldPos));
                    var underVoxel = VoxelHelpers.FindFirstVoxelBelow(Settings.World.ChunkManager.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(worldPos)));
                    var decay = Settings.NoiseGenerator.Generate(worldPos.X * 0.05f, worldPos.Y * 0.05f, worldPos.Z * 0.05f);

                    if (decay > 0.7f) continue;
                    if (!baseVoxel.IsValid) continue;
                    if (baseVoxel.Coordinate.Y == (Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY) - 1)  continue;
                    if (!underVoxel.IsValid) continue;

                    var edge = (dx == 0 || dx == structureWidth - 1) || (dz == 0 || dz == structureDepth - 1);
                    if (!edge && !baseVoxel.IsEmpty) continue;

                    if (edge)
                        baseVoxel.RawSetType(Library.GetVoxelType(biome.RuinWallType));
                    else
                        baseVoxel.RawSetType(Library.GetVoxelType(biome.RuinFloorType));

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

                        currVoxel.RawSetType(underVoxel.Type);
                    }

                    underVoxel.RawSetGrass(0);

                    if (edge)
                    {
                        for (int dy = 1; dy < wallHeight * (1.0f - decay) && dy < (Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY) - 2; dy++)
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

                            currVoxel.RawSetType(Library.GetVoxelType(biome.RuinWallType));
                        }
                    }
                }
            }
        }
    }
}
