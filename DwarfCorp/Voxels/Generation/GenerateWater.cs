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
        public static void GenerateWater(VoxelChunk chunk, float waterHeight)
        {
            var iceID = VoxelLibrary.GetVoxelType("Ice");

            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
            {
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    var biome = Overworld.GetBiomeAt(new Vector3(x, 0, z) + chunk.Origin.ToVector3(), chunk.Manager.World.WorldScale, chunk.Manager.World.WorldOrigin);

                    for (var y = 0; y < VoxelConstants.ChunkSizeY; ++y)
                    {
                        var globalY = y + chunk.Origin.Y;
                        if (globalY > waterHeight)
                            break;

                        var voxel = VoxelHandle.UnsafeCreateLocalHandle(chunk, new LocalVoxelCoordinate(x, y, z));
                        if (voxel.IsEmpty && voxel.Sunlight)
                        {
                            if (globalY == waterHeight && biome.WaterSurfaceIce)
                                voxel.RawSetType(iceID);
                            else
                                voxel.QuickSetLiquid(biome.WaterIsLava ? LiquidType.Lava : LiquidType.Water, WaterManager.maxWaterLevel);
                        }
                    }
                }
            }
        }
    }
}
