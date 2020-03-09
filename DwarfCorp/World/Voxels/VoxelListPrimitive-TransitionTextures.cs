using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    public partial class VoxelListPrimitive : GeometricPrimitive
    {
        private static int[] TransitionMultipliers = new int[] { 1, 2, 4, 8 }; // This is the value to add to the accumulator when each neighbor is a match.

        private static BoxPrimitive.BoxTextureCoords ComputeTransitionTexture(VoxelHandle V)
        {
            var type = V.Type;

            if (Library.GetVoxelPrimitive(type).HasValue(out BoxPrimitive primitive))
            {
                if (!type.HasTransitionTextures)
                    return primitive.UVs;
                else
                {
                    var transition = ComputeTransitions(V.Chunk.Manager, V, type);
                    return type.TransitionTextures[transition];
                }
            }
            else
                return null;
        }

        private static BoxTransition ComputeTransitions(
            ChunkManager Data,
            VoxelHandle V,
            VoxelType Type)
        {
            if (Type.Transitions == VoxelType.TransitionType.Horizontal)
            {
                var value = ComputeTransitionValueOnPlane(VoxelHelpers.EnumerateManhattanNeighbors2D(V.Coordinate).Select(c => new VoxelHandle(Data, c)), Type);

                return new BoxTransition()
                {
                    Top = (TransitionTexture)value
                };
            }
            else
            {
                var transitionFrontBack = ComputeTransitionValueOnPlane(
                    VoxelHelpers.EnumerateManhattanNeighbors2D_Z(V.Coordinate).Select(c => new VoxelHandle(Data, c)),
                    Type);

                var transitionLeftRight = ComputeTransitionValueOnPlane(
                    VoxelHelpers.EnumerateManhattanNeighbors2D_X(V.Coordinate).Select(c => new VoxelHandle(Data, c)),
                    Type);

                return new BoxTransition()
                {
                    Front = (TransitionTexture)transitionFrontBack,
                    Back = (TransitionTexture)transitionFrontBack,
                    Left = (TransitionTexture)transitionLeftRight,
                    Right = (TransitionTexture)transitionLeftRight
                };
            }
        }

        private static int ComputeTransitionValueOnPlane(IEnumerable<VoxelHandle> Neighbors, VoxelType Type)
        {
            var index = 0;
            var accumulator = 0;

            foreach (var v in Neighbors)
            {
                if (v.IsValid && !v.IsEmpty && v.Type == Type)
                    accumulator += TransitionMultipliers[index];
                index += 1;
            }

            return accumulator;
        }
    }
}