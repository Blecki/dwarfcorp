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
        public static void GenerateLava(VoxelChunk chunk, ChunkGeneratorSettings Settings)
        {
            if (chunk.Origin.Y >= Settings.LavaLevel)
                return;

            var lava = Library.GetLiquid("Lava");
            if (!lava.HasValue(out var _lava))
                return;

            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
            {
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    for (var y = 0; y < Settings.LavaLevel - chunk.Origin.Y; ++y)
                    {
                        var voxel = VoxelHandle.UnsafeCreateLocalHandle(chunk, new LocalVoxelCoordinate(x, y, z));
                        if (voxel.IsEmpty && voxel.LiquidLevel == 0)
                            voxel.QuickSetLiquid(_lava.ID, WaterManager.maxWaterLevel);
                    }
                }
            }
        }
    }
}
