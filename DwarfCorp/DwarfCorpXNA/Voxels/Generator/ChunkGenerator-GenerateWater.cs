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
    /// <summary>
    /// Creates randomly generated voxel chunks using data from the overworld.
    /// </summary>
    public partial class ChunkGenerator
    {
        // Todo: Needs to run on entire world, not an individual chunk.
        public void GenerateWater(VoxelChunk chunk, float maxHeight)
        {
            int waterHeight = Math.Min((int)(VoxelConstants.WorldSizeY * NormalizeHeight(Settings.SeaLevel + 1.0f / VoxelConstants.WorldSizeY, maxHeight)), VoxelConstants.WorldSizeY - 1);
            var iceID = VoxelLibrary.GetVoxelType("Ice");
            for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
            {
                for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                {
                    var biome = Overworld.GetBiomeAt(new Vector3(x, 0, z) + chunk.Origin.ToVector3(), chunk.Manager.World.WorldScale, chunk.Manager.World.WorldOrigin);
                    var topVoxelCoordinate = new GlobalVoxelCoordinate(chunk.ID, new LocalVoxelCoordinate(x, 0, z));
                    var topVoxel = VoxelHelpers.FindFirstVoxelBelow(chunk.Manager.CreateVoxelHandle(new GlobalVoxelCoordinate(topVoxelCoordinate.X, VoxelConstants.WorldSizeY - 1, topVoxelCoordinate.Y)));

                    for (var y = (int)(topVoxel.Coordinate.Y - chunk.Origin.Y); (y + chunk.Origin.Y) <= waterHeight && y < VoxelConstants.ChunkSizeY; ++y)
                    {
                        var vox = VoxelHandle.UnsafeCreateLocalHandle(chunk, new LocalVoxelCoordinate(x, y, z));
                        if (vox.IsEmpty && y > topVoxel.Coordinate.Y)
                        {
                            if (biome.WaterSurfaceIce && y == waterHeight)
                                vox.RawSetType(iceID);
                            else
                                vox.QuickSetLiquid(biome.WaterIsLava ? LiquidType.Lava : LiquidType.Water, WaterManager.maxWaterLevel);
                        }
                    }
                }
            }
        }
    }
}
