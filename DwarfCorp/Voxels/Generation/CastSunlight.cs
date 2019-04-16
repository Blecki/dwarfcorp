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
        public static void CastSunlight(VoxelChunk TopChunk, GeneratorSettings Settings)
        {
            var totalRays = Settings.WorldSizeInChunks.X * Settings.WorldSizeInChunks.Z * VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeZ;
            for (var x = TopChunk.Origin.X; x < TopChunk.Origin.X + VoxelConstants.ChunkSizeX; x++)
                for (var z = TopChunk.Origin.Z; z < TopChunk.Origin.Z + VoxelConstants.ChunkSizeZ; z++)
                    for (var y = (Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY) - 1; y >= 0; y--)
                    {
                        var v = TopChunk.Manager.CreateVoxelHandle(new GlobalVoxelCoordinate(x, y, z));
                        if (!v.IsValid) break;
                        v.Sunlight = true;
                        if (Settings.OverworldSettings.RevealSurface) v.RawSetIsExplored();
                        if (v.Type.ID != 0 && !v.Type.IsTransparent)
                            break;
                    }
        }
    }
}
