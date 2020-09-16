using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Voxels
{
    public enum FaceOrientation
    {
        Top,
        Bottom,
        North,
        East,
        South,
        West
    }

    public static class OrientationHelper
    {
        public static GlobalVoxelOffset GetFaceNeighborOffset(FaceOrientation Orientation)
        {
            switch (Orientation)
            {
                case FaceOrientation.Top:
                    return new GlobalVoxelOffset(0, 1, 0);
                case FaceOrientation.Bottom:
                    return new GlobalVoxelOffset(0, -1, 0);
                case FaceOrientation.North:
                    return new GlobalVoxelOffset(0, 0, 1);
                case FaceOrientation.East:
                    return new GlobalVoxelOffset(1, 0, 0);
                case FaceOrientation.South:
                    return new GlobalVoxelOffset(0, 0, -1);
                case FaceOrientation.West:
                    return new GlobalVoxelOffset(-1, 0, 0);
                default:
                    return new GlobalVoxelOffset(0, 0, 0);
            }
        }

        public static FaceOrientation GetOppositeFace(FaceOrientation Orientation)
        {
            switch (Orientation)
            {
                case FaceOrientation.Top:
                    return FaceOrientation.Bottom;
                case FaceOrientation.Bottom:
                    return FaceOrientation.Top;
                case FaceOrientation.North:
                    return FaceOrientation.South;
                case FaceOrientation.East:
                    return FaceOrientation.West;
                case FaceOrientation.South:
                    return FaceOrientation.North;
                case FaceOrientation.West:
                    return FaceOrientation.East;
                default:
                    return FaceOrientation.Top;
            }
        }
    }
}