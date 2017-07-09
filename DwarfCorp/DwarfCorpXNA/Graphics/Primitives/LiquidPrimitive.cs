using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace DwarfCorp
{
    /// <summary>
    /// Represents a list of liquid surfaces which are to be rendered. Only the top surfaces of liquids
    /// get rendered. Liquids are also "smoothed" in terms of their positions.
    /// </summary>
    public class LiquidPrimitive : GeometricPrimitive
    {
        public LiquidType LiqType { get; set; }
        public bool IsBuilding = false;

        // The primitive everything will be based on.
        private static readonly BoxPrimitive primitive = VoxelLibrary.GetPrimitive("water");

        // Easy successor lookup.
        private static Vector3[] faceDeltas = new Vector3[6];

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

                for (int i = 0; i < 27; i++) neighbors.Add(new VoxelHandle());

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
            InitializeStatics();
        }

        private void InitializeStatics()
        {
            if (StaticsInitialized) return;

            faceDeltas[(int)BoxFace.Back] = new Vector3(0, 0, 1);
            faceDeltas[(int)BoxFace.Front] = new Vector3(0, 0, -1);
            faceDeltas[(int)BoxFace.Left] = new Vector3(-1, 0, 0);
            faceDeltas[(int)BoxFace.Right] = new Vector3(1, 0, 0);
            faceDeltas[(int)BoxFace.Top] = new Vector3(0, 1, 0);
            faceDeltas[(int)BoxFace.Bottom] = new Vector3(0, -1, 0);

            caches = new List<LiquidRebuildCache>();

            StaticsInitialized = true;
        }


        // This will loop through the whole world and draw out all liquid primatives that are handed to the function.
        public static void InitializePrimativesFromChunk(VoxelChunk chunk, List<LiquidPrimitive> primitivesToInit)
        {
            LiquidPrimitive[] lps = new LiquidPrimitive[(int)LiquidType.Count];

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
                    if (lps[i] != null && lps[i].IsBuilding) return;
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

            LiquidType curLiqType = LiquidType.None;
            LiquidPrimitive curPrimative = null;
            ExtendedVertex[] curVertices = null;
            int[] maxVertices = new int[lps.Length];

            VoxelHandle myVoxel = chunk.MakeVoxel(0, 0, 0);
            VoxelHandle vox = chunk.MakeVoxel(0, 0, 0);
            int maxVertex = 0;
            bool fogOfWar = GameSettings.Default.FogofWar;

            for (int y = 0; y < Math.Min(chunk.Manager.ChunkData.MaxViewingLevel + 1, chunk.SizeY); y++)
            {
                for (int x = 0; x < chunk.SizeX; x++)
                {
                    for (int z = 0; z < chunk.SizeZ; z++)
                    {
                        int index = chunk.Data.IndexAt(x, y, z);
                        if (fogOfWar && !chunk.Data.IsExplored[index]) continue;

                        if (chunk.Data.Water[index].WaterLevel > 0)
                        {
                            LiquidType liqType = chunk.Data.Water[index].Type;

                            // We need to see if we changed types and should change the data we are writing to.
                            if (liqType != curLiqType)
                            {
                                LiquidPrimitive newPrimitive = lps[(int)liqType];
                                // We weren't passed a LiquidPrimitive object to work with for this type so we'll skip it.
                                if (newPrimitive == null) continue;

                                maxVertices[(int)curLiqType] = maxVertex;

                                curVertices = newPrimitive.Vertices;
                                curLiqType = liqType;
                                curPrimative = newPrimitive;
                                maxVertex = maxVertices[(int)liqType];
                            }

                            myVoxel.GridPosition = new Vector3(x, y, z);

                            int facesToDraw = 0;
                            for (int i = 0; i < 6; i++)
                            {
                                BoxFace face = (BoxFace)i;
                                // We won't draw the bottom face.  This might be needed down the line if we add transparent tiles like glass.
                                if (face == BoxFace.Bottom) continue;

                                Vector3 delta = faceDeltas[(int)face];

                                // Pull the current neighbor DestinationVoxel based on the face it would be touching.
                                bool success = myVoxel.GetNeighborBySuccessor(delta, ref vox, false);

                                if (success)
                                {
                                    if (face == BoxFace.Top)
                                    {
                                        if (!(vox.WaterLevel == 0 || y == (int)chunk.Manager.ChunkData.MaxViewingLevel))
                                        {
                                            cache.drawFace[(int)face] = false;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        if (vox.WaterLevel != 0 || !vox.IsEmpty)
                                        {
                                            cache.drawFace[(int)face] = false;
                                            continue;
                                        }
                                    }
                                }

                                cache.drawFace[(int)face] = true;
                                facesToDraw++;
                            }

                            // There's no faces to draw on this voxel.  Let's go to the next one.
                            if (facesToDraw == 0) continue;

                            // Now we check to see if we need to resize the current Vertex array.
                            int vertexSizeIncrease = facesToDraw * 6;
                            if (curVertices == null)
                            {
                                curVertices = new ExtendedVertex[256];
                                curPrimative.Vertices = curVertices;
                            }
                            else if (curVertices.Length <= maxVertex + vertexSizeIncrease)
                            {
                                ExtendedVertex[] newVerts = new ExtendedVertex[curVertices.Length * 2];
                                curVertices.CopyTo(newVerts, 0);
                                curVertices = newVerts;
                                curPrimative.Vertices = curVertices;
                            }

                            // Now we have a list of all the faces that will need to be drawn.  Let's draw them.
                            CreateWaterFaces(myVoxel, chunk, x, y, z, curVertices, maxVertex);

                            // Finally increase the size so we can move on.
                            maxVertex += vertexSizeIncrease;
                        }
                    }
                }
            }

            // The last thing we need to do is make sure we set the current primative's maxVertices to the right value.
            maxVertices[(int)curLiqType] = maxVertex;

            // Now actually force the VertexBuffer to be recreated in each primative we worked with.
            for (int i = 0; i < lps.Length; i++)
            {
                LiquidPrimitive updatedPrimative = lps[i];
                if (updatedPrimative == null) continue;

                maxVertex = maxVertices[i];
                if (maxVertex > 0)
                {
                    try
                    {
                        lock (updatedPrimative.VertexLock)
                        {
                            updatedPrimative.MaxVertex = maxVertex;
                            updatedPrimative.VertexBuffer = null;
                        }
                    }
                    catch (System.Threading.AbandonedMutexException e)
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
                            updatedPrimative.Indexes = null;
                            updatedPrimative.MaxVertex = 0;
                            updatedPrimative.MaxIndex = 0;
                        }
                    }
                    catch (System.Threading.AbandonedMutexException e)
                    {
                        Console.Error.WriteLine(e.Message);
                    }
                }
                updatedPrimative.IsBuilding = false;
            }

            cache.inUse = false;
            cache = null;
        }

        private static void CreateWaterFaces(VoxelHandle voxel, VoxelChunk chunk,
                                            int x, int y, int z,
                                            ExtendedVertex[] vertices,
                                            int startVertex)
        {
            // Reset the appropriate parts of the cache.
            cache.Reset();

            // These are reused for every face.
            Vector3 origin = chunk.Origin + new Vector3(x, y, z);
            int index = chunk.Data.IndexAt(x, y, z);
            float centerWaterlevel = chunk.Data.Water[chunk.Data.IndexAt(x, y, z)].WaterLevel;

            for (int faces = 0; faces < cache.drawFace.Length; faces++)
            {
                if (!cache.drawFace[faces]) continue;
                BoxFace face = (BoxFace)faces;

                // Let's get the vertex/index positions for the current face.
                int idx = 0;
                int vertexCount = 0;
                int vertOffset = 0;
                int numVerts = 0;
                primitive.GetFace(face, primitive.UVs, out idx, out vertexCount, out vertOffset, out numVerts);

                for (int i = 0; i < vertexCount; i++)
                {
                    // Used twice so we'll store it for later use.
                    int primitiveIndex = primitive.Indexes[i + idx];
                    VoxelVertex currentVertex = primitive.Deltas[primitiveIndex];

                    // These two will be filled out before being used.
                    float foaminess;
                    Vector3 pos;

                    // We are going to have to reuse some vertices when drawing a single so we'll store the position/foaminess
                    // for quick lookup when we find one of those reused ones.
                    // When drawing multiple faces the Vertex overlap gets bigger, which is a bonus.
                    if (!cache.vertexCalculated[(int)currentVertex])
                    {
                        float count = 1.0f;
                        float emptyNeighbors = 0.0f;
                        float averageWaterLevel = centerWaterlevel;

                        List<Vector3> vertexSucc = VoxelChunk.VertexSuccessors[currentVertex];

                        // Run through the successors and count up the water in each voxel.
                        for (int v = 0; v < vertexSucc.Count; v++)
                        {
                            Vector3 succ = vertexSucc[v];
                            // We are going to use a lookup key so calculate it now.
                            int key = VoxelChunk.SuccessorToEuclidianLookupKey(succ);

                            // If we haven't gotten this DestinationVoxel yet then retrieve it.
                            // This allows us to only get a particular voxel once a function call instead of once per vertexCount/per face.
                            if (!cache.retrievedNeighbors[key])
                            {
                                VoxelHandle neighbor = cache.neighbors[key];
                                cache.validNeighbors[key] = voxel.GetNeighborBySuccessor(succ, ref neighbor, false);
                                cache.retrievedNeighbors[key] = true;
                            }
                            // Only continue if it's a valid (non-null) voxel.
                            if (!cache.validNeighbors[key]) continue;

                            // Now actually do the math.
                            var vox = cache.neighbors[key];
                            count++;
                            if (vox.WaterLevel < 1) emptyNeighbors++;
                        }

                        foaminess = emptyNeighbors / count;

                        if (foaminess <= 0.5f) foaminess = 0.0f;

                        pos = primitive.Vertices[primitiveIndex].Position;
                        pos.Y -= 0.6f;// Minimum ramp position 
                        pos += origin;

                        // Store the vertex information for future use when we need it again on this or another face.
                        cache.vertexCalculated[(int)currentVertex] = true;
                        cache.vertexFoaminess[(int)currentVertex] = foaminess;
                        cache.vertexPositions[(int)currentVertex] = pos;
                    }
                    else
                    {
                        // We've already calculated this one.  Time for a cheap grab from the lookup.
                        foaminess = cache.vertexFoaminess[(int)currentVertex];
                        pos = cache.vertexPositions[(int)currentVertex];
                    }

                    switch (face)
                    {
                        case BoxFace.Back:
                        case BoxFace.Front:
                            vertices[i + startVertex].Set(pos,
                                                          new Color(foaminess * 0.5f, 0.0f, 1.0f, 1.0f),
                                                          Color.White,
                                                          new Vector2(pos.X, pos.Y),
                                                          new Vector4(0, 0, 1, 1));
                            break;
                        case BoxFace.Right:
                        case BoxFace.Left:
                            vertices[i + startVertex].Set(pos,
                                                        new Color(foaminess * 0.5f, 0.0f, 1.0f, 1.0f),
                                                        Color.White,
                                                        new Vector2(pos.Z, pos.Y),
                                                        new Vector4(0, 0, 1, 1));
                            break;
                        case BoxFace.Top:
                            vertices[i + startVertex].Set(pos,
                                                new Color(foaminess, 0.0f, 1.0f, 1.0f),
                                                Color.White,
                                                new Vector2(pos.X, pos.Z),
                                                new Vector4(0, 0, 1, 1));
                            break;
                    }
                }
                startVertex += 6;
            }
            // End cache.drawFace loop
        }
    }

}