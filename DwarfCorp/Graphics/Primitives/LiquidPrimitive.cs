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
        public LiquidType LiqType { get; set; }
        public bool IsBuilding = false;

        // The primitive everything will be based on.
        private static BoxPrimitive primitive = null;

        // Easy successor lookup.
        private static GlobalVoxelOffset[] faceDeltas = new GlobalVoxelOffset[6];

        // A flag to avoid reinitializing the static values.
        private static bool StaticsInitialized = false;

        private static List<LiquidRebuildCache> caches;

        private class LiquidRebuildCache
        {
            public LiquidRebuildCache()
            {
                int euclidianNeighborCount = 27;
                neighbors = new List<VoxelHandle>(euclidianNeighborCount);
                validNeighbors = new bool[euclidianNeighborCount];
                retrievedNeighbors = new bool[euclidianNeighborCount];

                for (int i = 0; i < 27; i++) neighbors.Add(VoxelHandle.InvalidHandle);

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
            internal List<VoxelHandle> neighbors;

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

        public LiquidPrimitive(LiquidType type) :
            base()
        {
            LiqType = type;
            InitializeLiquidStatics();
        }

        private void InitializeLiquidStatics()
        {
            if (!StaticsInitialized)
            { 

                faceDeltas[(int)BoxFace.Back] = new GlobalVoxelOffset(0, 0, 1);
                faceDeltas[(int)BoxFace.Front] = new GlobalVoxelOffset(0, 0, -1);
                faceDeltas[(int)BoxFace.Left] = new GlobalVoxelOffset(-1, 0, 0);
                faceDeltas[(int)BoxFace.Right] = new GlobalVoxelOffset(1, 0, 0);
                faceDeltas[(int)BoxFace.Top] = new GlobalVoxelOffset(0, 1, 0);
                faceDeltas[(int)BoxFace.Bottom] = new GlobalVoxelOffset(0, -1, 0);

                caches = new List<LiquidRebuildCache>();

                StaticsInitialized = true;
                primitive = new BoxPrimitive(1.0f, 1.0f, 1.0f, new BoxPrimitive.BoxTextureCoords(32, 32, 32, 32, Point.Zero, Point.Zero, Point.Zero, Point.Zero, Point.Zero, Point.Zero));
            }
        }

        public override void Render(GraphicsDevice device)
        {
            lock (base.VertexLock)
            {
                base.Render(device);
            }
        }

        private static bool AddCaches(List<LiquidPrimitive> primitivesToInit, ref LiquidPrimitive[] lps)
        {
            // We are going to first set up the internal array.
            foreach (LiquidPrimitive lp in primitivesToInit)
            {
                if (lp != null) lps[(int)lp.LiqType] = lp;
            }

            // We are going to lock around the IsBuilding check/set to avoid the situation where two threads could both pass through
            // if they both checked IsBuilding at the same time before either of them set IsBuilding.
            lock (caches)
            {
                // We check all parts of the array before setting any to avoid somehow setting a few then leaving before we can unset them.
                for (int i = 0; i < lps.Length; i++)
                {
                    if (lps[i] != null && lps[i].IsBuilding) return false;
                }

                // Now we know we are safe so we can set IsBuilding.
                for (int i = 0; i < lps.Length; i++)
                {
                    if (lps[i] != null) lps[i].IsBuilding = true;
                }

                // Now we have to get a valid cache object.
                bool cacheSet = false;
                for (int i = 0; i < caches.Count; i++)
                {
                    if (!caches[i].inUse)
                    {
                        cache = caches[i];
                        cache.inUse = true;
                        cacheSet = true;
                    }
                }
                if (!cacheSet)
                {
                    cache = new LiquidRebuildCache();
                    cache.inUse = true;
                    caches.Add(cache);
                }
            }

            return true;
        }

        // This will loop through the whole world and draw out all liquid primatives that are handed to the function.
        public static void InitializePrimativesFromChunk(VoxelChunk chunk, List<LiquidPrimitive> primitivesToInit)
        {
            LiquidPrimitive[] lps = new LiquidPrimitive[(int)LiquidType.Count];

            if(!AddCaches(primitivesToInit, ref lps))
                return;

            LiquidType curLiqType = LiquidType.None;
            LiquidPrimitive curPrimitive = null;
            ExtendedVertex[] curVertices = null;
            ushort[] curIndexes = null;
            int[] maxVertices = new int[lps.Length];
            int[] maxIndexes = new int[lps.Length];

            int maxVertex = 0;
            int maxIndex = 0;
            int totalFaces = 6;
            bool fogOfWar = GameSettings.Default.FogofWar;

            for (int globalY = chunk.Origin.Y; globalY < Math.Min(chunk.Manager.World.Renderer.PersistentSettings.MaxViewingLevel + 1, chunk.Origin.Y + VoxelConstants.ChunkSizeY); globalY++)
            {
                var y = globalY - chunk.Origin.Y;
                if (chunk.Data.LiquidPresent[y] == 0) continue;

                for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
                {
                    for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                    {
                        var voxel = VoxelHandle.UnsafeCreateLocalHandle(chunk, new LocalVoxelCoordinate(x, y, z));
                        if (fogOfWar && !voxel.IsExplored) continue;

                        if (voxel.LiquidLevel > 0)
                        {
                            var liqType = voxel.LiquidType;

                            // We need to see if we changed types and should change the data we are writing to.
                            if (liqType != curLiqType)
                            {
                                LiquidPrimitive newPrimitive = lps[(int)liqType];
                                // We weren't passed a LiquidPrimitive object to work with for this type so we'll skip it.
                                if (newPrimitive == null) continue;

                                maxVertices[(int)curLiqType] = maxVertex;
                                maxIndexes[(int)curLiqType] = maxIndex;

                                curVertices = newPrimitive.Vertices;
                                curIndexes = newPrimitive.Indexes;

                                curLiqType = liqType;
                                curPrimitive = newPrimitive;
                                maxVertex = maxVertices[(int)liqType];
                                maxIndex = maxIndexes[(int)liqType];
                            }

                            int facesToDraw = 0;
                            for (int i = 0; i < totalFaces; i++)
                            {
                                BoxFace face = (BoxFace)i;
                                // We won't draw the bottom face.  This might be needed down the line if we add transparent tiles like glass.
                                if (face == BoxFace.Bottom) continue;

                                var delta = faceDeltas[(int)face];

                                // Pull the current neighbor DestinationVoxel based on the face it would be touching.

                                var vox = VoxelHelpers.GetNeighbor(voxel, delta);

                                if (vox.IsValid)
                                {
                                    if (face == BoxFace.Top)
                                    {
                                        if (!(vox.LiquidLevel == 0 || y == (int)chunk.Manager.World.Renderer.PersistentSettings.MaxViewingLevel))
                                        {
                                            cache.drawFace[(int)face] = false;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        if (vox.LiquidLevel != 0 || !vox.IsEmpty)
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
                            int indexSizeIncrease  = facesToDraw * 6;

                            lock (curPrimitive.VertexLock)
                            {
                                // Check vertex array size
                                if (curVertices == null)
                                {

                                    curVertices = new ExtendedVertex[256];
                                    curPrimitive.Vertices = curVertices;
                                }
                                else if (curVertices.Length <= maxVertex + vertexSizeIncrease)
                                {
                                    ExtendedVertex[] newVerts = new ExtendedVertex[MathFunctions.NearestPowerOf2(maxVertex + vertexSizeIncrease)];

                                    curVertices.CopyTo(newVerts, 0);
                                    curVertices = newVerts;
                                    curPrimitive.Vertices = curVertices;
                                }

                                // Check index array size
                                if (curIndexes == null)
                                {
                                    curIndexes = new ushort[256];
                                    curPrimitive.Indexes = curIndexes;
                                }
                                else if (curIndexes.Length <= maxIndex + indexSizeIncrease)
                                {
                                    ushort[] newIdxs = new ushort[MathFunctions.NearestPowerOf2(maxIndex + indexSizeIncrease)];

                                    curIndexes.CopyTo(newIdxs, 0);
                                    curIndexes = newIdxs;
                                    curPrimitive.Indexes = curIndexes;
                                }
                            }

                            // Now we have a list of all the faces that will need to be drawn.  Let's draw  them.
                            CreateWaterFaces(voxel, chunk, x, y, z, curVertices, curIndexes, maxVertex, maxIndex);

                            // Finally increase the size so we can move on.
                            maxVertex += vertexSizeIncrease;
                            maxIndex  += indexSizeIncrease;
                        }
                    }
                }
            }

            // The last thing we need to do is make sure we set the current primative's maxVertices to the right value.
            maxVertices[(int)curLiqType] = maxVertex;
            maxIndexes[(int)curLiqType] = maxIndex;

            // Now actually force the VertexBuffer to be recreated in each primative we worked with.
            for (int i = 0; i < lps.Length; i++)
            {
                LiquidPrimitive updatedPrimative = lps[i];
                if (updatedPrimative == null) continue;

                maxVertex = maxVertices[i];
                maxIndex = maxIndexes[i];

                if (maxVertex > 0)
                {
                    try
                    {
                        lock (updatedPrimative.VertexLock)
                        {
                            updatedPrimative.VertexCount = maxVertex;
                            updatedPrimative.IndexCount = maxIndex;
                            updatedPrimative.VertexBuffer = null;
                            updatedPrimative.IndexBuffer = null;
                        }
                    }
                    catch (global::System.Threading.AbandonedMutexException e)
                    {
                        Console.Error.WriteLine(e.Message);
                    }
                }
                else
                {
                    try
                    {
                        lock (updatedPrimative.VertexLock)
                        {
                            updatedPrimative.VertexBuffer = null;
                            updatedPrimative.Vertices = null;
                            updatedPrimative.IndexBuffer = null;
                            updatedPrimative.Indexes = null;
                            updatedPrimative.VertexCount = 0;
                            updatedPrimative.IndexCount = 0;
                        }
                    }
                    catch (global::System.Threading.AbandonedMutexException e)
                    {
                        Console.Error.WriteLine(e.Message);
                    }
                }
                updatedPrimative.IsBuilding = false;
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
            VoxelHandle voxel, 
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
            float centerWaterlevel = voxel.LiquidLevel;

            var below = VoxelHelpers.GetVoxelBelow(voxel);
            bool belowFilled = false;
            bool belowLiquid = below.IsValid && below.LiquidLevel > 0;
            bool belowRamps = below.IsValid && below.RampType != RampType.None;
            if ((below.IsValid && !below.IsEmpty) || belowLiquid)
            {
                belowFilled = true;
            }

            float[] foaminess = new float[4];

            for (int i = 0; i < cache.drawFace.Length; i++)
            {
                if (!cache.drawFace[i]) continue;
                BoxFace face = (BoxFace)i;

                var faceDescriptor = primitive.GetFace(face);
                int indexOffset = startVertex;

                for (int vertOffset = 0; vertOffset < faceDescriptor.VertexCount; vertOffset++)
                {
                    VoxelVertex currentVertex = primitive.Deltas[faceDescriptor.VertexOffset + vertOffset];

                    // These will be filled out before being used   lh  .
                    //float foaminess1;
                    foaminess[vertOffset] = 0.0f;
                    bool shoreLine = false;

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
                        float averageWaterLevel = centerWaterlevel;

                        var vertexSucc = VoxelHelpers.VertexNeighbors[(int)currentVertex];

                        // Run through the successors and count up the water in each voxel.
                        for (int v = 0; v < vertexSucc.Length; v++)
                        {
                            var neighborVoxel = new VoxelHandle(chunk.Manager, voxel.Coordinate + vertexSucc[v]);
                            if (!neighborVoxel.IsValid) continue;

                            // Now actually do the math.
                            count++;
                            if (neighborVoxel.LiquidLevel < 1) emptyNeighbors++;
                            if (neighborVoxel.LiquidType == LiquidType.None && !neighborVoxel.IsEmpty) shoreLine = true;
                        }

                        foaminess[vertOffset] = emptyNeighbors / count;

                        if (foaminess[vertOffset] <= 0.5f)
                        {
                            foaminess[vertOffset] = 0.0f;
                        }
                        // Check if it should ramp.
                        else if (!shoreLine)
                        {
                            //rampOffset.Y = -0.4f;
                        }

                        pos = primitive.Vertices[vertOffset + faceDescriptor.VertexOffset].Position;
                        if ((currentVertex & VoxelVertex.Top) == VoxelVertex.Top)
                        {
                            if (belowFilled)
                                pos.Y -= 0.6f;// Minimum ramp position 

                            var neighbors = VoxelHelpers.EnumerateVertexNeighbors2D(voxel.Coordinate, currentVertex)
                                .Select(c => new VoxelHandle(chunk.Manager, c))
                                .Where(h => h.IsValid)
                                .Select(h => MathFunctions.Clamp((float)h.LiquidLevel / 8.0f, 0.25f, 1.0f));

                            if (neighbors.Count() > 0)
                            {
                                if (belowFilled)
                                    pos.Y *= neighbors.Average();
                            }
                        }
                        else
                        {
                            uv.Y -= 0.6f;
                        }

                        pos += VertexNoise.GetNoiseVectorFromRepeatingTexture(voxel.WorldPosition +
                            primitive.Vertices[vertOffset + faceDescriptor.VertexOffset].Position);

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

                    vertices[startVertex].Set(pos,
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
            // End cache.drawFace loop
        }
    }

}