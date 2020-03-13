using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp.Voxels
{
    public static partial class GeometryBuilder
    {
        private static Point SelectTile(VoxelType VoxelType, FaceOrientation Orientation)
        {
            switch (Orientation)
            {
                case FaceOrientation.Top:
                    return VoxelType.Top;
                case FaceOrientation.Bottom:
                    return VoxelType.Bottom;
                default:
                    return VoxelType.Sides;
            }
        }
    }
}