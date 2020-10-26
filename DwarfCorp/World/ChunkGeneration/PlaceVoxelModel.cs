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
        public static void PlaceVoxelModel(VoxelChunk Chunk, VoxelModel Model, VoxelType WallType, VoxelType FloorType, int Rotations)
        {
            for (var x = 0; x < Math.Min(VoxelConstants.ChunkSizeX, Model.Dimensions.X); ++x)
                for (var y = 0; y < Math.Min(VoxelConstants.ChunkSizeY, Model.Dimensions.Y); ++y)
                    for (var z = 0; z < Math.Min(VoxelConstants.ChunkSizeZ, Model.Dimensions.Z); ++z)
                    {
                        var modelX = x;
                        var modelZ = z;
                        for (var i = 0; i < Rotations; ++i)
                        {
                            var rX = modelZ;
                            modelZ = Model.Dimensions.X - modelX - 1;
                            modelX = rX;
                        }

                        if (Model.Index(modelX, y, modelZ) != 0)
                        {
                            var v = VoxelHandle.UnsafeCreateLocalHandle(Chunk, new LocalVoxelCoordinate(x, y, z));
                            v.Type = Model.Index(modelX, y, modelZ) == 249 ? WallType : FloorType; // Todo: Shouldn't be hardcoded like this.
                        }
                    }
        }
    }
}
