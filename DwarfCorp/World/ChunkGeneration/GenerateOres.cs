using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp.Generation
{
    public static partial class Generator
    {
        public static void GenerateOres(VoxelChunk Chunk, ChunkGeneratorSettings Settings)
        {
            // Prepare randoms for each voxel. Need to have a predictable - but different - seed.
            var noiseOffsets = new List<Vector3>();
            foreach (var voxelType in Library.EnumerateVoxelTypes())
                if (voxelType.SpawnClusters)
                {
                    var vRand = new Random(voxelType.ID);
                    noiseOffsets.Add(new Vector3(vRand.Next(0, 64), vRand.Next(0, 64), vRand.Next(0, 64)));
                }

            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    var overworldPosition = OverworldMap.WorldToOverworld(new Vector2(x + Chunk.Origin.X, z + Chunk.Origin.Z));

                    var normalizedHeight = NormalizeHeight(Settings, Settings.Overworld.Map.LinearInterpolate(overworldPosition, OverworldField.Height));
                    var height = MathFunctions.Clamp(normalizedHeight * Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY, 0.0f, Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY - 2);

                    for (int y = 0; y < VoxelConstants.ChunkSizeY; y++)
                    {
                        if (Chunk.Origin.Y + y >= height) break;
                        if (Chunk.Origin.Y + y == 0) continue;

                        var v = Chunk.Manager.CreateVoxelHandle(new GlobalVoxelCoordinate(Chunk.Origin.X + x, Chunk.Origin.Y + y, Chunk.Origin.Z + z));
                        if (!GameSettings.Current.NoStone && v.Sunlight) continue;

                        int vtypeIndex = 0;

                        foreach (var voxelType in Library.EnumerateVoxelTypes())
                        {
                            if (voxelType.SpawnClusters)
                            {
                                if (Chunk.Origin.Y + y < voxelType.MinSpawnHeight) continue;
                                if (Chunk.Origin.Y + y > voxelType.MaxSpawnHeight) continue;

                                var noiseVector = new Vector3(Chunk.Origin.X + x, Chunk.Origin.Y + y, Chunk.Origin.Z + z) * Settings.CaveNoiseScale * 2.0f;
                                noiseVector += noiseOffsets[vtypeIndex];

                                var fade = 1.0f - ((Chunk.Origin.Y + y - voxelType.MinSpawnHeight) / voxelType.MaxSpawnHeight);

                                var oreNoise = Settings.CaveNoise.GetValue(noiseVector.X, noiseVector.Y, noiseVector.Z);

                                if (Math.Abs(oreNoise) < voxelType.Rarity * fade)
                                    v.RawSetType(voxelType);

                                vtypeIndex += 1;
                            }
                        }
                    }
                }
            }
        }
    }
}
