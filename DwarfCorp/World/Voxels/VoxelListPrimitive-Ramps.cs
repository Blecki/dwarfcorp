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
        private static VoxelVertex[] TopVerticies = new VoxelVertex[]
        {
            VoxelVertex.FrontTopLeft,
            VoxelVertex.FrontTopRight,
            VoxelVertex.BackTopLeft,
            VoxelVertex.BackTopRight
        };
        
        private static bool ShouldRamp(VoxelVertex vertex, RampType rampType)
        {
            bool toReturn = false;

            if ((rampType & RampType.TopFrontRight) == RampType.TopFrontRight)
                toReturn = (vertex == VoxelVertex.FrontTopRight);

            if ((rampType & RampType.TopBackRight) == RampType.TopBackRight)
                toReturn = toReturn || (vertex == VoxelVertex.BackTopRight);

            if ((rampType & RampType.TopFrontLeft) == RampType.TopFrontLeft)
                toReturn = toReturn || (vertex == VoxelVertex.FrontTopLeft);

            if ((rampType & RampType.TopBackLeft) == RampType.TopBackLeft)
                toReturn = toReturn || (vertex == VoxelVertex.BackTopLeft);

            return toReturn;
        }

        private static void UpdateVoxelRamps(ChunkManager Chunks, VoxelHandle V)
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
                if (!VoxelHelpers.EnumerateVertexNeighbors2D(V.Coordinate, vertex)
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

        private static void UpdateCornerRamps(ChunkManager Chunks, VoxelChunk Chunk, int LocalY)
        {
            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                    UpdateVoxelRamps(Chunks, VoxelHandle.UnsafeCreateLocalHandle(Chunk, new LocalVoxelCoordinate(x, LocalY, z)));
        }

        private static void UpdateNeighborEdgeRamps(ChunkManager Chunks, VoxelChunk Chunk, int LocalY)
        {
            var startChunkCorner = new GlobalVoxelCoordinate(Chunk.ID, new LocalVoxelCoordinate(0, 0, 0)) + new GlobalVoxelOffset(-1, 0, -1);
            var endChunkCorner = new GlobalVoxelCoordinate(Chunk.ID, new LocalVoxelCoordinate(0, 0, 0)) + new GlobalVoxelOffset(VoxelConstants.ChunkSizeX, 0, VoxelConstants.ChunkSizeZ);

            for (int x = startChunkCorner.X; x <= endChunkCorner.X; ++x)
            {
                var v1 = new VoxelHandle(Chunk.Manager, new GlobalVoxelCoordinate(x, Chunk.Origin.Y + LocalY, startChunkCorner.Z));
                if (v1.IsValid) UpdateVoxelRamps(Chunks, v1);

                var v2 = new VoxelHandle(Chunk.Manager, new GlobalVoxelCoordinate(x, Chunk.Origin.Y + LocalY, endChunkCorner.Z));
                if (v2.IsValid) UpdateVoxelRamps(Chunks, v2);
            }

            for (int z = startChunkCorner.Z + 1; z < endChunkCorner.Z; ++z)
            {
                var v1 = new VoxelHandle(Chunk.Manager, new GlobalVoxelCoordinate(startChunkCorner.X, Chunk.Origin.Y + LocalY, z));
                if (v1.IsValid) UpdateVoxelRamps(Chunks, v1);

                var v2 = new VoxelHandle(Chunk.Manager, new GlobalVoxelCoordinate(endChunkCorner.X, Chunk.Origin.Y + LocalY, z));
                if (v2.IsValid) UpdateVoxelRamps(Chunks, v2);
            }
        }
    }
}