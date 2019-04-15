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
        public static void CastSunlight(ChunkManager ChunkManager, GeneratorSettings Settings)
        {
            var totalRays = Settings.WorldSizeInChunks.X * Settings.WorldSizeInChunks.Z * VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeZ;
            Settings.SetLoadingMessage(String.Format("{0} rays of sunshine to propogate.", totalRays));
            Settings.SetLoadingMessage("");
            for (var x = 0; x < Settings.WorldSizeInChunks.X * VoxelConstants.ChunkSizeX; x++)
            {
                for (var z = 0; z < Settings.WorldSizeInChunks.Z * VoxelConstants.ChunkSizeZ; z++)
                    for (var y = (Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY) - 1; y >= 0; y--)
                    {
                        var v = ChunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(x, y, z));
                        if (!v.IsValid) break;
                        v.Sunlight = true;
                        if (Settings.OverworldSettings.RevealSurface) v.RawSetIsExplored();
                        if (v.Type.ID != 0 && !v.Type.IsTransparent)
                            break;
                    }
                Settings.SetLoadingMessage(String.Format("#{0} of {1} rays...", (x + 1) * Settings.WorldSizeInChunks.Z * VoxelConstants.ChunkSizeZ, totalRays));
            }
        }
    }
}
