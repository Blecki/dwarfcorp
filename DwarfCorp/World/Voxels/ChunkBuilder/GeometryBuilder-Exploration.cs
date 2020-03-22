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
        private static int GetVoxelVertexExploredNeighbors(VoxelHandle V, Geo.TemplateFace Face, SliceCache Cache)
        {
            var exploredVerts = 0;

            if (V.IsExplored)
                exploredVerts = 4;
            else
            {
                for (int faceVertex = 0; faceVertex < Face.Mesh.VertexCount; ++faceVertex)
                {
                    var cacheKey = SliceCache.GetCacheKey(V, Face.Mesh.Verticies[faceVertex].LogicalVertex);
                    var anyNeighborExplored = true;

                    if (!Cache.ExploredCache.TryGetValue(cacheKey, out anyNeighborExplored))
                    {
                        anyNeighborExplored = VoxelHelpers.EnumerateVertexNeighbors2D(V.Coordinate, Face.Mesh.Verticies[faceVertex].LogicalVertex)
                            .Select(c => new VoxelHandle(V.Chunk.Manager, c))
                            .Any(n => n.IsValid && n.IsExplored);
                        Cache.ExploredCache.Add(cacheKey, anyNeighborExplored);
                    }

                    if (anyNeighborExplored)
                        exploredVerts += 1;
                }

            }

            return exploredVerts;
        }

    }
}