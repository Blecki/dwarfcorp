using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace DwarfCorp
{
    /// <summary>
    /// Represents a list of liquid surfaces which are to be rendered. Only the top surfaces of liquids
    /// get rendered. Liquids are also "smoothed" in terms of their positions.
    /// </summary>
    public class LiquidPrimitive : VoxelListPrimitive // GeometricPrimitive
    {
        public byte LiqType { get; set; }
        public bool IsBuilding = false;

        // The primitive everything will be based on.
        private static BoxPrimitive primitive = null;

        // Easy successor lookup.
        private static GlobalLiquidOffset[] faceDeltas = new GlobalLiquidOffset[6];

        // A flag to avoid reinitializing the static values.
        private static bool StaticsInitialized = false;

        private static List<LiquidRebuildCache> caches;

        private class LiquidRebuildCache
        {
            public LiquidRebuildCache()
            {
                int euclidianNeighborCount = 27;
                neighbors = new List<LiquidCellHandle>(euclidianNeighborCount);
                validNeighbors = new bool[euclidianNeighborCount];
                retrievedNeighbors = new bool[euclidianNeighborCount];

                for (int i = 0; i < 27; i++) neighbors.Add(LiquidCellHandle.InvalidHandle);

                int vertexCount = (int)VoxelVertex.Count;
                vertexCalculated = new bool[vertexCount];
                vertexFoaminess = new float[vertexCount];
                vertexPositions = new Vector3[vertexCount];
            }

            public void Reset()
            {
                // Clear the retrieved list for this run.
                for (int i = 0; i < retrievedNeighbors.Length; i++)
                    retrievedNeighbors[i] = false;

                for (int i = 0; i < vertexCalculated.Length; i++)
                    vertexCalculated[i] = false;
            }

            // Lookup to see which faces we are going to draw.
            internal bool[] drawFace = new bool[6];

            // A list of unattached voxels we can change to the neighbors of the voxel who's faces we are drawing.
            internal List<LiquidCellHandle> neighbors;

            // A list of which voxels are valid in the neighbors list.  We can't just set a neighbor to null as we reuse them so we use this.
            // Does not need to be cleared between sets of face drawing as retrievedNeighbors stops us from using a stale value.
            internal bool[] validNeighbors;

            // A list of which neighbors in the neighbors list have been filled in with the current DestinationVoxel's neighbors.
            // This does need to be cleared between drawing the faces on a DestinationVoxel.
            internal bool[] retrievedNeighbors;

            // Stored positions for the current DestinationVoxel's vertexes.  Lets us reuse the stored value when another face uses the same position.
            internal Vector3[] vertexPositions;

            // Stored foaminess value for the current DestinationVoxel's vertexes.
            internal float[] vertexFoaminess;

            // A flag to show if a particular vertex has already been calculated.  Must be cleared when drawing faces on a new DestinationVoxel position.
            internal bool[] vertexCalculated;

            // A flag to show if the cache is in use at that moment.
            internal bool inUse;
        }

        [ThreadStatic]
        private static LiquidRebuildCache cache;

        public LiquidPrimitive(byte type) :
            base()
        {
            LiqType = type;
            InitializeLiquidStatics();
        }

        private void InitializeLiquidStatics()
        {
            if (!StaticsInitialized)
            { 

                faceDeltas[(int)BoxFace.Back] = new GlobalLiquidOffset(0, 0, 1);
                faceDeltas[(int)BoxFace.Front] = new GlobalLiquidOffset(0, 0, -1);
                faceDeltas[(int)BoxFace.Left] = new GlobalLiquidOffset(-1, 0, 0);
                faceDeltas[(int)BoxFace.Right] = new GlobalLiquidOffset(1, 0, 0);
                faceDeltas[(int)BoxFace.Top] = new GlobalLiquidOffset(0, 1, 0);
                faceDeltas[(int)BoxFace.Bottom] = new GlobalLiquidOffset(0, -1, 0);

                caches = new List<LiquidRebuildCache>();

                StaticsInitialized = true;
                primitive = new BoxPrimitive(new BoxPrimitive.BoxTextureCoords(32, 32, 32, 32, Point.Zero, Point.Zero, Point.Zero, Point.Zero, Point.Zero, Point.Zero));
            }
        }

        public override void Render(GraphicsDevice device)
        {
            lock (base.VertexLock)
            {
                base.Render(device);
            }
        }

        private static LiquidRebuildCache GetCache()
        {
            // We are going to lock around the IsBuilding check/set to avoid the situation where two threads could both pass through
            // if they both checked IsBuilding at the same time before either of them set IsBuilding.
            lock (caches)
            {
                // Now we have to get a valid cache object.
                for (int i = 0; i < caches.Count; i++)
                {
                    if (!caches[i].inUse)
                    {
                        caches[i].inUse = true;
                        return caches[i];
                    }
                }

                var cache = new LiquidRebuildCache();
                cache.inUse = true;
                caches.Add(cache);
                return cache;
            }
        }

        // This will loop through the whole world and draw out all liquid primatives that are handed to the function.
        public static void InitializePrimativesFromChunk(VoxelChunk chunk, List<LiquidPrimitive> primitivesToInit)
        {
            cache = GetCache();

            foreach (var primitive in primitivesToInit)
            {
                lock (primitive.VertexLock)
                {
                    if (primitive.IsBuilding) continue;
                    primitive.IsBuilding = true;
                }

                // Need to build in temp buffers and copy over to primitive.
                int vertexCount = 0;
                int indexCount = 0;
                var vertices = new ExtendedVertex[256];
                var indexes = new ushort[256];

                
                primitive.VertexCount = 0;
                primitive.IndexCount = 0;

                int totalFaces = 6;

                var chunkLiquidOrigin = new GlobalLiquidCoordinate(chunk.ID, new LocalLiquidCoordinate(0, 0, 0));

                for (int globalY = chunkLiquidOrigin.Y; globalY < Math.Min((chunk.Manager.World.Renderer.PersistentSettings.MaxViewingLevel * 2) + 1, chunkLiquidOrigin.Y + VoxelConstants.LiquidChunkSizeY); globalY++)
                {
                    var y = globalY - chunkLiquidOrigin.Y;
                    if (chunk.Data.LiquidPresent[y] == 0) continue;

                    for (int x = 0; x < VoxelConstants.LiquidChunkSizeX; x++)
                    {
                        for (int z = 0; z < VoxelConstants.LiquidChunkSizeZ; z++)
                        {
                            var cell = LiquidCellHandle.UnsafeCreateLocalHandle(chunk, new LocalLiquidCoordinate(x, y, z));
                            var _voxel = chunk.Manager.CreateVoxelHandle(cell.Coordinate.ToGlobalVoxelCoordinate());
                            if (GameSettings.Current.FogofWar && !_voxel.IsExplored) continue;

                            if (cell.LiquidType != primitive.LiqType)
                                continue;

                            int facesToDraw = 0;
                            for (int i = 0; i < totalFaces; i++)
                            {
                                BoxFace face = (BoxFace)i;
                                // We won't draw the bottom face.  This might be needed down the line if we add transparent tiles like glass.
                                if (face == BoxFace.Bottom) continue;

                                var delta = faceDeltas[(int)face];

                                // Pull the current neighbor DestinationVoxel based on the face it would be touching.

                                var vox = LiquidCellHelpers.GetNeighbor(cell, delta);

                                if (vox.IsValid)
                                {
                                    if (face == BoxFace.Top)
                                    {
                                        if (!(vox.LiquidType == 0 || y == (int)chunk.Manager.World.Renderer.PersistentSettings.MaxViewingLevel))
                                        {
                                            cache.drawFace[(int)face] = false;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        var _vox = chunk.Manager.CreateVoxelHandle(vox.Coordinate.ToGlobalVoxelCoordinate());
                                        if (vox.LiquidType != 0)// || !_vox.IsEmpty)
                                        {
                                            cache.drawFace[(int)face] = false;
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    cache.drawFace[(int)face] = false;
                                    continue;
                                }

                                cache.drawFace[(int)face] = true;
                                facesToDraw++;
                            }

                            // There's no faces to draw on this voxel.  Let's go to the next one.
                            if (facesToDraw == 0) continue;

                            // Now we check to see if we need to resize the current Vertex array.
                            int vertexSizeIncrease = facesToDraw * 4;
                            int indexSizeIncrease = facesToDraw * 6;

                                // Check vertex array size
                                if (vertices.Length <= vertexCount + vertexSizeIncrease)
                                {
                                    var newVerts = new ExtendedVertex[MathFunctions.NearestPowerOf2(vertexCount + vertexSizeIncrease)];

                                    vertices.CopyTo(newVerts, 0);
                                    vertices = newVerts;
                                }

                                // Check index array size
                                if (indexes == null)
                                    indexes = new ushort[256];
                                else if (indexes.Length <= indexCount + indexSizeIncrease)
                                {
                                    var newIdxs = new ushort[MathFunctions.NearestPowerOf2(indexCount + indexSizeIncrease)];

                                    indexes.CopyTo(newIdxs, 0);
                                    indexes = newIdxs;
                                }

                            // Now we have a list of all the faces that will need to be drawn.  Let's draw  them.
                            CreateWaterFaces(cell, chunk, x, y, z, vertices, indexes, vertexCount, indexCount);

                            // Finally increase the size so we can move on.
                            vertexCount += vertexSizeIncrease;
                            indexCount += indexSizeIncrease;
                        }
                    }
                }

                try
                {
                    lock (primitive.VertexLock)
                    {
                        if (primitive.VertexBuffer != null)
                            primitive.VertexBuffer.Dispose();
                        primitive.VertexBuffer = null;
                        if (primitive.IndexBuffer != null)
                            primitive.IndexBuffer.Dispose();
                        primitive.IndexBuffer = null;

                        primitive.Vertices = vertices;
                        primitive.VertexCount = vertexCount;
                        primitive.Indexes = indexes;
                        primitive.IndexCount = indexCount;
                    }
                }
                catch (global::System.Threading.AbandonedMutexException e)
                {
                    Console.Error.WriteLine(e.Message);
                }

                primitive.IsBuilding = false;
            }

            cache.inUse = false;
            cache = null;
        }

        private static int SuccessorToEuclidianLookupKey(GlobalVoxelOffset C)
        {
            return (C.X + 1) + (C.Y + 1) * 3 + (C.Z + 1) * 9;
        }

        private static bool IsBottom(VoxelVertex vertex)
        {
            switch (vertex)
            {
                case VoxelVertex.BackBottomLeft:
                    return true;
                case VoxelVertex.BackBottomRight:
                    return true;
                case VoxelVertex.FrontBottomLeft:
                    return true;
                case VoxelVertex.FrontBottomRight:
                    return true;
                default:
                    return false;
            }
            
        }

        private static void CreateWaterFaces(
            LiquidCellHandle voxel, 
            VoxelChunk chunk,
            int x, int y, int z,
                                            ExtendedVertex[] vertices,
                                            ushort[] Indexes,
                                            int startVertex,
                                            int startIndex)
        {
            // Reset the appropriate parts of the cache.
            cache.Reset();

            // These are reused for every face.
            var origin = voxel.WorldPosition;

            var below = LiquidCellHelpers.GetLiquidCellBelow(voxel);
            var _below = chunk.Manager.CreateVoxelHandle(below.Coordinate.ToGlobalVoxelCoordinate());
            bool belowLiquid = below.IsValid && below.LiquidType != 0;

            float[] foaminess = new float[4];

            for (int i = 0; i < cache.drawFace.Length; i++)
            {
                if (!cache.drawFace[i]) continue;
                BoxFace face = (BoxFace)i;

                var faceDescriptor = primitive.GetFace(face);
                int indexOffset = startVertex;

                for (int vertOffset = 0; vertOffset < faceDescriptor.VertexCount; vertOffset++)
                {
                    VoxelVertex currentVertex = primitive.VertexClassifications[faceDescriptor.VertexOffset + vertOffset];

                    // These will be filled out before being used   lh  .
                    //float foaminess1;
                    foaminess[vertOffset] = 0.0f;

                    Vector3 pos = Vector3.Zero;
                    Vector3 rampOffset = Vector3.Zero;
                    var uv = primitive.UVs.Uvs[vertOffset + faceDescriptor.VertexOffset];
                    // We are going to have to reuse some vertices when drawing a single so we'll store the position/foaminess
                    // for quick lookup when we find one of those reused ones.
                    // When drawing multiple faces the Vertex overlap gets bigger, which is a bonus.
                    if (!cache.vertexCalculated[(int)currentVertex])
                    {
                        float count = 1.0f;
                        float emptyNeighbors = 0.0f;

                        var vertexSucc = LiquidCellHelpers.VertexNeighbors[(int)currentVertex];

                        // Run through the successors and count up the water in each voxel.
                        for (int v = 0; v < vertexSucc.Length; v++)
                        {
                            var neighborVoxel = new LiquidCellHandle(chunk.Manager, voxel.Coordinate + vertexSucc[v]);
                            if (!neighborVoxel.IsValid) continue;

                            // Now actually do the math.
                            count++;
                            if (neighborVoxel.LiquidType == 0) emptyNeighbors++;
                        }

                        foaminess[vertOffset] = emptyNeighbors / count;

                        if (foaminess[vertOffset] <= 0.5f)
                            foaminess[vertOffset] = 0.0f;

                        pos = primitive.Vertices[vertOffset + faceDescriptor.VertexOffset].Position;
                        /*
                        if ((currentVertex & VoxelVertex.Top) == VoxelVertex.Top)
                        {
                            //if (belowFilled)
                            //    pos.Y -= 0.6f;// Minimum ramp position 

                             //var neighbors = LiquidCellHelpers.EnumerateVertexNeighbors2D(voxel.Coordinate, currentVertex)
                             //    .Select(c => new LiquidCellHandle(chunk.Manager, c))
                             //    .Where(h => h.IsValid)
                             //    .Select(h => MathFunctions.Clamp((float)h.LiquidLevel / (float)WaterManager.maxWaterLevel, 0.1f, 1.0f));

                             //if (neighbors.Count() > 0)
                             //{
                             //    if (belowFilled)
                             //        pos.Y *= neighbors.Average();
                             //}
                            //pos.Y *= (float)voxel.LiquidLevel / (float)WaterManager.maxWaterLevel;
                        }
                        else
                        {
                            uv.Y -= 0.6f;
                        }*/

                        pos += VertexNoise.GetNoiseVectorFromRepeatingTexture((voxel.WorldPosition +
                            primitive.Vertices[vertOffset + faceDescriptor.VertexOffset].Position) / 2.0f);

                        /*
                        if (!belowFilled)
                        {
                            pos = (pos - Vector3.One * 0.5f);
                            pos.Normalize();
                            pos *= 0.35f;
                            pos += Vector3.One * 0.5f;
                        }
                        else if ((belowLiquid || belowRamps) && IsBottom(currentVertex))
                        {
                            if  (belowRamps)
                            {
                                pos -= Vector3.Up * 0.5f;
                            }
                            else
                            {
                                pos -= Vector3.Up * 0.8f;
                            }
                        }
                        */

                        pos += origin + rampOffset;
                        // Store the vertex information for future use when we need it again on this or another face.
                        cache.vertexCalculated[(int)currentVertex] = true;
                        cache.vertexFoaminess[(int)currentVertex] = foaminess[vertOffset];
                        cache.vertexPositions[(int)currentVertex] = pos;
                    }
                    else
                    {
                        // We've already calculated this one.  Time for a cheap grab from the lookup.
                        foaminess[vertOffset] = cache.vertexFoaminess[(int)currentVertex];
                        pos = cache.vertexPositions[(int)currentVertex];
                    }

                    vertices[startVertex].Set(pos * 0.5f,
                        new Color(foaminess[vertOffset], 0.0f, 1.0f, 1.0f),
                        Color.White,
                        uv,
                        new Vector4(0, 0, 1, 1));

                    startVertex++;
                }

                bool flippedQuad = foaminess[1] + foaminess[3] > 
                                   foaminess[0] + foaminess[2];

                for (int idx = faceDescriptor.IndexOffset; idx < faceDescriptor.IndexCount + faceDescriptor.IndexOffset; idx++)
                {
                    ushort offset = flippedQuad ? primitive.FlippedIndexes[idx] : primitive.Indexes[idx];
                    ushort offset0 = flippedQuad ? primitive.FlippedIndexes[faceDescriptor.IndexOffset] : primitive.Indexes[faceDescriptor.IndexOffset];

                    Indexes[startIndex] = (ushort)(indexOffset + offset - offset0);
                    startIndex++;
                }
            }
        }
    }

}