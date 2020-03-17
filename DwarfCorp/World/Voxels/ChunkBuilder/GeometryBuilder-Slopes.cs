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
        public static bool ShouldSlope(VoxelVertex vertex, RampType rampType) // Todo: Rename ramp type when geo gen is replaced.
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

        public static bool ShouldVoxelVertexSlope(ChunkManager Chunks, VoxelHandle V, VoxelVertex Vertex, SliceCache Cache)
        {
            if (!Cache.ShouldSlope.ContainsKey(Vertex))
                Cache.ShouldSlope[Vertex] = ShouldVoxelVertexSlope(Chunks, V, Vertex);
            return Cache.ShouldSlope[Vertex];
        }

        private static bool ShouldVoxelVertexSlope(ChunkManager Chunks, VoxelHandle V, VoxelVertex Vertex)
        {
            if (!V.IsValid) return false;
            if (!V.IsExplored) return false;

            if (V.IsEmpty || !V.IsVisible || V.Type == null || !V.Type.CanRamp)
                return false;

            var vAbove = VoxelHelpers.GetVoxelAbove(V);
            if (vAbove.IsValid && !vAbove.IsEmpty)
                return false;

            if (VoxelHelpers.EnumerateVertexNeighbors2D(V.Coordinate, Vertex).Any(n => 
            {
                var handle = Chunks.CreateVoxelHandle(n);
                if (!handle.IsValid) return true;
                if (!handle.IsEmpty && !handle.Type.CanRamp) return true;
                if (!handle.IsExplored) return true;
                var above = VoxelHelpers.GetVoxelAbove(handle);
                if (above.IsValid && !above.IsEmpty) return true;
                return false;
                    
            }))
                return false;

            if (VoxelHelpers.EnumerateVertexNeighbors2D(V.Coordinate, Vertex).Any(n =>
            {
                var handle = Chunks.CreateVoxelHandle(n);
                if (handle.IsValid) return handle.IsEmpty;
                return false;
            }))
                return true;

            return false;
        }
    }
}