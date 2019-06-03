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
        public static void CastSunlightColumn(int X, int Z, GeneratorSettings Settings)
        {
            for (var y = (Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY) - 1; y >= 0; y--)
            {
                var v = Settings.World.ChunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(X, y, Z));
                if (!v.IsValid) break;
                v.Sunlight = true;
                v.RawSetIsExplored();
                if (v.Type.ID != 0 && !v.Type.IsTransparent)
                    break;
            }
        }

        public static void CastSunlight(VoxelChunk TopChunk, GeneratorSettings Settings)
        {
            for (var x = TopChunk.Origin.X; x < TopChunk.Origin.X + VoxelConstants.ChunkSizeX; x++)
                for (var z = TopChunk.Origin.Z; z < TopChunk.Origin.Z + VoxelConstants.ChunkSizeZ; z++)
                    CastSunlightColumn(x, z, Settings);
        }
    }
}
