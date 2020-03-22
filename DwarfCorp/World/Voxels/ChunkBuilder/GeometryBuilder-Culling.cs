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
        private static bool IsSideFace(Geo.TemplateFace face)
        {
            return face.Orientation != FaceOrientation.Top && face.Orientation != FaceOrientation.Bottom;
        }

        private static bool IsFaceVisible(VoxelHandle V, Geo.TemplateFace Face, ChunkManager Chunks, out VoxelHandle Neighbor)
        {
            var delta = OrientationHelper.GetFaceNeighborOffset(Face.Orientation);
            Neighbor = new VoxelHandle(Chunks, V.Coordinate + delta);
            return IsFaceVisible(V, Neighbor, Face);
        }

        private static bool IsFaceVisible(VoxelHandle voxel, VoxelHandle neighbor, Geo.TemplateFace face)
        {
            if (!voxel.IsValid)
                return false;

            if (!neighbor.IsValid)
            {
                if (voxel.IsExplored)
                    return !voxel.IsEmpty;
                else
                    return true;
            }
            else
            {
                if (!voxel.IsExplored)
                {
                    if (!neighbor.IsVisible)
                        return true;

                    if (neighbor.IsExplored && neighbor.IsEmpty) return true;

                    if (!neighbor.Type.CanRamp)
                        return false;

                    if (neighbor.RampType == RampType.None)
                        return false;

                    if (!IsSideFace(face))
                        return false;

                    if (!neighbor.IsExplored)
                        return false;

                    return true;
                }
                else
                {
                    if (neighbor.IsExplored && neighbor.IsEmpty)
                        return true;

                    if (face.Orientation == FaceOrientation.Top && !neighbor.IsVisible)
                        return true;

                    if (neighbor.Type.IsTransparent && !voxel.Type.IsTransparent)
                        return true;

                    if (neighbor.Type.CanRamp
                       && neighbor.RampType != RampType.None
                       && IsSideFace(face)
                       && ShouldDrawFace(face.Orientation, neighbor.RampType, voxel.RampType)
                       && neighbor.IsExplored)
                        return true;

                    return false;
                }
            }
        }

    }
}