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
    public partial class ChunkGenerator
    {
        public void GenerateLava(VoxelChunk chunk)
        {
            if (chunk.Origin.Y >= Settings.LavaLevel)
                return;

            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
            {
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    for (var y = 0; y < Settings.LavaLevel - chunk.Origin.Y; ++y)
                    {
                        var voxel = VoxelHandle.UnsafeCreateLocalHandle(chunk, new LocalVoxelCoordinate(x, y, z));
                        if (voxel.IsEmpty && voxel.LiquidLevel == 0)
                            voxel.QuickSetLiquid(LiquidType.Lava, WaterManager.maxWaterLevel);
                    }
                }
            }
        }
    }
}
