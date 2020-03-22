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
        private static VoxelVertex[] TopVerticies = new VoxelVertex[]
        {
            VoxelVertex.FrontTopLeft,
            VoxelVertex.FrontTopRight,
            VoxelVertex.BackTopLeft,
            VoxelVertex.BackTopRight
        };

        public static bool ShouldSlope(VoxelVertex vertex, VoxelHandle Voxel)
        {
            bool toReturn = false;

            if ((Voxel.RampType & RampType.TopFrontRight) == RampType.TopFrontRight)
                toReturn = (vertex == VoxelVertex.FrontTopRight);

            if ((Voxel.RampType & RampType.TopBackRight) == RampType.TopBackRight)
                toReturn = toReturn || (vertex == VoxelVertex.BackTopRight);

            if ((Voxel.RampType & RampType.TopFrontLeft) == RampType.TopFrontLeft)
                toReturn = toReturn || (vertex == VoxelVertex.FrontTopLeft);

            if ((Voxel.RampType & RampType.TopBackLeft) == RampType.TopBackLeft)
                toReturn = toReturn || (vertex == VoxelVertex.BackTopLeft);

            return toReturn;
        }

        private static bool RampSet(RampType ToCheck, RampType For)
        {
            return (ToCheck & For) != 0;
        }

        private static bool CheckRamps(RampType A, RampType A1, RampType A2, RampType B, RampType B1, RampType B2)
        {
            return (!RampSet(A, A1) && RampSet(B, B1)) || (!RampSet(A, A2) && RampSet(B, B2));
        }

        private static bool ShouldDrawFace(FaceOrientation face, RampType neighborRamp, RampType myRamp)
        {
            switch (face)
            {
                case FaceOrientation.Top:
                case FaceOrientation.Bottom:
                    return true;
                case FaceOrientation.South:
                    return CheckRamps(myRamp, RampType.TopBackLeft, RampType.TopBackRight,
                        neighborRamp, RampType.TopFrontLeft, RampType.TopFrontRight);
                case FaceOrientation.North:
                    return CheckRamps(myRamp, RampType.TopFrontLeft, RampType.TopFrontRight,
                        neighborRamp, RampType.TopBackLeft, RampType.TopBackRight);
                case FaceOrientation.West:
                    return CheckRamps(myRamp, RampType.TopBackLeft, RampType.TopFrontLeft,
                        neighborRamp, RampType.TopBackRight, RampType.TopFrontRight);
                case FaceOrientation.East:
                    return CheckRamps(myRamp, RampType.TopBackRight, RampType.TopFrontRight,
                        neighborRamp, RampType.TopBackLeft, RampType.TopFrontLeft);
                default:
                    return false;
            }
        }

        private static void PrecomputeVoxelSlopes(ChunkManager Chunks, VoxelHandle V)
        {
            if (!V.IsValid) return;

            if (V.IsEmpty || !V.IsVisible || V.Type == null || !V.Type.CanRamp)
            {
                V.RampType = RampType.None;
                return;
            }

            var vAbove = VoxelHelpers.GetVoxelAbove(V);
            if (vAbove.IsValid && !vAbove.IsEmpty)
            {
                V.RampType = RampType.None;
                return;
            }

            var compositeRamp = RampType.None;

            foreach (var vertex in TopVerticies)
            {
                // If there are no empty neighbors, no slope.


                // - Check manhattan neighbors - both full? 
                //      then check if they can slope - if yes, and diagonal is empty, slope
                //          if not, then no slope
                //      if diagonal full - obviously no slope.
                if (!VoxelHelpers.EnumerateVertexNeighbors2D(V.Coordinate, vertex) // Todo: Account for case where only open voxel is diagonal between two full ones
                    .Any(n =>
                    {
                        var handle = Chunks.CreateVoxelHandle(n);
                        if (handle.IsValid)
                            return handle.IsEmpty;
                        return false;
                    }))
                    continue;

                switch (vertex)
                {
                    case VoxelVertex.FrontTopLeft:
                        compositeRamp |= RampType.TopFrontLeft;
                        break;
                    case VoxelVertex.FrontTopRight:
                        compositeRamp |= RampType.TopFrontRight;
                        break;
                    case VoxelVertex.BackTopLeft:
                        compositeRamp |= RampType.TopBackLeft;
                        break;
                    case VoxelVertex.BackTopRight:
                        compositeRamp |= RampType.TopBackRight;
                        break;
                }
            }

            V.RampType = compositeRamp;
        }

        private static void PrecomputeVoxelSlopesSlice(ChunkManager Chunks, VoxelChunk Chunk, int LocalY)
        {
            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                    PrecomputeVoxelSlopes(Chunks, VoxelHandle.UnsafeCreateLocalHandle(Chunk, new LocalVoxelCoordinate(x, LocalY, z)));
        
            var startChunkCorner = new GlobalVoxelCoordinate(Chunk.ID, new LocalVoxelCoordinate(0, 0, 0)) + new GlobalVoxelOffset(-1, 0, -1);
            var endChunkCorner = new GlobalVoxelCoordinate(Chunk.ID, new LocalVoxelCoordinate(0, 0, 0)) + new GlobalVoxelOffset(VoxelConstants.ChunkSizeX, 0, VoxelConstants.ChunkSizeZ);

            for (int x = startChunkCorner.X; x <= endChunkCorner.X; ++x)
            {
                var v1 = new VoxelHandle(Chunk.Manager, new GlobalVoxelCoordinate(x, Chunk.Origin.Y + LocalY, startChunkCorner.Z));
                if (v1.IsValid) PrecomputeVoxelSlopes(Chunks, v1);

                var v2 = new VoxelHandle(Chunk.Manager, new GlobalVoxelCoordinate(x, Chunk.Origin.Y + LocalY, endChunkCorner.Z));
                if (v2.IsValid) PrecomputeVoxelSlopes(Chunks, v2);
            }

            for (int z = startChunkCorner.Z + 1; z < endChunkCorner.Z; ++z)
            {
                var v1 = new VoxelHandle(Chunk.Manager, new GlobalVoxelCoordinate(startChunkCorner.X, Chunk.Origin.Y + LocalY, z));
                if (v1.IsValid) PrecomputeVoxelSlopes(Chunks, v1);

                var v2 = new VoxelHandle(Chunk.Manager, new GlobalVoxelCoordinate(endChunkCorner.X, Chunk.Origin.Y + LocalY, z));
                if (v2.IsValid) PrecomputeVoxelSlopes(Chunks, v2);
            }
        }
    }
}