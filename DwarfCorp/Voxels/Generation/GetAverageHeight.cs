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
        public static float GetAverageHeight(int X, int Z, int Width, int Height, GeneratorSettings Settings)
        {
            var avgHeight = 0;
            var numHeight = 0;

            for (var dx = 0; dx < Width; dx++)
            {
                for (var dz = 0; dz < Height; dz++)
                {
                    var worldPos = new Vector3(dx, (Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY) - 1, dz);
                    var baseVoxel = VoxelHelpers.FindFirstVoxelBelow(Settings.World.ChunkManager.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(worldPos)));

                    if (!baseVoxel.IsValid) continue;

                    avgHeight += baseVoxel.Coordinate.Y + 1;
                    numHeight += 1;
                }
            }

            if (numHeight == 0) return 0;
            return avgHeight / numHeight;
        }
    }
}
