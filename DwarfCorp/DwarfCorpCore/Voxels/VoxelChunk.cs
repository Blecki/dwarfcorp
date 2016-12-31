// VoxelChunk.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     A 3D grid of voxels, water, and light.
    /// </summary>
    public class VoxelChunk : IBoundedObject
    {
        /// <summary> Called whenever a voxel at the given coordinate is destroyed </summary>
        public delegate void VoxelDestroyed(Point3 voxelID);

        /// <summary> Called whenever a voxel at the given coordinate is revealed (fog of war cleared) </summary>
        public delegate void VoxelExplored(Point3 voxelID);
        
        /// <summary> ambient lighting for voxels that are hidden by fog of war </summary>
        public static byte m_fogOfWar = 1;
        /// <summary> if true, the static data for all VoxelChunks has been initialized </summary>
        private static bool staticsInitialized;
        /// <summary> cache of the offsets from the leastmost corner of the voxel to the origin of each of its 8 vertices </summary>
        private static readonly Vector3[] vertexDeltas = new Vector3[8];
        /// <summary> cache of the offsets from the leastmost corner of the voxel to the center of each of its 6 faces </summary>
        private static readonly Vector3[] faceDeltas = new Vector3[6];

        /// <summary> 
        /// A dictionary caching a map from each of the voxel's 8 vertices to a list of neighbors
        /// which touch that vertex. This is useful for determining which vertices are visible
        /// from which voxels (a necessary computation for lighting).
        /// Maps VoxelVertex to normalized directions corresponding to the neighbors.
        /// </summary>
        public static readonly Dictionary<VoxelVertex, List<Vector3>> VertexSuccessors =
            new Dictionary<VoxelVertex, List<Vector3>>();

        /// <summary>
        /// A dictionary caching a map from each of the voxel's 8 vertices to a list of neighbors
        /// adjacent to that vertex, and which have the same Y value as the voxel in question.
        /// This is useful for determining which vertices are visible
        /// from which voxels (a necessary computation for lighting).
        /// Maps VoxelVertex to normalized directions corresponding to the neighbors.
        /// </summary>
        public static readonly Dictionary<VoxelVertex, List<Vector3>> VertexSuccessorsDiag =
            new Dictionary<VoxelVertex, List<Vector3>>();

        /// <summary>
        /// A dictionary caching a map from each of the voxel's 6 faces to a list of 4 vertices.
        /// This is useful for determining which vertices are visible on a face, allowing us to 
        /// hide invisible vertices.
        /// </summary>
        public static readonly Dictionary<BoxFace, VoxelVertex[]> FaceVertices =
            new Dictionary<BoxFace, VoxelVertex[]>();

        /// <summary>
        /// A list of normalized offsets corresponding to the 6 neighbors of a voxel
        /// which touch exactly one of the voxel's faces.
        /// (That is [1, 0, 0], [-1, 0, 0], [0, 1, 0], [0, -1, 0], [0, 0, 1], [0, 0, -1])
        /// This is done for caching.
        /// </summary>
        public static List<Vector3> ManhattanSuccessors;
        
        /// <summary>
        /// Caches a list of 4 offsets corresponding to the neighbors of a voxel
        /// which touch its x or z faces and which have the same y coordinate.
        /// </summary>
        public static List<Vector3> Manhattan2DSuccessors;
        
        /// <summary>
        /// This takes the neighbors stored in Manhattan2DSuccessors and turns them into a 
        /// binary hash. Using this hash, we can index into a texture to determine which texture
        /// to draw on each voxel face. For example, if all four of the neighbors are filled, 
        /// the binary hash is 1111 = 15. If all of the neighbors but one is filled, the hash might
        /// be 0111 = 7, or 1011 = 11, and so on. That means there are 2^4 = 16 unique textures that
        /// might be displayed on the top of a voxel, making for cool looking transitions.
        /// The same hash is used to determine how much a voxel slopes.
        /// </summary>
        private static int[] manhattan2DMultipliers;
        
        
        /// <summary>
        /// This perlin noise determines how dense detail grass motes are, giving us smooth clumps of
        /// grass.
        /// </summary>
        public static Perlin MoteNoise = new Perlin(0);

        /// <summary>
        /// This Perlin noise determines how much detail grass motes get scaled, giving us smooth
        /// transitions between large and small blades of grass.
        /// </summary>
        public static Perlin MoteScaleNoise = new Perlin(250);
        
        /// <summary> The number of voxels in the X direction </summary>
        private readonly int sizeX = -1;
        /// <summary> The number of voxels in the Y direction </summary>
        private readonly int sizeY = -1;
        /// <summary> The number of voxels in the Z direction </summary>
        private readonly int sizeZ = -1;
        /// <summary> 
        /// If true, the water manager has updated the liquid stored in this chunk (water, lava, etc.).
        /// This means we must update the geometry of the chunk.
        /// </summary>
        public bool NewLiquidReceived = false;
        /// <summary>
        /// When the ChunkManager updates the geometry of this chunk, it puts the new
        /// vertex buffer here, rather than directly setting the vertex buffer. This is 
        /// to help with cases where the ChunkManager destroys the vertex buffer as it is
        /// being drawn, or other craziness that can happen with threading.
        /// </summary>
        public VoxelListPrimitive NewPrimitive = null;
        /// <summary>
        /// If the chunk has received a new vertex buffer, this is set to true.
        /// </summary>
        public bool NewPrimitiveReceived = false;
        /// <summary>
        /// This is true the very first time that the chunks' vertex buffer has been built.
        /// It is false afterwards.
        /// </summary>
        private bool firstRebuild = true;
        /// <summary>
        /// This is a BoundingBox completely containing this chunk and all its voxels
        /// </summary>
        private BoundingBox m_boundingBox;
        /// <summary>
        /// This is set to true if the BoundingBox has been initialized
        /// </summary>
        private bool m_boundingBoxCreated;
        /// <summary>
        /// This is a BoundingSphere completely containing this chunk and all its voxels.
        /// </summary>
        private BoundingSphere m_boundingSphere;
        /// <summary>
        /// This is set to true if the BoundingSphere has been initialized.
        /// </summary>
        private bool m_boundingSphereCreated;
        /// <summary>
        /// This is an integer number of voxels per game unit.
        /// It should always be set to one! (TODO: get rid of this)
        /// </summary>
        private int tileSize = -1;

        /// <summary>
        /// Construct a new VoxelChunk
        /// </summary>
        /// <param name="manager"> The chunk manager </param>
        /// <param name="origin"> The leastmost coordinate of the chunk </param>
        /// <param name="tileSize"> The number of voxels per coordinate (should be exactly 1) </param>
        /// <param name="id"> Integer id of the chunk </param>
        /// <param name="sizeX"> The number of voxels in the X direction </param>
        /// <param name="sizeY"> The number of voxels in the Y direction </param>
        /// <param name="sizeZ"> The number of voxels in the Z direction </param>
        public VoxelChunk(ChunkManager manager, Vector3 origin, int tileSize, Point3 id, int sizeX, int sizeY, int sizeZ)
        {
            FirstWaterIter = true;
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            this.sizeZ = sizeZ;
            ID = id;
            Origin = origin;
            Data = AllocateData(sizeX, sizeY, sizeZ);
            IsVisible = true;
            ShouldRebuild = true;
            this.tileSize = tileSize;
            HalfLength = new Vector3(this.tileSize/2.0f, this.tileSize/2.0f, this.tileSize/2.0f);
            Primitive = new VoxelListPrimitive();
            RenderWireframe = false;
            Manager = manager;
            IsActive = true;

            InitializeStatics();
            PrimitiveMutex = new Mutex();
            ShouldRecalculateLighting = true;
            Neighbors = new ConcurrentDictionary<Point3, VoxelChunk>();
            DynamicLights = new List<DynamicLight>();
            Liquids = new Dictionary<LiquidType, LiquidPrimitive>();
            Liquids[LiquidType.Water] = new LiquidPrimitive(LiquidType.Water);
            Liquids[LiquidType.Lava] = new LiquidPrimitive(LiquidType.Lava);
            ShouldRebuildWater = true;
            Springs = new ConcurrentDictionary<Voxel, byte>();

            IsRebuilding = false;
            LightingCalculated = false;
            RebuildPending = false;
            RebuildLiquidPending = false;
            ReconstructRamps = true;
        }

        /// <summary>
        /// For each kind of detail mode, contains a list of positions and colors
        /// associated with this chunk.
        /// </summary>
        public Dictionary<string, List<InstanceData>> Motes { get; set; }
        /// <summary>
        /// The vertex buffer associated with this chunk's geometry.
        /// </summary>
        public VoxelListPrimitive Primitive { get; set; }
        /// <summary>
        /// Contains a dictionary from liquid type (water, lava, etc.) to vertex buffers for this chunk.
        /// </summary>
        public Dictionary<LiquidType, LiquidPrimitive> Liquids { get; set; }
        
        /// <summary>
        /// The raw data associated with this chunk's voxels.
        /// </summary>
        public VoxelData Data { get; set; }
        
        /// <summary>
        /// DEPRECATED. Springs were originally sources of liquid.
        /// They were removed because they caused excessive flooding.
        /// </summary>
        public ConcurrentDictionary<Voxel, byte> Springs { get; set; }

        /// <summary> get the number of voxels in the X direction </summary>
        public int SizeX
        {
            get { return sizeX; }
        }

        /// <summary> get the number of voxels in the Y direction </summary>
        public int SizeY
        {
            get { return sizeY; }
        }

        /// <summary> get the number of voxels in the Z direction </summary>
        public int SizeZ
        {
            get { return sizeZ; }
        }

        /// <summary> If true, the chunk should be drawn this frame. </summary>
        public bool IsVisible { get; set; }
        /// <summary> If true, the chunk manager should rebuild the vertex buffer for this chunk </summary>
        public bool ShouldRebuild { get; set; }
        /// <summary> If true, the chunk manager is currently rebuilding this chunk </summary>
        public bool IsRebuilding { get; set; }
        /// <summary> gets the least most coordinate of this chunk in world space </summary>
        public Vector3 Origin { get; set; }
        /// <summary> gets a vector which is half the width of a voxel </summary>
        private Vector3 HalfLength { get; set; }
        /// <summary> If true, renders a wireframe representation of the chunk's mesh instead of the real geometry </summary>
        public bool RenderWireframe { get; set; }
        /// <summary> gets the ChunkManager </summary>
        public ChunkManager Manager { get; set; }
        /// <summary> Unnecessary, deprecated. TODO: remove </summary>
        public bool IsActive { get; set; }
        /// <summary> If true, water hasn't yet been updated for this chunk </summary>
        public bool FirstWaterIter { get; set; }
        /// <summary> Mutex which controls access to the chunk's vertex buffer mesh </summary>
        public Mutex PrimitiveMutex { get; set; }
        /// <summary> If true, sunlight, ambient light, and torchlight should be computed for this voxel.
        public bool ShouldRecalculateLighting { get; set; }
        /// <summary> If true, the vertex buffers associated with this chunk's liquids should be rebuilt </summary>
        public bool ShouldRebuildWater { get; set; }
        /// <summary> Concurrent dictionary caching the 8-connected neighbors of this chunk </summary>
        public ConcurrentDictionary<Point3, VoxelChunk> Neighbors { get; set; }
        /// <summary> List of torchlight producing sources in this chunk </summary>
        public List<DynamicLight> DynamicLights { get; set; }
        /// <summary> If true, sunlight, dynamic light and ambient light have already been calculated for this chunk </summary>
        public bool LightingCalculated { get; set; }
        /// <summary> If true, the chunk manager knows about this chunk, but has not yet rebuilt its vertex buffer. </summary>
        public bool RebuildPending { get; set; }
        /// <summary> If true, the chunk manager knows about this chunk, but has not yet rebuilt its liquid (water, lava) vertex buffers </summary>
        public bool RebuildLiquidPending { get; set; }
        /// <summary> Global identifier of this chunk </summary>
        public Point3 ID { get; set; }
        /// <summary> If true, the chunk manager will recalculate the slopes of ramping voxels (like dirt) for this chunk </summary>
        public bool ReconstructRamps { get; set; }

        /// <summary> Convert the chunk's 3D ID into a linear hash value </summary>
        public uint GetID()
        {
            return (uint) ID.GetHashCode();
        }

        /// <summary> Get the bounding box completely containing this chunk and all its voxels </summary>
        public BoundingBox GetBoundingBox()
        {
            if (!m_boundingBoxCreated)
            {
                Vector3 max = new Vector3(sizeX, sizeY, sizeZ) + Origin;
                m_boundingBox = new BoundingBox(Origin, max);
                m_boundingBoxCreated = true;
            }

            return m_boundingBox;
        }

        #region statics

        /// <summary> Generate static data for all VoxelChunk instances </summary>
        public static void InitializeStatics()
        {
            if (staticsInitialized)
            {
                return;
            }

            // Vertex deltas tell us the offset from a voxel's leastmost corner
            // to each of its 8 vertices.
            vertexDeltas[(int) VoxelVertex.BackBottomLeft] = new Vector3(0, 0, 0);
            vertexDeltas[(int) VoxelVertex.BackTopLeft] = new Vector3(0, 1.0f, 0);
            vertexDeltas[(int) VoxelVertex.BackBottomRight] = new Vector3(1.0f, 0, 0);
            vertexDeltas[(int) VoxelVertex.BackTopRight] = new Vector3(1.0f, 1.0f, 0);

            vertexDeltas[(int) VoxelVertex.FrontBottomLeft] = new Vector3(0, 0, 1.0f);
            vertexDeltas[(int) VoxelVertex.FrontTopLeft] = new Vector3(0, 1.0f, 1.0f);
            vertexDeltas[(int) VoxelVertex.FrontBottomRight] = new Vector3(1.0f, 0, 1.0f);
            vertexDeltas[(int) VoxelVertex.FrontTopRight] = new Vector3(1.0f, 1.0f, 1.0f);

            // Neighbor directions for neighbors which only touch one face
            // of a voxel.
            ManhattanSuccessors = new List<Vector3>
            {
                new Vector3(1.0f, 0, 0),
                new Vector3(-1.0f, 0, 0),
                new Vector3(0, -1.0f, 0),
                new Vector3(0, 1.0f, 0),
                new Vector3(0, 0, -1.0f),
                new Vector3(0, 0, 1.0f)
            };

            // Neighbor directions for neighbors which touch either the X or Z
            // faces of a voxel.
            Manhattan2DSuccessors = new List<Vector3>
            {
                new Vector3(-1.0f, 0, 0),
                new Vector3(1.0f, 0, 0),
                new Vector3(0, 0, -1.0f),
                new Vector3(0, 0, 1.0f)
            };

            // For each of the 2D successors, defines a binary hash.
            manhattan2DMultipliers = new[]
            {
                2,
                8,
                4,
                1
            };


            // Defines the center of each face w.r.t the leastmost corner 
            // of a voxel.
            faceDeltas[(int) BoxFace.Top] = new Vector3(0.5f, 0.0f, 0.5f);
            faceDeltas[(int) BoxFace.Bottom] = new Vector3(0.5f, 1.0f, 0.5f);
            faceDeltas[(int) BoxFace.Left] = new Vector3(1.0f, 0.5f, 0.5f);
            faceDeltas[(int) BoxFace.Right] = new Vector3(0.0f, 0.5f, 0.5f);
            faceDeltas[(int) BoxFace.Front] = new Vector3(0.5f, 0.5f, 0.0f);
            faceDeltas[(int) BoxFace.Back] = new Vector3(0.5f, 0.5f, 1.0f);

            // Define all the vertices associated with each face.
            FaceVertices[BoxFace.Top] = new[]
            {
                VoxelVertex.BackTopLeft,
                VoxelVertex.BackTopRight,
                VoxelVertex.FrontTopLeft,
                VoxelVertex.FrontTopRight
            };
            FaceVertices[BoxFace.Bottom] = new[]
            {
                VoxelVertex.BackBottomLeft,
                VoxelVertex.BackBottomRight,
                VoxelVertex.FrontBottomLeft,
                VoxelVertex.FrontBottomRight
            };
            FaceVertices[BoxFace.Left] = new[]
            {
                VoxelVertex.BackTopLeft,
                VoxelVertex.BackBottomLeft,
                VoxelVertex.FrontTopLeft,
                VoxelVertex.FrontBottomLeft
            };
            FaceVertices[BoxFace.Right] = new[]
            {
                VoxelVertex.BackTopRight,
                VoxelVertex.BackBottomRight,
                VoxelVertex.FrontTopRight,
                VoxelVertex.FrontBottomRight
            };
            FaceVertices[BoxFace.Front] = new[]
            {
                VoxelVertex.FrontBottomLeft,
                VoxelVertex.FrontBottomRight,
                VoxelVertex.FrontTopLeft,
                VoxelVertex.FrontTopRight
            };
            FaceVertices[BoxFace.Back] = new[]
            {
                VoxelVertex.BackTopLeft,
                VoxelVertex.BackTopRight,
                VoxelVertex.BackBottomLeft,
                VoxelVertex.BackBottomRight
            };

            // Find all of the voxels which touch each vertex. Store them in caches.
            for (int i = 0; i < 8; i++)
            {
                // The vertex we are considering.
                var vertex = (VoxelVertex) (i);
                // All the voxels which touch this vertex.
                var successors = new List<Vector3>();
                // All the voxels which touch this vertex but which have the same Y height
                var diagSuccessors = new List<Vector3>();
                int xlim = 0;
                int ylim = 0;
                int zlim = 0;
                int nXLim = 0;
                int nYLim = 0;
                int nZLim = 0;

                switch (vertex)
                {
                        // Back = -z, bottom = -y, left = -x
                        // Successors are all in the -1 to 0 range.
                    case VoxelVertex.BackBottomLeft:
                        nXLim = -1;
                        xlim = 1;

                        nYLim = -1;
                        ylim = 1;

                        nZLim = -1;
                        zlim = 1;
                        break;

                        // Back = -z, Bottom = -y, Right = +x
                        // z Successors are [-1, 0]
                        // y successors are [-1, 0]
                        // x Successors are [0, 1]
                    case VoxelVertex.BackBottomRight:
                        nXLim = 0;
                        xlim = 2;

                        nYLim = -1;
                        ylim = 1;

                        nZLim = -1;
                        zlim = 1;
                        break;

                        // Back = -z, Top = +y, Left = -x
                        // z Successors are [-1, 0]
                        // y successors are [0, 1]
                        // x Successors are [-1, 1]
                    case VoxelVertex.BackTopLeft:
                        nXLim = -1;
                        xlim = 1;

                        nYLim = 0;
                        ylim = 2;

                        nZLim = -1;
                        zlim = 1;
                        break;

                        // Back = -z, Top = +y, Right = +x
                        // z Successors are [-1, 0]
                        // y successors are [0, 1]
                        // x Successors are [0, 1]
                    case VoxelVertex.BackTopRight:
                        nXLim = 0;
                        xlim = 2;

                        nYLim = 0;
                        ylim = 2;

                        nZLim = -1;
                        zlim = 1;
                        break;

                        // Front = +z, Bottom = -y, Left = -x
                        // z Successors are [0, 1]
                        // y successors are [-1, 0]
                        // x Successors are [-1, 0]
                    case VoxelVertex.FrontBottomLeft:
                        nXLim = -1;
                        xlim = 1;

                        nYLim = -1;
                        ylim = 1;

                        nZLim = 0;
                        zlim = 2;
                        break;

                        // Front = +z, Bottom = -y, Right = +x
                        // z Successors are [0, 1]
                        // y successors are [-1, 0]
                        // x Successors are [0, 1]
                    case VoxelVertex.FrontBottomRight:
                        nXLim = 0;
                        xlim = 2;

                        nYLim = -1;
                        ylim = 1;

                        nZLim = 0;
                        zlim = 2;
                        break;

                        // Front = +z, Top = +y, Left = -x
                        // z Successors are [0, 1]
                        // y successors are [0, 1]
                        // x Successors are [-1, 0]
                    case VoxelVertex.FrontTopLeft:
                        nXLim = -1;
                        xlim = 1;
                        nYLim = 0;
                        ylim = 2;
                        nZLim = 0;
                        zlim = 2;
                        break;

                        // Front = +z, Top = +y, Right = +x
                        // z Successors are [0, 1]
                        // y successors are [0, 1]
                        // x Successors are [0, 1]
                    case VoxelVertex.FrontTopRight:
                        nXLim = 0;
                        xlim = 2;
                        nYLim = 0;
                        ylim = 2;
                        nZLim = 0;
                        zlim = 2;
                        break;
                }

                // Loop through all the voxels adjacent to this vertex.
                for (int dx = nXLim; dx < xlim; dx++)
                {
                    for (int dy = nYLim; dy < ylim; dy++)
                    {
                        for (int dz = nZLim; dz < zlim; dz++)
                        {
                            successors.Add(new Vector3(dx, dy, dz));
                            // Only add successors with the same Y height.
                            if ((dx != 0 && dz != 0 && dy == 0))
                            {
                                diagSuccessors.Add(new Vector3(dx, dy, dz));
                            }
                        }
                    }
                }

                VertexSuccessors[vertex] = successors;
                VertexSuccessorsDiag[vertex] = diagSuccessors;
            }

            staticsInitialized = true;
        }

        #endregion

        /// <summary> called whenever a specific voxel in this chunk is destroyed </summary>
        public event VoxelDestroyed OnVoxelDestroyed;
        /// <summary> called whenever a specific voxel in this chunk is revealed </summary>
        public event VoxelExplored OnVoxelExplored;

        /// <summary> notify all subscribers that the given voxel has been explored </summary>
        /// <param name="voxel"> The chunk-relative coordinates of the voxel in question </param>
        public void NotifyExplored(Point3 voxel)
        {
            if (OnVoxelExplored != null)
            {
                OnVoxelExplored.Invoke(voxel);
            }
        }
        
        /// <summary> notify all subscribers that the given voxel has been destroyed </summary>
        /// <param name="voxel"> The chunk-relative coordinates of the voxel in question </param>
        public void NotifyDestroyed(Point3 voxel)
        {
            if (OnVoxelDestroyed != null)
            {
                OnVoxelDestroyed.Invoke(voxel);
            }
        }

        /// <summary> Allocate the raw data associated with this voxel. </summary>
        /// <param name="sx"> The number of voxels in X </param>
        /// <param name="sy"> The number of voxels in Y </param>
        /// <param name="sz"> The number of voxels in Z </param>
        public static VoxelData AllocateData(int sx, int sy, int sz)
        {
            int numVoxels = sx*sy*sz;
            var toReturn = new VoxelData
            {
                Health = new byte[numVoxels],
                IsExplored = new bool[numVoxels],
                SunColors = new byte[numVoxels],
                Types = new byte[numVoxels],
                Water = new WaterCell[numVoxels],
                RampTypes = new RampType[numVoxels],
                VertexColors = new Color[(sx + 1)*(sy + 1)*(sz + 1)],
                SizeX = sx,
                SizeY = sy,
                SizeZ = sz
            };

            for (int i = 0; i < numVoxels; i++)
            {
                toReturn.Water[i] = new WaterCell();
            }

            return toReturn;
        }


        /// <summary> DEPRECATED. DO NOT USE. Water data is now stored inside VoxelData! </summary>
        public static WaterCell[][][] WaterAllocate(int sx, int sy, int sz)
        {
            var w = new WaterCell[sx][][];

            for (int x = 0; x < sx; x++)
            {
                w[x] = new WaterCell[sy][];

                for (int y = 0; y < sy; y++)
                {
                    w[x][y] = new WaterCell[sz];

                    for (int z = 0; z < sx; z++)
                    {
                        w[x][y][z] = new WaterCell();
                    }
                }
            }

            return w;
        }

        /// <summary>
        /// Given some position relative to a voxel's leastmost coordinate,
        /// returns the vertex closest to that position.
        /// </summary>
        public static VoxelVertex GetNearestDelta(Vector3 position)
        {
            float bestDist = float.MaxValue;
            var bestKey = VoxelVertex.BackTopRight;
            for (int i = 0; i < 8; i++)
            {
                float dist = (position - vertexDeltas[i]).LengthSquared();
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestKey = (VoxelVertex) (i);
                }
            }


            return bestKey;
        }

        /// <summary>
        /// Given a position relative to a voxel's leastmost coordinate,
        /// returns the face closest to that position.
        /// </summary>
        public static BoxFace GetNearestFace(Vector3 position)
        {
            float bestDist = 10000000;
            var bestKey = BoxFace.Top;
            for (int i = 0; i < 6; i++)
            {
                float dist = (position - faceDeltas[i]).LengthSquared();
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestKey = (BoxFace) (i);
                }
            }


            return bestKey;
        }

        /// <summary> 
        /// Computes a sphere centered on this voxel chunk which
        /// completely encompasses the chunk.
        /// </summary>
        public BoundingSphere GetBoundingSphere()
        {
            if (!m_boundingSphereCreated)
            {
                float m = Math.Max(Math.Max(sizeX, sizeY), sizeZ)*0.5f;
                m_boundingSphere = new BoundingSphere(Origin + new Vector3(sizeX, sizeY, sizeZ)*0.5f,
                    (float) Math.Sqrt(3*m*m));
                m_boundingSphereCreated = true;
            }

            return m_boundingSphere;
        }

        /// <summary>
        /// Updates the chunk.
        /// </summary>
        public void Update(DwarfTime t)
        {
            // Wait for new data from the chunk manager.
            PrimitiveMutex.WaitOne();
            if (NewPrimitiveReceived)
            {
                Primitive = NewPrimitive;
                NewPrimitive = null;
                NewPrimitiveReceived = false;
            }
            PrimitiveMutex.ReleaseMutex();
        }

        /// <summary>
        /// Renders the chunk to the screen.
        /// </summary>
        public void Render(GraphicsDevice device)
        {
            if (!RenderWireframe)
            {
                Primitive.Render(device);
            }
            else
            {
                Primitive.RenderWireframe(device);
            }
        }

        /// <summary>
        /// Rebuilds the liquid vertex buffers (water, lava, etc.)
        /// ascciated with this chunk.
        /// </summary>
        public void RebuildLiquids()
        {
            foreach (var primitive in Liquids)
            {
                primitive.Value.InitializeFromChunk(this);
            }
            ShouldRebuildWater = false;
        }

        /// <summary>
        /// Gets the maximum byte in a list of bytes.
        /// </summary>
        private byte getMax(byte[] values)
        {
            byte max = 0;

            foreach (byte b in values)
            {
                if (b > max)
                {
                    max = b;
                }
            }

            return max;
        }

        /// <summary> Clamps a float v to be between -a and a. </summary>
        private float Clamp(float v, float a)
        {
            if (v > a)
            {
                return a;
            }

            if (v < -a)
            {
                return -a;
            }

            return v;
        }
        
        /// <summary> Clamps the components of vector v to be between -a and a </summary>
        private Vector3 ClampVector(Vector3 v, float a)
        {
            v.X = Clamp(v.X, a);
            v.Y = Clamp(v.Y, a);
            v.Z = Clamp(v.Z, a);
            return v;
        }

        /// <summary> Creates a new voxel referencing the one stored at x, y, z in this chunk </summary>
        public Voxel MakeVoxel(int x, int y, int z)
        {
            return new Voxel(new Point3(x, y, z), this);
        }

        /// <summary> 
        /// Construct the detail motes associated with this chunk
        /// and a particular biome. 
        /// </summary>
        public void BuildGrassMotes(Overworld.Biome biome)
        {
            BiomeData biomeData = BiomeLibrary.Biomes[biome];

            string grassType = biomeData.GrassVoxel;

            for (int i = 0; i < biomeData.Motes.Count; i++)
            {
                var grassPositions = new List<Vector3>();
                var grassColors = new List<Color>();
                var grassScales = new List<float>();
                DetailMoteData moteData = biomeData.Motes[i];
                Voxel v = MakeVoxel(0, 0, 0);
                Voxel voxelBelow = MakeVoxel(0, 0, 0);
                for (int x = 0; x < SizeX; x++)
                {
                    for (int y = 1; y < Math.Min(Manager.ChunkData.MaxViewingLevel + 1, SizeY - 1); y++)
                    {
                        for (int z = 0; z < SizeZ; z++)
                        {
                            v.GridPosition = new Vector3(x, y, z);
                            voxelBelow.GridPosition = new Vector3(x, y - 1, z);

                            if (v.IsEmpty || voxelBelow.IsEmpty
                                || v.Type.Name != grassType || !v.IsVisible
                                || voxelBelow.WaterLevel != 0)
                            {
                                continue;
                            }

                            float vOffset = 0.0f;

                            if (v.RampType != RampType.None)
                            {
                                vOffset = -0.5f;
                            }

                            float value = MoteNoise.Noise(v.Position.X*moteData.RegionScale,
                                v.Position.Y*moteData.RegionScale, v.Position.Z*moteData.RegionScale);
                            float s =
                                MoteScaleNoise.Noise(v.Position.X*moteData.RegionScale,
                                    v.Position.Y*moteData.RegionScale, v.Position.Z*moteData.RegionScale)*
                                moteData.MoteScale;

                            if (!(Math.Abs(value) > moteData.SpawnThreshold))
                            {
                                continue;
                            }

                            Vector3 smallNoise =
                                ClampVector(
                                    VertexNoise.GetRandomNoiseVector(v.Position*moteData.RegionScale*20.0f)*20.0f, 0.4f);
                            smallNoise.Y = 0.0f;
                            grassPositions.Add(v.Position + new Vector3(0.5f, 1.0f + s*0.5f + vOffset, 0.5f) +
                                               smallNoise);
                            grassScales.Add(s);
                            grassColors.Add(new Color(v.SunColor, 128, 0));
                        }
                    }
                }

                if (Motes == null)
                {
                    Motes = new Dictionary<string, List<InstanceData>>();
                }

                if (Motes.Count < i + 1)
                {
                    Motes[moteData.Name] = new List<InstanceData>();
                }

                Motes[moteData.Name] = EntityFactory.GenerateGrassMotes(grassPositions,
                    grassColors, grassScales, Manager.Components, Manager.Content, Manager.Graphics,
                    Motes[moteData.Name], moteData.Asset, moteData.Name);
            }
        }

        /// <summary>
        /// Updates the sloping voxels ramp state.
        /// </summary>
        public void UpdateRamps()
        {
            if (ReconstructRamps || firstRebuild)
            {
                //VoxelListPrimitive.UpdateRamps(this);
                VoxelListPrimitive.UpdateCornerRamps(this);
                ReconstructRamps = false;
            }
        }

        /// <summary>
        /// Builds the grass detail motes and determines the chunk's biome.
        /// </summary>
        public void BuildGrassMotes()
        {
            Vector2 v = new Vector2(Origin.X, Origin.Z)/PlayState.WorldScale;

            Overworld.Biome biome =
                Overworld.Map[
                    (int) MathFunctions.Clamp(v.X, 0, Overworld.Map.GetLength(0) - 1),
                    (int) MathFunctions.Clamp(v.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;
            BuildGrassMotes(biome);
        }

        /// <summary>
        /// Rebuilds the vertex buffer, grass motes, and other details of the chunk.
        /// </summary>
        /// <param name="g">The graphics device.</param>
        public void Rebuild(GraphicsDevice g)
        {
            if (g == null || g.IsDisposed)
            {
                return;
            }
            IsRebuilding = true;

            BuildPrimitive(g);
            BuildGrassMotes();
            if (firstRebuild)
            {
                firstRebuild = false;
            }
            RebuildLiquids();
            IsRebuilding = false;

            if (ShouldRecalculateLighting)
            {
                NotifyChangedComponents();
            }
            ShouldRebuild = false;
        }

        /// <summary>
        /// Creates a new vertex buffer for this chunk.
        /// </summary>
        /// <param name="g">The graphics device</param>
        public void BuildPrimitive(GraphicsDevice g)
        {
            var primitive = new VoxelListPrimitive();
            primitive.InitializeFromChunk(this, g);
        }

        /// <summary>
        /// Finds and notifies all components that are inside the chunk that the chunk has been modified.
        /// </summary>
        public void NotifyChangedComponents()
        {
            var componentsInside = new HashSet<IBoundedObject>();
            Manager.Components.CollisionManager.GetObjectsIntersecting(GetBoundingBox(), componentsInside,
                CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

            var changedMessage = new Message(Message.MessageType.OnChunkModified, "Chunk Modified");

            foreach (IBoundedObject c in componentsInside)
            {
                if (c is GameComponent)
                {
                    ((GameComponent) c).ReceiveMessageRecursive(changedMessage);
                }
            }
        }

        /// <summary>
        /// Destroys the primitive and associated detail motes.
        /// </summary>
        /// <param name="device">The graphics device.</param>
        public void Destroy(GraphicsDevice device)
        {
            if (Primitive != null)
            {
                Primitive.ResetBuffer(device);
            }

            if (Motes != null)
            {
                foreach (var mote in Motes)
                {
                    foreach (InstanceData mote2 in mote.Value)
                    {
                        EntityFactory.InstanceManager.RemoveInstance(mote.Key, mote2);
                    }
                }
            }
        }

        #region transformations

        /// <summary>
        /// Transforms a vector from world coordinates to chunk-relative coordinates.
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <returns>A floating-point vector relative to the chunk's origin.</returns>
        public Vector3 WorldToGrid(Vector3 worldLocation)
        {
            Vector3 grid = (worldLocation - Origin);
            return grid;
        }

        /// <summary>
        /// Gets the voxel at a world location.
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <param name="voxel">The voxel to return.</param>
        /// <returns>True if a voxel exists in this chunk at the given location. False otherwise.</returns>
        public bool GetVoxelAtWorldLocation(Vector3 worldLocation, ref Voxel voxel)
        {
            Vector3 grid = WorldToGrid(worldLocation);

            bool valid = IsCellValid(MathFunctions.FloorInt(grid.X), MathFunctions.FloorInt(grid.Y),
                MathFunctions.FloorInt(grid.Z));

            if (valid)
            {
                voxel.Chunk = this;
                voxel.GridPosition = new Vector3(MathFunctions.FloorInt(grid.X), MathFunctions.FloorInt(grid.Y),
                    MathFunctions.FloorInt(grid.Z));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the grid position (relative to the chunk's origin) is inside the chunk.
        /// </summary>
        /// <param name="grid">The grid position (relative to the chunk's origin)</param>
        /// <returns>
        ///   <c>true</c> if [is grid position valid] [the specified grid]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsGridPositionValid(Vector3 grid)
        {
            return IsCellValid((int) grid.X, (int) grid.Y, (int) grid.Z);
        }

        /// <summary>
        /// Determines whether a specified position in world coordinates is inside the chunk.
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <returns>
        ///   <c>true</c> if [is world location valid] [the specified world location]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsWorldLocationValid(Vector3 worldLocation)
        {
            Vector3 grid = WorldToGrid(worldLocation);

            return IsCellValid((int) grid.X, (int) grid.Y, (int) grid.Z);
        }

        /// <summary>
        /// Determines whether the voxel at [x, y, z] exists.
        /// </summary>
        /// <param name="x">The x coordinate of the voxel in the chunk.</param>
        /// <param name="y">The y coordinate of the voxel in the chunk.</param>
        /// <param name="z">The z coordinate of the voxel in the chunk.</param>
        /// <returns>
        ///   <c>true</c> if the coordinates are inside the chunk; otherwise, <c>false</c>.
        /// </returns>
        public bool IsCellValid(int x, int y, int z)
        {
            return x >= 0 && y >= 0 && z >= 0 && x < SizeX && y < SizeY && z < SizeZ;
        }

        #endregion transformations

        #region lighting

        /// <summary>
        /// Gets the brightness of a dynamic light at the given voxel..
        /// </summary>
        /// <param name="light">The light.</param>
        /// <param name="lightIntensity">The light intensity.</param>
        /// <param name="voxel">The voxel.</param>
        /// <returns>The intensity of the light as observed from the given voxel.</returns>
        public byte GetIntensity(DynamicLight light, byte lightIntensity, Voxel voxel)
        {
            Vector3 vertexPos = voxel.Position;
            Vector3 diff = vertexPos - (light.Position + new Vector3(0.5f, 0.5f, 0.5f));
            float dist = diff.LengthSquared()*2;

            return (byte) (int) ((Math.Min(1.0f/(dist + 0.0001f), 1.0f))*light.Intensity);
        }


        /// <summary>
        /// Calculates the vertex color for a specified voxel and vertex 
        /// </summary>
        /// <param name="vox">The voxel.</param>
        /// <param name="face">The vertex to find the light for.</param>
        /// <param name="chunks">The chunks manager</param>
        /// <param name="neighbors">The neighbors of the vertex. This will be computed inside and allocated/shared outside the fn.</param>
        /// <param name="color">The color to return</param>
        public static void CalculateVertexLight(Voxel vox, VoxelVertex face,
            ChunkManager chunks, List<Voxel> neighbors, ref VertexColorInfo color)
        {
            float numHit = 1;
            float numChecked = 1;

            int index = vox.Index;
            color.DynamicColor = 0;
            color.SunColor += vox.Chunk.Data.SunColors[index];
            vox.Chunk.GetNeighborsVertex(face, vox, neighbors);

            // Calculate ambient light by summing up the number of occupied voxels around the vertex.
            foreach (Voxel v in neighbors)
            {
                if (!chunks.ChunkData.ChunkMap.ContainsKey(v.Chunk.ID))
                {
                    continue;
                }

                VoxelChunk c = chunks.ChunkData.ChunkMap[v.Chunk.ID];
                color.SunColor += c.Data.SunColors[v.Index];
                if (VoxelLibrary.IsSolid(v) || !v.IsExplored)
                {
                    if (v.Type.EmitsLight) color.DynamicColor = 255;
                    numHit++;
                    numChecked++;
                }
                else
                {
                    numChecked++;
                }
            }


            float proportionHit = numHit/numChecked;
            color.AmbientColor = (int) Math.Min((1.0f - proportionHit)*255.0f, 255);
            color.SunColor = (int) Math.Min(color.SunColor/numChecked, 255);
        }


        /// <summary>
        /// Resets the sunlight of the voxels in the chunk without considering
        /// chunk edges to the specified intensity.
        /// </summary>
        /// <param name="sunColor">Intensity of the sun.</param>
        public void ResetSunlightIgnoreEdges(byte sunColor)
        {
            for (int x = 1; x < SizeX - 1; x++)
            {
                for (int z = 1; z < SizeZ - 1; z++)
                {
                    for (int y = 0; y < SizeY; y++)
                    {
                        int index = Data.IndexAt(x, y, z);
                        Data.SunColors[index] = sunColor;
                    }
                }
            }
        }

        /// <summary>
        /// Resets the sunlight of the voxels in the chunk to the specified intensity.
        /// </summary>
        /// <param name="sunColor">Color of the sun.</param>
        public void ResetSunlight(byte sunColor)
        {
            int numVoxels = sizeX*sizeY*sizeZ;
            for (int i = 0; i < numVoxels; i++)
            {
                Data.SunColors[i] = sunColor;
            }
        }

        /// <summary>
        /// Gets the total height of water above the given voxel by summing up
        /// all of the water levels for voxels above this one.
        /// </summary>
        /// <param name="voxRef">The vox reference.</param>
        /// <returns>The sum of the water height above the current voxel.</returns>
        public float GetTotalWaterHeight(Voxel voxRef)
        {
            float tot = 0;
            var x = (int) voxRef.GridPosition.X;
            var z = (int) voxRef.GridPosition.Z;
            for (var y = (int) voxRef.GridPosition.Y; y < SizeY; y++)
            {
                int index = Data.IndexAt(x, y, z);
                tot += Data.Water[index].WaterLevel;

                if (Data.Water[index].WaterLevel == 0)
                {
                    return tot;
                }
            }

            return tot;
        }

        /// <summary>
        /// Gets the total water height in voxels above the current voxel.
        /// </summary>
        /// <param name="voxRef">The vox reference.</param>
        /// <returns>The total height of the water in voxels at and above this one.</returns>
        public float GetTotalWaterHeightCells(Voxel voxRef)
        {
            return GetTotalWaterHeight(voxRef)/8.0f;
        }

        /// <summary>
        /// Calculates the sunlight for all cells in the chunk by casting
        /// rays of sunlight downward from the sky.
        /// </summary>
        /// <param name="sunColor">Intensity of the sun.</param>
        public void UpdateSunlight(byte sunColor)
        {
            LightingCalculated = false;

            if (!Manager.ChunkData.ChunkMap.ContainsKey(ID))
            {
                return;
            }

            ResetSunlight(0);
            Voxel reference = MakeVoxel(0, 0, 0);

            // For each voxel in the XZ plane.
            for (int x = 0; x < SizeX; x++)
            {
                for (int z = 0; z < SizeZ; z++)
                {
                    // Cast a ray down from the top of the chunk.
                    for (int y = SizeY - 1; y >= 0; y--)
                    {
                        reference.GridPosition = new Vector3(x, y, z);
                        int index = Data.IndexAt(x, y, z);
                        if (Data.Types[index] == 0)
                        {
                            // Fill empty voxels with sunlight.
                            Data.SunColors[index] = sunColor;
                            continue;
                        }

                        if (y >= SizeY - 1)
                        {
                            continue;
                        }

                        // If we hit a voxel, set its color and break.
                        Data.SunColors[reference.Index] = sunColor;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the shared vertices between a voxel and all its neighbors that touch a given vertex of that voxel.
        /// </summary>
        /// <param name="v">The voxel.</param>
        /// <param name="vertex">The vertex to check.</param>
        /// <param name="vertices">A list containing the voxels and for each filled voxel a list of vertices that touch the given vertex.</param>
        /// <param name="neighbors">The neighbors of the voxel vertex (pre-allocated outside the function)</param>
        public void GetSharedVertices(Voxel v, VoxelVertex vertex, List<KeyValuePair<Voxel, List<VoxelVertex>>> vertices,
            List<Voxel> neighbors)
        {
            vertices.Clear();
            GetNeighborsVertex(vertex, v, neighbors);

            Vector3 myDelta = vertexDeltas[(int) vertex];
            foreach (Voxel neighbor in neighbors)
            {
                if (neighbor == null || neighbor.IsEmpty)
                {
                    continue;
                }

                var vertsNeighbor = new List<VoxelVertex>();
                Vector3 otherDelta = v.Position - neighbor.Position + myDelta;
                vertsNeighbor.Add(GetNearestDelta(otherDelta));


                vertices.Add(new KeyValuePair<Voxel, List<VoxelVertex>>(neighbor, vertsNeighbor));
            }
        }

        /// <summary>
        /// Calculates the vertex lighting for all voxels in the chunk.
        /// </summary>
        public void CalculateVertexLighting()
        {
            var neighbors = new List<Voxel>();
            var colorInfo = new VertexColorInfo();
            bool ambientOcclusion = GameSettings.Default.AmbientOcclusion;
            Voxel voxel = MakeVoxel(0, 0, 0);
            // Store a set of all vertex indexes that have been updated already.
            // Only update each vertex once. This is important because each vertex 
            // must have a consistent color.
            var indexesToUpdate = new HashSet<int>();

            for (int x = 0; x < SizeX; x++)
            {
                for (int y = 0; y < Math.Min(Manager.ChunkData.MaxViewingLevel + 1, SizeY); y++)
                {
                    for (int z = 0; z < SizeZ; z++)
                    {
                        voxel.GridPosition = new Vector3(x, y, z);

                        if (voxel.IsEmpty)
                        {
                            continue;
                        }

                        VoxelType type = voxel.Type;

                        if (VoxelLibrary.IsSolid(voxel) && voxel.IsVisible)
                        {
                            if (ambientOcclusion)
                            {
                                for (int i = 0; i < 8; i++)
                                {
                                    int vertIndex = Data.VertIndex(x, y, z, (VoxelVertex) i);
                                    if (indexesToUpdate.Contains(vertIndex)) continue;
                                    indexesToUpdate.Add(Data.VertIndex(x, y, z, (VoxelVertex) i));
                                    CalculateVertexLight(voxel, (VoxelVertex) i, Manager, neighbors, ref colorInfo);
                                    if (type.EmitsLight) colorInfo.DynamicColor = 255;
                                    Data.SetColor(x, y, z, (VoxelVertex) i,
                                        new Color(colorInfo.SunColor, colorInfo.AmbientColor, colorInfo.DynamicColor));
                                }
                            }
                            else
                            {
                                byte sunColor =
                                    Data.SunColors[
                                        Data.IndexAt((int) voxel.GridPosition.X, (int) voxel.GridPosition.Y,
                                            (int) voxel.GridPosition.Z)];
                                for (int i = 0; i < 8; i++)
                                {
                                    int vertIndex = Data.VertIndex(x, y, z, (VoxelVertex) i);
                                    if (!indexesToUpdate.Contains(vertIndex))
                                    {
                                        indexesToUpdate.Add(vertIndex);
                                        Data.SetColor(x, y, z, (VoxelVertex) i, new Color(sunColor, 128, 0));
                                    }
                                }
                            }
                        }
                        else if (voxel.IsVisible)
                        {
                            Data.SunColors[
                                Data.IndexAt((int) voxel.GridPosition.X, (int) voxel.GridPosition.Y,
                                    (int) voxel.GridPosition.Z)] = 0;
                            for (int i = 0; i < 8; i++)
                            {
                                int vertIndex = Data.VertIndex(x, y, z, (VoxelVertex) i);
                                if (indexesToUpdate.Contains(vertIndex)) continue;
                                indexesToUpdate.Add(vertIndex);
                                Data.SetColor(x, y, z, (VoxelVertex) i, new Color(0, m_fogOfWar, 0));
                            }
                        }
                    }
                }
            }

            LightingCalculated = true;
        }


        /// <summary>
        /// Calculates thhe sunlight.
        /// </summary>
        public void CalculateGlobalLight()
        {
            if (GameSettings.Default.CalculateSunlight)
            {
                UpdateSunlight(255);
            }
            else
            {
                ResetSunlight(255);
            }
        }

        #endregion

        #region visibility

        /// <summary>
        /// Gets the height of the first filled voxel beneath the specified coordinates.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate</param>
        /// <param name="z">The z coordinate</param>
        /// <returns>The height of the first filled voxel beneath this one if it exists, or -1 otherwise.</returns>
        public int GetFilledVoxelGridHeightAt(int x, int y, int z)
        {
            const int invalid = -1;

            if (!IsCellValid(x, y, z))
            {
                return invalid;
            }
            for (int h = y; h > 0; h--)
            {
                if (Data.Types[Data.IndexAt(x, h, z)] != 0)
                {
                    return h + 1;
                }
            }

            return invalid;
        }


        /// <summary>
        /// Gets the height of the first filled voxel beneath the specified coordinates, considering any cell with water "filled".
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate</param>
        /// <param name="z">The z coordinate</param>
        /// <returns>The height of the first filled voxel beneath this one if it exists, or -1 otherwise.</returns>
        public int GetFilledHeightOrWaterAt(int x, int y, int z)
        {
            if (!IsCellValid(x, y, z))
            {
                return -1;
            }
            for (int h = y; h >= 0; h--)
            {
                if (Data.Types[Data.IndexAt(x, h, z)] != 0 || Data.Water[Data.IndexAt(x, h, z)].WaterLevel > 1)
                {
                    return h + 1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns whether or not this chunk was affected by the chunk slice changing.
        /// </summary>
        /// <returns>True if any filled voxel exists above the viewing slice. False otherwise.</returns>
        public bool NeedsViewingLevelChange()
        {
            float level = Manager.ChunkData.MaxViewingLevel;

            int mx = SizeX;
            int my = SizeY;
            int mz = SizeZ;
            Voxel voxel = MakeVoxel(0, 0, 0);
            for (int y = 0; y < my; y++)
            {
                for (int x = 0; x < mx; x++)
                {
                    for (int z = 0; z < mz; z++)
                    {
                        float test = 0.0f;

                        switch (Manager.ChunkData.Slice)
                        {
                            case ChunkManager.SliceMode.X:
                                test = x + Origin.X;
                                break;
                            case ChunkManager.SliceMode.Y:
                                test = y + Origin.Y;
                                break;
                            case ChunkManager.SliceMode.Z:
                                test = z + Origin.Z;
                                break;
                        }

                        voxel.GridPosition = new Vector3(x, y, z);
                        if (test > level && !voxel.IsEmpty)
                        {
                            return true;
                        }
                    }
                }
            }


            return false;
        }

        #endregion

        #region neighbors

        /// <summary>
        /// Gets a list of neighboring voxels of a vertex.
        /// </summary>
        /// <param name="vertex">The vertex of a voxel.</param>
        /// <param name="v">The voxel to check.</param>
        /// <param name="toReturn">A list of voxels adjacent to that vertex.</param>
        public void GetNeighborsVertex(VoxelVertex vertex, Voxel v, List<Voxel> toReturn)
        {
            Vector3 grid = v.GridPosition;
            GetNeighborsVertex(vertex, (int) grid.X, (int) grid.Y, (int) grid.Z, toReturn);
        }

        /// <summary>
        /// Determines whether the specified x, y and z grid coordinate is inside
        /// the chunk, or if it lies on the border.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate</param>
        /// <param name="z">The z coordinate</param>
        /// <returns>
        ///   <c>true</c> if the specified coordinate is not on the edge of the chunk; otherwise, <c>false</c>.
        /// </returns>
        public bool IsInterior(int x, int y, int z)
        {
            return (x != 0 && y != 0 && z != 0 && x != SizeX - 1 && y != SizeY - 1 && z != SizeZ - 1);
        }

        /// <summary>
        /// Computes a binary hash of the voxel at [x, y, z] telling us which transition texture
        /// to use for the top of the voxel.
        /// </summary>
        /// <param name="x">The x coordinate of the voxel.</param>
        /// <param name="y">The y coordinate of the voxel.</param>
        /// <param name="z">The z coordinate of the voxel.</param>
        /// <param name="neighbors">The neighbors of the voxel (preallocated)</param>
        /// <returns>A transition texture for the top of the voxel.</returns>
        public TransitionTexture ComputeTransitionValue(int x, int y, int z, Voxel[] neighbors)
        {
            VoxelType type = VoxelLibrary.GetVoxelType(Data.Types[Data.IndexAt(x, y, z)]);
            Get2DManhattanNeighbors(neighbors, x, y, z);

            int value = 0;
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != null && !neighbors[i].IsEmpty && neighbors[i].Type == type)
                {
                    value += manhattan2DMultipliers[i];
                }
            }
            var toReturn = (TransitionTexture) value;
            return toReturn;
        }

        /// <summary>
        /// Gets the neighbors of a voxel that touch its x or z faces.
        /// </summary>
        /// <param name="neighbors">The neighbors of the voxel (preallocated). Fills with null if no neighbor exists.</param>
        /// <param name="x">The x coordinate of the voxel.</param>
        /// <param name="y">The y coordinate of the voxel</param>
        /// <param name="z">The z coordinate of the voxel.</param>
        public void Get2DManhattanNeighbors(Voxel[] neighbors, int x, int y, int z)
        {
            List<Vector3> succ = Manhattan2DSuccessors;
            int count = succ.Count;
            bool isInterior = IsInterior(x, y, z);
            for (int i = 0; i < count; i++)
            {
                Vector3 successor = succ[i];
                int nx = (int) successor.X + x;
                int ny = (int) successor.Y + y;
                int nz = (int) successor.Z + z;

                if (isInterior || IsCellValid(nx, ny, nz))
                {
                    if (neighbors[i] == null)
                    {
                        neighbors[i] = MakeVoxel(nx, ny, nz);
                    }
                    else
                    {
                        neighbors[i].Chunk = this;
                        neighbors[i].GridPosition = new Vector3(nx, ny, nz);
                    }
                }
                else
                {
                    Point3 chunkID = ID;
                    if (nx >= SizeZ)
                    {
                        chunkID.X += 1;
                        nx = 0;
                    }
                    else if (nx < 0)
                    {
                        chunkID.X -= 1;
                        nx = SizeX - 1;
                    }

                    if (ny >= SizeY)
                    {
                        chunkID.Y += 1;
                        ny = 0;
                    }
                    else if (ny < 0)
                    {
                        chunkID.Y -= 1;
                        ny = SizeY - 1;
                    }

                    if (nz >= SizeZ)
                    {
                        chunkID.Z += 1;
                        nz = 0;
                    }
                    else if (nz < 0)
                    {
                        chunkID.Z -= 1;
                        nz = SizeZ - 1;
                    }


                    if (!Manager.ChunkData.ChunkMap.ContainsKey(chunkID))
                    {
                        continue;
                    }

                    VoxelChunk chunk = Manager.ChunkData.ChunkMap[chunkID];

                    if (neighbors[i] == null)
                    {
                        neighbors[i] = chunk.MakeVoxel(nx, ny, nz);
                    }
                    else
                    {
                        neighbors[i].Chunk = chunk;
                        neighbors[i].GridPosition = new Vector3(nx, ny, nz);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the neighbors of a voxel given an arbitrary successors list.
        /// </summary>
        /// <param name="succ">The successors of a voxel (as incremental directions)</param>
        /// <param name="x">The x coordinate of the voxel.</param>
        /// <param name="y">The y coordinate of the voxel.</param>
        /// <param name="z">The z coordinate of the voxel.</param>
        /// <param name="toReturn">A list of voxels adjacent to the given voxel given the successor list.</param>
        public void GetNeighborsSuccessors(List<Vector3> succ, int x, int y, int z, List<Voxel> toReturn)
        {
            if (succ.Count != toReturn.Count)
            {
                toReturn.Clear();
                for (int i = 0; i < succ.Count; i++)
                {
                    toReturn.Add(MakeVoxel(0, 0, 0));
                }
            }

            bool isInterior = IsInterior(x, y, z);
            int count = succ.Count;
            for (int i = 0; i < count; i++)
            {
                Vector3 successor = succ[i];
                int nx = (int) successor.X + x;
                int ny = (int) successor.Y + y;
                int nz = (int) successor.Z + z;

                if (isInterior || IsCellValid(nx, ny, nz))
                {
                    toReturn[i].Chunk = this;
                    toReturn[i].GridPosition = new Vector3(nx, ny, nz);
                }
                else
                {
                    Point3 chunkID = ID;
                    if (nx >= SizeZ)
                    {
                        chunkID.X += 1;
                        nx = 0;
                    }
                    else if (nx < 0)
                    {
                        chunkID.X -= 1;
                        nx = SizeX - 1;
                    }

                    if (ny >= SizeY)
                    {
                        chunkID.Y += 1;
                        ny = 0;
                    }
                    else if (ny < 0)
                    {
                        chunkID.Y -= 1;
                        ny = SizeY - 1;
                    }

                    if (nz >= SizeZ)
                    {
                        chunkID.Z += 1;
                        nz = 0;
                    }
                    else if (nz < 0)
                    {
                        chunkID.Z -= 1;
                        nz = SizeZ - 1;
                    }


                    if (!Manager.ChunkData.ChunkMap.ContainsKey(chunkID))
                    {
                        continue;
                    }

                    VoxelChunk chunk = Manager.ChunkData.ChunkMap[chunkID];
                    toReturn[i].Chunk = chunk;
                    toReturn[i].GridPosition = new Vector3(nx, ny, nz);
                }
            }
        }

        /// <summary>
        /// Gets the neighbors of a vertex.
        /// </summary>
        /// <param name="vertex">The vertex to get neighbors of.</param>
        /// <param name="x">The x coordinate of the voxel.</param>
        /// <param name="y">The y coordinate of the voxel.</param>
        /// <param name="z">The z coordinate of the voxel.</param>
        /// <param name="toReturn">List of voxels adjacent to the vertex of the voxel.</param>
        public void GetNeighborsVertex(VoxelVertex vertex, int x, int y, int z, List<Voxel> toReturn)
        {
            GetNeighborsSuccessors(VertexSuccessors[vertex], x, y, z, toReturn);
        }

        /// <summary>
        /// Gets the neighbors a vertex that also share its Y coordinate.
        /// </summary>
        /// <param name="vertex">The vertex to get neighbors of.</param>
        /// <param name="x">The x coordinate of the voxel.</param>
        /// <param name="y">The y coordinate of the voxel.</param>
        /// <param name="z">The z coordinate of the voxel.</param>
        /// <param name="toReturn">List of voxels adjacent to the vertex of the voxel..</param>
        public void GetNeighborsVertexDiag(VoxelVertex vertex, int x, int y, int z, List<Voxel> toReturn)
        {
            GetNeighborsSuccessors(VertexSuccessorsDiag[vertex], x, y, z, toReturn);
        }

        /// <summary>
        /// Gets the 26 neighbors that touch the given voxel.
        /// </summary>
        /// <param name="v">The voxel to get neighbors of.</param>
        /// <returns>All the neighbors that touch this voxel.</returns>
        public List<Voxel> GetNeighborsEuclidean(Voxel v)
        {
            Vector3 gridCoord = v.GridPosition;
            return GetNeighborsEuclidean((int) gridCoord.X, (int) gridCoord.Y, (int) gridCoord.Z);
        }
        /// <summary>
        /// Gets the 26 neighbors that touch the given voxel.
        /// </summary>
        /// <param name="x">The x coordinate of the voxel.</param>
        /// <param name="y">The y coordinate of the voxel.</param>
        /// <param name="z">The z coordinate of the voxel.</param>
        /// <returns>All the neighbors that touch this voxel.</returns>
        public List<Voxel> GetNeighborsEuclidean(int x, int y, int z)
        {
            var toReturn = new List<Voxel>();
            bool isInterior = (x > 0 && y > 0 && z > 0 && x < SizeX - 1 && y < SizeY - 1 && z < SizeZ - 1);
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    for (int dz = -1; dz < 2; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0)
                        {
                            continue;
                        }

                        int nx = dx + x;
                        int ny = dy + y;
                        int nz = dz + z;

                        if (isInterior || IsCellValid(nx, ny, nz))
                        {
                            toReturn.Add(MakeVoxel(nx, ny, nz));
                        }
                        else
                        {
                            var otherVox = new Voxel();
                            if (Manager.ChunkData.GetVoxel(this, new Vector3(nx, ny, nz) + Origin, ref otherVox))
                            {
                                toReturn.Add(otherVox);
                            }
                        }
                    }
                }
            }
            return toReturn;
        }

        /// <summary>
        /// Creates a fixed number of voxels.
        /// </summary>
        /// <param name="num">The number of voxels to create.</param>
        /// <returns>A list containing a number of voxels assigned to this chunk.</returns>
        public List<Voxel> AllocateVoxels(int num)
        {
            var toReturn = new List<Voxel>();
            for (int i = 0; i < num; i++)
            {
                toReturn.Add(MakeVoxel(0, 0, 0));
            }

            return toReturn;
        }

        /// <summary>
        /// Gets the 6 neighbors that touch the faces of this voxel.
        /// </summary>
        /// <param name="x">The x coordinate of the voxel..</param>
        /// <param name="y">The y coordinate of hte voxel.</param>
        /// <param name="z">The z coordinate of the voxel</param>
        /// <param name="neighbors">The neighbors.</param>
        public void GetNeighborsManhattan(int x, int y, int z, List<Voxel> neighbors)
        {
            GetNeighborsSuccessors(ManhattanSuccessors, x, y, z, neighbors);
        }

        /// <summary>
        /// Notify the chunk that it should completely rebuild its vertex buffer,
        /// liquid primitives, ramps, and other properties.
        /// </summary>
        /// <param name="neighbors">if set to <c>true</c> also rebuild the neighboring chunks.</param>
        public void NotifyTotalRebuild(bool neighbors)
        {
            ShouldRebuild = true;
            ShouldRecalculateLighting = true;
            ShouldRebuildWater = true;
            ReconstructRamps = true;

            if (!neighbors) return;
            foreach (VoxelChunk chunk in Neighbors.Values)
            {
                chunk.ShouldRebuild = true;
                chunk.ShouldRecalculateLighting = true;
                chunk.ShouldRebuildWater = true;
            }
        }

        /// <summary>
        /// Determines whether the given voxel has no filled neighbors.
        /// </summary>
        /// <param name="v">The voxel to check.</param>
        /// <returns>
        ///   <c>true</c> if the voxel has no filled neighbors; otherwise, <c>false</c>.
        /// </returns>
        public bool HasNoNeighbors(Voxel v)
        {
            Vector3 pos = v.Position;
            Vector3 gridPos = v.GridPosition;

            if (!Manager.ChunkData.ChunkMap.ContainsKey(v.ChunkID))
            {
                return false;
            }

            VoxelChunk chunk = Manager.ChunkData.ChunkMap[v.ChunkID];
            var gridPoint = new Point3(gridPos);

            bool interior = Voxel.IsInteriorPoint(gridPoint, chunk);

            if (interior)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3 neighbor = ManhattanSuccessors[i];
                    int index = Data.IndexAt(gridPoint.X + (int) neighbor.X, gridPoint.Y + (int) neighbor.Y,
                        gridPoint.Z + (int) neighbor.Z);

                    if (Data.Types[index] != 0)
                    {
                        return false;
                    }
                }
            }
            else
            {
                var atPos = new Voxel();
                for (int i = 0; i < 6; i++)
                {
                    Vector3 neighbor = ManhattanSuccessors[i];

                    if (!IsGridPositionValid(neighbor + gridPos))
                    {
                        if (Manager.ChunkData.GetNonNullVoxelAtWorldLocation(pos + neighbor, ref atPos) &&
                            !atPos.IsEmpty)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        int index = Data.IndexAt(gridPoint.X + (int) neighbor.X, gridPoint.Y + (int) neighbor.Y,
                            gridPoint.Z + (int) neighbor.Z);

                        if (Data.Types[index] != 0)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified voxel is completely surrounded by other filled voxels.
        /// </summary>
        /// <param name="v">The voxel to check</param>
        /// <param name="ramps">if set to <c>true</c> consider any ramping voxel to be "empty". This is for lighting calculations.</param>
        /// <returns>
        ///   <c>true</c> if the specified voxel is completely surrounded; otherwise, <c>false</c>.
        /// </returns>
        public bool IsCompletelySurrounded(Voxel v, bool ramps)
        {
            if (!Manager.ChunkData.ChunkMap.ContainsKey(v.ChunkID))
            {
                return false;
            }

            Vector3 pos = v.Position;
            VoxelChunk chunk = Manager.ChunkData.ChunkMap[v.ChunkID];
            var gridPoint = new Point3(v.GridPosition);
            bool interior = Voxel.IsInteriorPoint(gridPoint, chunk);


            if (interior)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3 neighbor = ManhattanSuccessors[i];
                    int neighborIndex = Data.IndexAt(gridPoint.X + (int) neighbor.X, gridPoint.Y + (int) neighbor.Y,
                        gridPoint.Z + (int) neighbor.Z);
                    if (Data.Types[neighborIndex] == 0 || (ramps && Data.RampTypes[neighborIndex] != RampType.None))
                    {
                        return false;
                    }
                }
            }
            else
            {
                var atPos = new Voxel();
                for (int i = 0; i < 6; i++)
                {
                    Vector3 neighbor = ManhattanSuccessors[i];

                    if (!Manager.ChunkData.GetNonNullVoxelAtWorldLocationCheckFirst(chunk, pos + neighbor, ref atPos) ||
                        atPos.IsEmpty || (ramps && atPos.RampType != RampType.None))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified voxel is completely surrounded by other filled voxels.
        /// </summary>
        /// <param name="v">The voxel.</param>
        /// <returns>
        ///   <c>true</c> if the voxel is completely surrounded; otherwise, <c>false</c>.
        /// </returns>
        public bool IsCompletelySurrounded(Voxel v)
        {
            return IsCompletelySurrounded(v, false);
        }

        /// <summary>
        /// Gets the 6 neighbors that are coincident to the voxel's faces.
        /// </summary>
        /// <param name="v">The voxel to check.</param>
        /// <param name="toReturn">A list of voxels touching the voxel's faces.</param>
        public void GetNeighborsManhattan(Voxel v, List<Voxel> toReturn)
        {
            GetNeighborsManhattan((int) v.GridPosition.X, (int) v.GridPosition.Y, (int) v.GridPosition.Z, toReturn);
        }

        /// <summary>
        /// Resets the state of water cells in the chunk.
        /// </summary>
        public void ResetWaterBuffer()
        {
            int numVoxels = sizeX*sizeY*sizeZ;

            for (int i = 0; i < numVoxels; i++)
            {
                Data.Water[i].HasChanged = false;
                Data.Water[i].IsFalling = false;
            }
        }

        /// <summary>
        /// Gets the world coordinates of a specified chunk-relative grid coordinate.
        /// </summary>
        /// <param name="gridCoord">The grid coordinate relative to the chunk.</param>
        /// <returns>A world-relative coordinate.</returns>
        public Vector3 GridToWorld(Vector3 gridCoord)
        {
            return gridCoord + Origin;
        }

        /// <summary>
        /// Gets the voxels intersecting the given bounding box.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <returns>A list of voxels intersecting that box.</returns>
        public List<Voxel> GetVoxelsIntersecting(BoundingBox box)
        {
            if (!GetBoundingBox().Intersects(box) && GetBoundingBox().Contains(box) != ContainmentType.Disjoint)
            {
                return new List<Voxel>();
            }
            BoundingBox myBox = GetBoundingBox();
            var toReturn = new List<Voxel>();
            for (float x = Math.Max(box.Min.X, myBox.Min.X); x < Math.Min(box.Max.X, myBox.Max.X); x++)
            {
                for (float y = Math.Max(box.Min.Y, myBox.Min.Y); y < Math.Min(box.Max.Y, myBox.Max.Y); y++)
                {
                    for (float z = Math.Max(box.Min.Z, myBox.Min.Z); z < Math.Min(box.Max.Z, myBox.Max.Z); z++)
                    {
                        Vector3 grid = new Vector3(x, y, z) - Origin;
                        Voxel vox = MakeVoxel((int) grid.X, (int) grid.Y, (int) grid.Z);
                        toReturn.Add(vox);
                    }
                }
            }

            return toReturn;
        }

        #endregion neighbors

        /// <summary>
        /// Colors are lookups into a ramp (0-255) for ambient light, dynamic light and sunlight.
        /// </summary>
        public struct VertexColorInfo
        {
            public int AmbientColor;
            public int DynamicColor;
            public int SunColor;
        }

        /// <summary>
        /// VoxelData contains the raw data associated with the VoxelChunk
        /// </summary>
        public class VoxelData
        {
            /// <summary>
            /// The health of each voxel (0-255)
            /// </summary>
            public byte[] Health;
            /// <summary>
            /// Whether or not each voxel is explored or is obscured by fog of war.
            /// </summary>
            public bool[] IsExplored;
            /// <summary>
            /// The state of ramps of each voxel (all, forward, backward, etc.)
            /// </summary>
            public RampType[] RampTypes;
            /// <summary>
            /// The number of voxels in X.
            /// </summary>
            public int SizeX;
            /// <summary>
            /// The number of voxels in Y.
            /// </summary>
            public int SizeY;
            /// <summary>
            /// The number of voxels in Z.
            /// </summary>
            public int SizeZ;
            /// <summary>
            /// The intensity of sunlight at each voxel.
            /// </summary>
            public byte[] SunColors;
            /// <summary>
            /// The type index of each voxel.
            /// </summary>
            public byte[] Types;
            /// <summary>
            /// For each vertex of each voxel, a color (sunlight, ambient, dynamic)
            /// </summary>
            public Color[] VertexColors;
            /// <summary>
            /// The water at each voxel.
            /// </summary>
            public WaterCell[] Water;

            /// <summary>
            /// Sets the color of a vertex of a specified voxel to a color.
            /// </summary>
            /// <param name="x">The x coord of the voxel.</param>
            /// <param name="y">The y coord of the voxel.</param>
            /// <param name="z">The z coord of the voxel.</param>
            /// <param name="v">The vertex.</param>
            /// <param name="color">The color to set the vertex to.</param>
            public void SetColor(int x, int y, int z, VoxelVertex v, Color color)
            {
                VertexColors[VertIndex(x, y, z, v)] = color;
            }

            /// <summary>
            /// Gets the color of the specified voxel vertex.
            /// </summary>
            /// <param name="x">The x coordinate of the voxel.</param>
            /// <param name="y">The y coordinate of the voxel.</param>
            /// <param name="z">The z coordinate of the voxel.</param>
            /// <param name="v">The vertex.</param>
            /// <returns>The color at that vertex.</returns>
            public Color GetColor(int x, int y, int z, VoxelVertex v)
            {
                return VertexColors[VertIndex(x, y, z, v)];
            }

            /// <summary>
            /// Gets the linear index of the voxel vertex.
            /// </summary>
            /// <param name="x">The x coordinate of the voxel.</param>
            /// <param name="y">The y coordinate of the voxel.</param>
            /// <param name="z">The z coordinate of the voxel.</param>
            /// <param name="v">The vertex.</param>
            /// <returns>A linear coordinate of the verex.</returns>
            public int VertIndex(int x, int y, int z, VoxelVertex v)
            {
                int cornerX = x;
                int cornerY = y;
                int cornerZ = z;
                switch (v)
                {
                        // -x, -y, -z
                    case VoxelVertex.BackBottomLeft:
                        cornerX += 0;
                        cornerY += 0;
                        cornerZ += 0;
                        break;
                        // +x, -y, -z
                    case VoxelVertex.BackBottomRight:
                        cornerX += 1;
                        cornerY += 0;
                        cornerZ += 0;
                        break;
                        // -x, +y, -z
                    case VoxelVertex.BackTopLeft:
                        cornerX += 0;
                        cornerY += 1;
                        cornerZ += 0;
                        break;
                        // +x, +y, -z
                    case VoxelVertex.BackTopRight:
                        cornerX += 1;
                        cornerY += 1;
                        cornerZ += 0;
                        break;
                        // -x, -y, +z
                    case VoxelVertex.FrontBottomLeft:
                        cornerX += 0;
                        cornerY += 0;
                        cornerZ += 1;
                        break;
                        // +x, -y, +z
                    case VoxelVertex.FrontBottomRight:
                        cornerX += 1;
                        cornerY += 0;
                        cornerZ += 1;
                        break;
                        // -x, +y, +z
                    case VoxelVertex.FrontTopLeft:
                        cornerX += 0;
                        cornerY += 1;
                        cornerZ += 1;
                        break;
                        // +x, +y, +z
                    case VoxelVertex.FrontTopRight:
                        cornerX += 1;
                        cornerY += 1;
                        cornerZ += 1;
                        break;
                }
                return CornerIndexAt(cornerX, cornerY, cornerZ);
            }

            /// <summary>
            /// Superimpose a sub-grid onto the voxel grid such that each vertex
            /// is in the center of a grid cell. This returns the linear coordinate 
            /// of the x, y, z coordinate of that grid cell.
            /// </summary>
            /// <param name="x">The x coordinate of the vertex.</param>
            /// <param name="y">The y coordinate of the vertex.</param>
            /// <param name="z">The z coordinate of the vertex.</param>
            /// <returns>The linear index of the vertex at that coordinate.</returns>
            public int CornerIndexAt(int x, int y, int z)
            {
                return (z*(SizeY + 1) + y)*(SizeX + 1) + x;
            }

            /// <summary>
            /// Gets the linear index of a voxel at the specified coordinate.
            /// </summary>
            /// <param name="x">The x coordinate of the voxel.</param>
            /// <param name="y">The y coordinate of the voxel.</param>
            /// <param name="z">The z coordinate of the voxel.</param>
            /// <returns>A linear index of the voxel at that coordinate.</returns>
            public int IndexAt(int x, int y, int z)
            {
                return (z*SizeY + y)*SizeX + x;
            }

            /// <summary>
            /// Inverse of IndexAt
            /// </summary>
            /// <param name="idx">The linear index.</param>
            /// <returns>A grid position of the voxel at the given index.</returns>
            public Vector3 CoordsAt(int idx)
            {
                int x = idx%(SizeX);
                idx /= (SizeX);
                int y = idx%(SizeY);
                idx /= (SizeY);
                int z = idx;
                return new Vector3(x, y, z);
            }
        }
    }
}
