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
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using System.Collections.Concurrent;
using Color = Microsoft.Xna.Framework.Color;
using System.Diagnostics;

namespace DwarfCorp
{
    /// <summary>
    /// A 3D grid of voxels, water, and light.
    /// </summary>
    public class VoxelChunk : IBoundedObject
    {
        public delegate void VoxelDestroyed(Point3 voxelID);
        public event VoxelDestroyed OnVoxelDestroyed;

        public delegate void VoxelExplored(Point3 voxelID);

        public event VoxelExplored OnVoxelExplored;

        public class VoxelData
        {
            public bool[] IsExplored;
            public byte[] Health;
            public byte[] Types;
            public byte[] SunColors;
            public WaterCell[] Water;
            public int SizeX;
            public int SizeY;
            public int SizeZ;
            public RampType[] RampTypes;
            //public Color[] VertexColors;
            /*
            public void SetColor(int x, int y, int z, VoxelVertex v, Color color)
            {
                VertexColors[VertIndex(x, y, z, v)] = color;
            }

            public Color GetColor(int x, int y, int z, VoxelVertex v)
            {
                return VertexColors[VertIndex(x, y, z, v)];
            }
             */

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

            public int CornerIndexAt(int x, int y, int z)
            {
                return (z * (SizeY + 1) + y) * (SizeX + 1) + x;
            }

            public int IndexAt(int x, int y, int z)
            {
                return (z * SizeY + y) * SizeX + x;
            }

            public int IndexAt(Point3 point)
            {
                return IndexAt(point.X, point.Y, point.Z);
            }

            public Vector3 CoordsAt(int idx)
            {
                int x = idx % (SizeX);
                idx /= (SizeX);
                int y = idx % (SizeY);
                idx /= (SizeY);
                int z = idx;
                return new Vector3(x, y, z);
            }
        }

        public struct VertexColorInfo
        {
            public int SunColor;
            public int AmbientColor;
            public int DynamicColor;
        }

        public Dictionary<string, List<InstanceData>> Motes { get; set; }
        public VoxelListPrimitive Primitive { get; set; }
        public VoxelListPrimitive NewPrimitive = null;
        public Dictionary<LiquidType, LiquidPrimitive> Liquids { get; set; }
        public bool NewPrimitiveReceived = false;
        public bool NewLiquidReceived = false;


        public VoxelData Data { get; set; }
        public ConcurrentDictionary<Voxel, byte> Springs { get; set; }

        public int SizeX
        {
            get { return sizeX; }
        }

        public int SizeY
        {
            get { return sizeY; }
        }

        public int SizeZ
        {
            get { return sizeZ; }
        }

        public bool IsVisible { get; set; }
        public bool ShouldRebuild { get; set; }
        public bool IsRebuilding { get; set; }
        public Vector3 Origin { get; set; }
        private Vector3 HalfLength { get; set; }
        public bool RenderWireframe { get; set; }
        public ChunkManager Manager { get; set; }
        public bool IsActive { get; set; }
        public bool FirstWaterIter { get; set; }

        public Mutex PrimitiveMutex { get; set; }
        public bool ShouldRecalculateLighting { get; set; }
        public bool ShouldRebuildWater { get; set; }

        public static byte m_fogOfWar = 1;
        public ConcurrentDictionary<Point3, VoxelChunk> Neighbors { get; set; }
        public ConcurrentDictionary<int, VoxelChunk> EuclidianNeighbors { get; set; }
        public List<DynamicLight> DynamicLights { get; set; }


        private static bool staticsInitialized = false;
        private static Vector3[] vertexDeltas = new Vector3[8];
        private static Vector3[] faceDeltas = new Vector3[6];

        public static readonly Dictionary<VoxelVertex, List<Vector3>> VertexSuccessors = new Dictionary<VoxelVertex, List<Vector3>>();
        public static readonly Dictionary<VoxelVertex, List<Vector3>> VertexSuccessorsDiag = new Dictionary<VoxelVertex, List<Vector3>>();
        public static readonly Dictionary<BoxFace, VoxelVertex[]> FaceVertices = new Dictionary<BoxFace, VoxelVertex[]>();
        public static List<Vector3> ManhattanSuccessors;
        public static List<Vector3> Manhattan2DSuccessors;
        public static List<List<Point3>> EuclideanSuccessorsByVoxelPosition;
        private static int[] manhattan2DMultipliers;


        //public static ColorGradient MSunGradient = new ColorGradient(new Color(70, 70, 70), new Color(255, 254, 224), 255);
        //public static ColorGradient MAmbientGradient = new ColorGradient(new Color(50, 50, 50), new Color(255, 255, 255), 255);
        //public static ColorGradient MCaveGradient = new ColorGradient(new Color(8, 12, 17), new Color(41, 54, 76), 255);
        //public static ColorGradient MTorchGradient = null;
        public bool LightingCalculated { get; set; }
        private bool firstRebuild = true;
        private int sizeX = -1;
        private int sizeY = -1;
        private int sizeZ = -1;
        private int tileSize = -1;


        public bool RebuildPending { get; set; }
        public bool RebuildLiquidPending { get; set; }
        public Point3 ID { get; set; }

        public bool ReconstructRamps { get; set; }

        public uint GetID()
        {
            return (uint)ID.GetHashCode();
        }

        #region statics

        public static void InitializeStatics()
        {
            if (staticsInitialized)
            {
                return;
            }

            vertexDeltas[(int)VoxelVertex.BackBottomLeft] = new Vector3(0, 0, 0);
            vertexDeltas[(int)VoxelVertex.BackTopLeft] = new Vector3(0, 1.0f, 0);
            vertexDeltas[(int)VoxelVertex.BackBottomRight] = new Vector3(1.0f, 0, 0);
            vertexDeltas[(int)VoxelVertex.BackTopRight] = new Vector3(1.0f, 1.0f, 0);

            vertexDeltas[(int)VoxelVertex.FrontBottomLeft] = new Vector3(0, 0, 1.0f);
            vertexDeltas[(int)VoxelVertex.FrontTopLeft] = new Vector3(0, 1.0f, 1.0f);
            vertexDeltas[(int)VoxelVertex.FrontBottomRight] = new Vector3(1.0f, 0, 1.0f);
            vertexDeltas[(int)VoxelVertex.FrontTopRight] = new Vector3(1.0f, 1.0f, 1.0f);

            ManhattanSuccessors = new List<Vector3>
            {
                new Vector3(1.0f, 0, 0),
                new Vector3(-1.0f, 0, 0),
                new Vector3(0, -1.0f, 0),
                new Vector3(0, 1.0f, 0),
                new Vector3(0, 0, -1.0f),
                new Vector3(0, 0, 1.0f)
            };

            Manhattan2DSuccessors = new List<Vector3>
            {
                new Vector3(-1.0f, 0, 0),
                new Vector3(1.0f, 0, 0),
                new Vector3(0, 0, -1.0f),
                new Vector3(0, 0, 1.0f)
            };

            manhattan2DMultipliers = new[]
            {
                2,
                8,
                4,
                1
            };



            faceDeltas[(int)BoxFace.Top] = new Vector3(0.5f, 0.0f, 0.5f);
            faceDeltas[(int)BoxFace.Bottom] = new Vector3(0.5f, 1.0f, 0.5f);
            faceDeltas[(int)BoxFace.Left] = new Vector3(1.0f, 0.5f, 0.5f);
            faceDeltas[(int)BoxFace.Right] = new Vector3(0.0f, 0.5f, 0.5f);
            faceDeltas[(int)BoxFace.Front] = new Vector3(0.5f, 0.5f, 0.0f);
            faceDeltas[(int)BoxFace.Back] = new Vector3(0.5f, 0.5f, 1.0f);


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

            for (int i = 0; i < 8; i++)
            {
                VoxelVertex vertex = (VoxelVertex)(i);
                List<Vector3> successors = new List<Vector3>();
                List<Vector3> diagSuccessors = new List<Vector3>();
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

                for (int dx = nXLim; dx < xlim; dx++)
                {
                    for (int dy = nYLim; dy < ylim; dy++)
                    {
                        for (int dz = nZLim; dz < zlim; dz++)
                        {
                            if (dx == 0 && dy == 0 && dz == 0)
                            {
                                continue;
                            }
                            else
                            {
                                successors.Add(new Vector3(dx, dy, dz));

                                if ((dx != 0 && dz != 0 && dy == 0))
                                {
                                    diagSuccessors.Add(new Vector3(dx, dy, dz));
                                }
                            }
                        }
                    }
                }

                VertexSuccessors[vertex] = successors;
                VertexSuccessorsDiag[vertex] = diagSuccessors;
            }

            InitializeRelativeNeighborLookup();

            staticsInitialized = true;
        }

        public static void InitializeRelativeNeighborLookup()
        {
            EuclideanSuccessorsByVoxelPosition = new List<List<Point3>>();
            for (int i = 0; i < 27; i++)
            {
                EuclideanSuccessorsByVoxelPosition.Add(new List<Point3>());
            }

            List<Point3> euclidianNeighbors = new List<Point3>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0) continue;
                        euclidianNeighbors.Add(new Point3(x, y, z));
                    }
                }
            }

            int key;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        key = SuccessorToEuclidianLookupKey(x, y, z);
                        List<Point3> neighbors = EuclideanSuccessorsByVoxelPosition[key];

                        for (int i = 0; i < euclidianNeighbors.Count; i++)
                        {
                            Point3 testPoint = euclidianNeighbors[i];
                            if ((testPoint.X == x || testPoint.X == 0) &&
                                (testPoint.Y == y || testPoint.Y == 0) &&
                                (testPoint.Z == z || testPoint.Z == 0))
                                neighbors.Add(testPoint);
                        }
                    }
                }
            }
        }

        #endregion

        public void NotifyExplored(Point3 voxel)
        {
            if (OnVoxelExplored != null)
            {
                OnVoxelExplored.Invoke(voxel);
            }
        }

        public void NotifyDestroyed(Point3 voxel)
        {
            if (OnVoxelDestroyed != null)
            {
                OnVoxelDestroyed.Invoke(voxel);
            }
        }

        public static VoxelData AllocateData(int sx, int sy, int sz)
        {
            int numVoxels = sx * sy * sz;
            VoxelData toReturn = new VoxelData()
            {
                Health = new byte[numVoxels],
                IsExplored = new bool[numVoxels],
                SunColors = new byte[numVoxels],
                Types = new byte[numVoxels],
                Water = new WaterCell[numVoxels],
                RampTypes = new RampType[numVoxels],
                //VertexColors = new Color[(sx + 1) * (sy + 1) * (sz + 1)],
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
            HalfLength = new Vector3((float)this.tileSize / 2.0f, (float)this.tileSize / 2.0f, (float)this.tileSize / 2.0f);
            Primitive = new VoxelListPrimitive();
            RenderWireframe = false;
            Manager = manager;
            IsActive = true;

            InitializeStatics();
            PrimitiveMutex = new Mutex();
            ShouldRecalculateLighting = true;
            Neighbors = new ConcurrentDictionary<Point3, VoxelChunk>();
            EuclidianNeighbors = new ConcurrentDictionary<int, VoxelChunk>();
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


        public static WaterCell[][][] WaterAllocate(int sx, int sy, int sz)
        {
            WaterCell[][][] w = new WaterCell[sx][][];

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

        /*
        public VoxelChunk(Vector3 origin, ChunkManager manager, DestinationVoxel[][][] voxelGrid, Point3 id, int tileSize)
        {
            FirstWaterIter = true;
            Motes = new Dictionary<string, List<InstanceData>>();
            VoxelGrid = voxelGrid;
            sizeX = VoxelGrid.Length;
            sizeY = VoxelGrid[0].Length;
            sizeZ = VoxelGrid[0][0].Length;
            ID = id;
            IsVisible = true;
            this.tileSize = tileSize;
            HalfLength = new Vector3((float) this.tileSize / 2.0f, (float) this.tileSize / 2.0f, (float) this.tileSize / 2.0f);
            Origin = origin;
            Primitive = new VoxelListPrimitive();
            RenderWireframe = false;
            Manager = manager;
            IsActive = true;

            Neighbors = new ConcurrentDictionary<Point3, VoxelChunk>();
            DynamicLights = new List<DynamicLight>();
            Liquids = new Dictionary<LiquidType, LiquidPrimitive>();
            Liquids[LiquidType.Water] = new LiquidPrimitive(LiquidType.Water);
            Liquids[LiquidType.Lava] = new LiquidPrimitive(LiquidType.Lava);

            for(int x = 0; x < voxelGrid.Length; x++)
            {
                for(int y = 0; y < voxelGrid[x].Length; y++)
                {
                    for(int z = 0; z < voxelGrid[x][y].Length; z++)
                    {
                        DestinationVoxel v = voxelGrid[x][y][z];
                        if(!v.IsEmpty)
                        {
                            v.Chunk = this;
                            v.GridPosition = v.Position - Origin;
                        }
                    }
                }
            }

            Water = WaterAllocate(sizeX, sizeY, sizeZ);
            SunColors = ChunkGenerator.Allocate<byte>(sizeX, sizeY, sizeZ);
            DynamicColors = ChunkGenerator.Allocate<byte>(sizeX, sizeY, sizeZ);
            InitializeStatics();
            PrimitiveMutex = new Mutex();
            ShouldRecalculateLighting = true;
            ShouldRebuildWater = true;
            Springs = new ConcurrentDictionary<DestinationVoxel, byte>();
            IsRebuilding = false;
            InitializeWater();
            LightingCalculated = false;
        }
        */


        public static VoxelVertex GetNearestDelta(Vector3 position)
        {
            float bestDist = float.MaxValue;
            VoxelVertex bestKey = VoxelVertex.BackTopRight;
            for (int i = 0; i < 8; i++)
            {
                float dist = (position - vertexDeltas[i]).LengthSquared();
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestKey = (VoxelVertex)(i);
                }
            }


            return bestKey;
        }

        public static BoxFace GetNearestFace(Vector3 position)
        {
            float bestDist = 10000000;
            BoxFace bestKey = BoxFace.Top;
            for (int i = 0; i < 6; i++)
            {
                float dist = (position - faceDeltas[i]).LengthSquared();
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestKey = (BoxFace)(i);
                }
            }


            return bestKey;
        }

        private BoundingBox m_boundingBox;
        private bool m_boundingBoxCreated = false;

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

        private BoundingSphere m_boundingSphere;
        private bool m_boundingSphereCreated = false;

        public BoundingSphere GetBoundingSphere()
        {
            if (!m_boundingSphereCreated)
            {
                float m = Math.Max(Math.Max(sizeX, sizeY), sizeZ) * 0.5f;
                m_boundingSphere = new BoundingSphere(Origin + new Vector3(sizeX, sizeY, sizeZ) * 0.5f, (float)Math.Sqrt(3 * m * m));
                m_boundingSphereCreated = true;
            }

            return m_boundingSphere;
        }

        public void Update(DwarfTime t)
        {
            PrimitiveMutex.WaitOne();
            if (NewPrimitiveReceived)
            {
                Primitive = NewPrimitive;
                NewPrimitive = null;
                NewPrimitiveReceived = false;
            }
            PrimitiveMutex.ReleaseMutex();
        }

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

        public void RebuildLiquids()
        {
            List<LiquidPrimitive> toInit = new List<LiquidPrimitive>();

            foreach (KeyValuePair<LiquidType, LiquidPrimitive> primitive in Liquids)
            {
                toInit.Add(primitive.Value);
            }
            LiquidPrimitive.InitializePrimativesFromChunk(this, toInit);
            ShouldRebuildWater = false;
        }

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

        public static Perlin MoteNoise = new Perlin(0);
        public static Perlin MoteScaleNoise = new Perlin(250);

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

        private Vector3 ClampVector(Vector3 v, float a)
        {
            v.X = Clamp(v.X, a);
            v.Y = Clamp(v.Y, a);
            v.Z = Clamp(v.Z, a);
            return v;
        }

        public Voxel MakeVoxel(int x, int y, int z)
        {
            return new Voxel(new Point3(x, y, z), this);
        }

        public void BuildGrassMotes(Overworld.Biome biome)
        {
            BiomeData biomeData = BiomeLibrary.Biomes[biome];

            string grassType = biomeData.GrassLayer.VoxelType;

            for (int i = 0; i < biomeData.Motes.Count; i++)
            {
                List<Vector3> grassPositions = new List<Vector3>();
                List<Color> grassColors = new List<Color>();
                List<float> grassScales = new List<float>();
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

                            float value = MoteNoise.Noise(v.Position.X * moteData.RegionScale, v.Position.Y * moteData.RegionScale, v.Position.Z * moteData.RegionScale);
                            float s = MoteScaleNoise.Noise(v.Position.X * moteData.RegionScale, v.Position.Y * moteData.RegionScale, v.Position.Z * moteData.RegionScale) * moteData.MoteScale;

                            if (!(Math.Abs(value) > moteData.SpawnThreshold))
                            {
                                continue;
                            }

                            Vector3 smallNoise = ClampVector(VertexNoise.GetRandomNoiseVector(v.Position * moteData.RegionScale * 20.0f) * 20.0f, 0.4f);
                            smallNoise.Y = 0.0f;
                            grassPositions.Add(v.Position + new Vector3(0.5f, 1.0f + s * 0.5f + vOffset, 0.5f) + smallNoise);
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
                    grassColors, grassScales, Manager.Components, Manager.Content, Manager.Graphics, Motes[moteData.Name], moteData.Asset, moteData.Name);
            }
        }

        public void UpdateRamps()
        {
            if (ReconstructRamps || firstRebuild)
            {
                //VoxelListPrimitive.UpdateRamps(this);
                VoxelListPrimitive.UpdateCornerRamps(this);
                ReconstructRamps = false;
            }
        }

        public void BuildGrassMotes()
        {
            Vector2 v = new Vector2(Origin.X, Origin.Z) / Manager.World.WorldScale;

            Overworld.Biome biome = Overworld.Map[(int)MathFunctions.Clamp(v.X, 0, Overworld.Map.GetLength(0) - 1), (int)MathFunctions.Clamp(v.Y, 0, Overworld.Map.GetLength(1) - 1)].Biome;
            BuildGrassMotes(biome);
        }

        public void Rebuild(GraphicsDevice g)
        {
            //Drawer3D.DrawBox(GetBoundingBox(), Color.White, 0.1f);

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

        public void BuildPrimitive(GraphicsDevice g)
        {
            //Primitive.InitializeFromChunk(this, g);
            VoxelListPrimitive primitive = new VoxelListPrimitive();
            primitive.InitializeFromChunk(this, g);
        }

        public void NotifyChangedComponents()
        {
            HashSet<IBoundedObject> componentsInside = new HashSet<IBoundedObject>();
            Manager.World.CollisionManager.GetObjectsIntersecting(GetBoundingBox(), componentsInside, CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

            Message changedMessage = new Message(Message.MessageType.OnChunkModified, "Chunk Modified");

            foreach (IBoundedObject c in componentsInside)
            {
                if (c is GameComponent)
                {
                    ((GameComponent)c).ReceiveMessageRecursive(changedMessage);
                }
            }
        }

        public void Destroy(GraphicsDevice device)
        {
            if (Primitive != null)
            {
                Primitive.ResetBuffer(device);
            }

            if (Motes != null)
            {
                foreach (KeyValuePair<string, List<InstanceData>> mote in Motes)
                {
                    foreach (InstanceData mote2 in mote.Value)
                    {
                        EntityFactory.InstanceManager.RemoveInstance(mote.Key, mote2);
                    }
                }
            }
        }

        #region transformations

        public Vector3 WorldToGrid(Vector3 worldLocation)
        {
            Vector3 grid = (worldLocation - Origin);
            return grid;
        }

        public Point3 WorldToGridPoint3(Vector3 worldLocation)
        {
            return new Point3(worldLocation - Origin);
        }


        public bool GetVoxelAtWorldLocation(Vector3 worldLocation, ref Voxel voxel)
        {
            Vector3 grid = WorldToGrid(worldLocation);

            bool valid = IsCellValid(MathFunctions.FloorInt(grid.X), MathFunctions.FloorInt(grid.Y), MathFunctions.FloorInt(grid.Z));

            if (!valid) return false;

            voxel.Chunk = this;
            voxel.GridPosition = new Vector3(MathFunctions.FloorInt(grid.X), MathFunctions.FloorInt(grid.Y), MathFunctions.FloorInt(grid.Z));
            return true;
        }

        // Same as GetVoxelAtWorldLocation except skips the part where it has to check to see if it is in range.
        // If you don't know for sure it is valid use the other function.
        public bool GetVoxelAtValidWorldLocation(Vector3 worldLocation, ref Voxel voxel)
        {
            Vector3 grid = WorldToGrid(worldLocation);

            voxel.Chunk = this;
            voxel.GridPosition = new Vector3(MathFunctions.FloorInt(grid.X), MathFunctions.FloorInt(grid.Y), MathFunctions.FloorInt(grid.Z));
            return true;
        }

        public bool IsGridPositionValid(Vector3 grid)
        {
            return IsCellValid((int)grid.X, (int)grid.Y, (int)grid.Z);
        }

        public bool IsWorldLocationValid(Vector3 worldLocation)
        {
            Vector3 grid = WorldToGrid(worldLocation);

            return IsCellValid((int)grid.X, (int)grid.Y, (int)grid.Z);
        }

        public bool IsCellValid(int x, int y, int z)
        {
            return x >= 0 && y >= 0 && z >= 0 && x < SizeX && y < SizeY && z < SizeZ;
        }

        public bool IsCellValid(Point3 point)
        {
            return IsCellValid(point.X, point.Y, point.Z);
        }

        #endregion transformations

        #region lighting





        public byte GetIntensity(DynamicLight light, byte lightIntensity, Voxel voxel)
        {
            Vector3 vertexPos = voxel.Position;
            Vector3 diff = vertexPos - (light.Position + new Vector3(0.5f, 0.5f, 0.5f));
            float dist = diff.LengthSquared() * 2;

            return (byte)(int)((Math.Min(1.0f / (dist + 0.0001f), 1.0f)) * (float)light.Intensity);
        }


        public static void CalculateVertexLight(Voxel vox, VoxelVertex face,
            ChunkManager chunks, List<Voxel> neighbors, ref VertexColorInfo color)
        {
            float numHit = 1;
            float numChecked = 1;

            int index = vox.Index;
            color.DynamicColor = 0;
            color.SunColor += vox.Chunk.Data.SunColors[index];
            vox.Chunk.GetNeighborsVertex(face, vox, neighbors);

            foreach (Voxel v in neighbors)
            {
                if (v == null)
                {
                    continue;
                }
                color.SunColor += v.SunColor;
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


            float proportionHit = numHit / numChecked;
            color.AmbientColor = (int)Math.Min((1.0f - proportionHit) * 255.0f, 255);
            color.SunColor = (int)Math.Min((float)color.SunColor / (float)numChecked, 255);
        }


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

        public void ResetSunlight(byte sunColor)
        {
            int numVoxels = sizeX * sizeY * sizeZ;
            for (int i = 0; i < numVoxels; i++)
            {
                Data.SunColors[i] = sunColor;
            }
        }

        public float GetTotalWaterHeight(Voxel voxRef)
        {
            float tot = 0;
            int x = (int)voxRef.GridPosition.X;
            int z = (int)voxRef.GridPosition.Z;
            for (int y = (int)voxRef.GridPosition.Y; y < SizeY; y++)
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

        public float GetTotalWaterHeightCells(Voxel voxRef)
        {
            float tot = 0;
            int x = (int)voxRef.GridPosition.X;
            int z = (int)voxRef.GridPosition.Z;
            for (int y = (int)voxRef.GridPosition.Y; y < SizeY; y++)
            {
                int index = Data.IndexAt(x, y, z);
                tot += (Data.Water[index].WaterLevel) / (float)WaterManager.maxWaterLevel;

                if (Data.Water[index].WaterLevel == 0 && y > (int)voxRef.GridPosition.Y)
                {
                    return tot;
                }
            }

            return tot;
        }

        public void UpdateSunlight(byte sunColor)
        {
            LightingCalculated = false;

            if (!Manager.ChunkData.ChunkMap.ContainsKey(ID))
            {
                return;
            }

            ResetSunlight(0);
            Voxel reference = MakeVoxel(0, 0, 0);

            for (int x = 0; x < SizeX; x++)
            {
                for (int z = 0; z < SizeZ; z++)
                {
                    bool rayHit = false;
                    for (int y = SizeY - 1; y >= 0; y--)
                    {
                        if (rayHit)
                        {
                            break;
                        }
                        reference.GridPosition = new Vector3(x, y, z);
                        int index = Data.IndexAt(x, y, z);
                        if (Data.Types[index] == 0 || VoxelLibrary.GetVoxelType(Data.Types[index]).IsTransparent)
                        {
                            Data.SunColors[index] = sunColor;
                            continue;
                        }

                        if (y >= SizeY - 1)
                        {
                            continue;
                        }

                        Data.SunColors[reference.Index] = sunColor;
                        rayHit = true;
                    }
                }
            }
        }

        public void GetSharedVertices(Voxel v, VoxelVertex vertex, List<KeyValuePair<Voxel, List<VoxelVertex>>> vertices, List<Voxel> neighbors)
        {
            vertices.Clear();


            GetNeighborsVertex(vertex, v, neighbors);

            Vector3 myDelta = vertexDeltas[(int)vertex];
            foreach (Voxel neighbor in neighbors)
            {
                if (neighbor == null || neighbor.IsEmpty)
                {
                    continue;
                }

                List<VoxelVertex> vertsNeighbor = new List<VoxelVertex>();
                Vector3 otherDelta = v.Position - neighbor.Position + myDelta;
                vertsNeighbor.Add(GetNearestDelta(otherDelta));


                vertices.Add(new KeyValuePair<Voxel, List<VoxelVertex>>(neighbor, vertsNeighbor));
            }
        }

        public void CalculateVertexLighting()
        {
            /*
            List<Voxel> neighbors = new List<Voxel>();
            VertexColorInfo colorInfo = new VertexColorInfo();
            bool ambientOcclusion = GameSettings.Default.AmbientOcclusion;
            Voxel voxel = MakeVoxel(0, 0, 0);
            HashSet<int> indexesToUpdate = new HashSet<int>();

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
                                    int vertIndex = Data.VertIndex(x, y, z, (VoxelVertex)i);
                                    if (!indexesToUpdate.Contains(vertIndex))
                                    {
                                        indexesToUpdate.Add(Data.VertIndex(x, y, z, (VoxelVertex)i));
                                        CalculateVertexLight(voxel, (VoxelVertex)i, Manager, neighbors, ref colorInfo);
                                        if (type.EmitsLight) colorInfo.DynamicColor = 255;
                                        Data.SetColor(x, y, z, (VoxelVertex)i,
                                            new Color(colorInfo.SunColor, colorInfo.AmbientColor, colorInfo.DynamicColor));
                                    }
                                }
                            }
                            else
                            {
                                byte sunColor = Data.SunColors[Data.IndexAt((int)voxel.GridPosition.X, (int)voxel.GridPosition.Y, (int)voxel.GridPosition.Z)];
                                for (int i = 0; i < 8; i++)
                                {
                                    int vertIndex = Data.VertIndex(x, y, z, (VoxelVertex)i);
                                    if (!indexesToUpdate.Contains(vertIndex))
                                    {
                                        indexesToUpdate.Add(vertIndex);
                                        Data.SetColor(x, y, z, (VoxelVertex)i, new Color(sunColor, 128, 0));
                                    }
                                }
                            }
                        }
                        else if (voxel.IsVisible)
                        {
                            Data.SunColors[Data.IndexAt((int)voxel.GridPosition.X, (int)voxel.GridPosition.Y, (int)voxel.GridPosition.Z)] = 0;
                            for (int i = 0; i < 8; i++)
                            {
                                int vertIndex = Data.VertIndex(x, y, z, (VoxelVertex)i);
                                if (!indexesToUpdate.Contains(vertIndex))
                                {
                                    indexesToUpdate.Add(vertIndex);
                                    Data.SetColor(x, y, z, (VoxelVertex)i, new Color(0, m_fogOfWar, 0));
                                }
                            }
                        }
                    }
                }
            }

            LightingCalculated = true;
             */
        }

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

        public int GetFilledVoxelGridHeightAt(int x, int y, int z)
        {
            int invalid = -1;

            if (!IsCellValid(x, y, z))
            {
                return invalid;
            }
            else
            {
                for (int h = y; h > 0; h--)
                {
                    if (Data.Types[Data.IndexAt(x, h, z)] != 0)
                    {
                        return h + 1;
                    }
                }
            }

            return invalid;
        }

        public int GetFilledHeightOrWaterAt(int x, int y, int z)
        {
            if (!IsCellValid(x, y, z))
            {
                return -1;
            }
            else
            {
                for (int h = y; h >= 0; h--)
                {
                    if (Data.Types[Data.IndexAt(x, h, z)] != 0 || Data.Water[Data.IndexAt(x, h, z)].WaterLevel > 1)
                    {
                        return h + 1;
                    }
                }
            }

            return -1;
        }

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
                        if (test > level && (!voxel.IsEmpty || voxel.WaterLevel > 0))
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

        //-------------------------
        public void GetNeighborsVertex(VoxelVertex vertex, Voxel v, List<Voxel> toReturn)
        {
            Vector3 grid = v.GridPosition;
            GetNeighborsVertex(vertex, (int)grid.X, (int)grid.Y, (int)grid.Z, toReturn);
        }

        public int GridPositionToEuclidianLookupKey(Vector3 gridPos)
        {
            Debug.Assert(gridPos.X >= 0 && gridPos.X < SizeX, "X Position is out of range.");
            Debug.Assert(gridPos.Y >= 0 && gridPos.Y < SizeY, "Y Position is out of range.");
            Debug.Assert(gridPos.Z >= 0 && gridPos.Z < SizeZ, "Z Position is out of range.");

            int key = 0;
            if (gridPos.X == SizeX - 1) key += 2;
            else if (gridPos.X != 0) key += 1;
            if (gridPos.Y == SizeY - 1) key += 6;
            else if (gridPos.Y != 0) key += 3;
            if (gridPos.Z == SizeZ - 1) key += 18;
            else if (gridPos.Z != 0) key += 9;

            return key;
        }

        // Function does not need any chunk specific information so it is static.
        public static int SuccessorToEuclidianLookupKey(Vector3 successor)
        {
            return SuccessorToEuclidianLookupKey(MathFunctions.FloorInt(successor.X), MathFunctions.FloorInt(successor.Y), MathFunctions.FloorInt(successor.Z));
        }

        public static int SuccessorToEuclidianLookupKey(Point3 successor)
        {
            return SuccessorToEuclidianLookupKey(successor.X, successor.Y, successor.Z);
        }

        public static int SuccessorToEuclidianLookupKey(int x, int y, int z)
        {
            Debug.Assert(x >= -1 && x <= 1, "X coordinate of successor must be between -1 and 1");
            Debug.Assert(y >= -1 && y <= 1, "Y coordinate of successor must be between -1 and 1");
            Debug.Assert(z >= -1 && z <= 1, "Z coordinate of successor must be between -1 and 1");

            return (x + 1) + (y + 1) * 3 + (z + 1) * 9;
        }

        public List<Point3> GetEuclidianSuccessorByVoxel(Voxel v)
        {
            Vector3 gridPos;
            if (v == null)
            {
                gridPos = Vector3.Zero;
            }
            else
            {
                gridPos = v.GridPosition;
            }

            int key = GridPositionToEuclidianLookupKey(gridPos);

            return EuclideanSuccessorsByVoxelPosition[key];
        }

        public bool IsInterior(int x, int y, int z)
        {
            return (x != 0 && y != 0 && z != 0 && x != SizeX - 1 && y != SizeY - 1 && z != SizeZ - 1);
        }

        /*
        public void GetNeighborsSuccessors(List<Vector3> succ, int x, int y, int z, List<DestinationVoxel> toReturn, bool considerEmpties)
        {
            toReturn.Clear();

            Vector3 successor = Vector3.Zero;
            bool isInterior = IsInterior(x, y, z);
            int count = succ.Count;
            for(int i = 0; i < count; i++)
            {
                successor = succ[i];
                int nx = (int) successor.X + x;
                int ny = (int) successor.Y + y;
                int nz = (int) successor.Z + z;

                if(isInterior || IsCellValid(nx, ny, nz))
                {
                    DestinationVoxel v = MakeVoxel(nx, ny, nz);
                    if(!v.IsEmpty)
                    {
                        toReturn.Add(v);
                    }
                    else if(considerEmpties)
                    {
                        toReturn.Add(null);
                    }
                }
                else
                {
                    Point3 chunkID = ID;
                    if(nx >= SizeZ)
                    {
                        chunkID.X += 1;
                        nx = 0;
                    }
                    else if(nx < 0)
                    {
                        chunkID.X -= 1;
                        nx = SizeX - 1;
                    }

                    if(ny >= SizeY)
                    {
                        chunkID.Y += 1;
                        ny = 0;
                    }
                    else if(ny < 0)
                    {
                        chunkID.Y -= 1;
                        ny = SizeY - 1;
                    }

                    if(nz >= SizeZ)
                    {
                        chunkID.Z += 1;
                        nz = 0;
                    }
                    else if(nz < 0)
                    {
                        chunkID.Z -= 1;
                        nz = SizeZ - 1;
                    }


                    if(!Manager.ChunkData.ChunkMap.ContainsKey(chunkID))
                    {
                        continue;
                    }

                    VoxelChunk chunk = Manager.ChunkData.ChunkMap[chunkID];
                    DestinationVoxel n = chunk.MakeVoxel(nx, ny, nz);

                    if(n != null)
                    {
                        toReturn.Add(n);
                    }
                    else if(considerEmpties)
                    {
                        toReturn.Add(null);
                    }
                }
            }
        }
        */

        public BoxTransition ComputeTransitionValue(VoxelType.TransitionType transitionType, int x, int y, int z, Voxel[] neighbors)
        {
            VoxelType type = VoxelLibrary.GetVoxelType(Data.Types[Data.IndexAt(x, y, z)]);

            if (transitionType == VoxelType.TransitionType.Horizontal)
            {
                Get2DManhattanNeighbors(neighbors, x, y, z);

                var value = ComputeTransitionValue(neighbors, type);
                TransitionTexture toReturn = (TransitionTexture)value;
                return new BoxTransition()
                {
                    Top = toReturn
                };
            }
            else
            {
                Get2DManhattanNeighbors(ChunkManager.SliceMode.Z, neighbors, x, y, z);
                var transitionFrontBack = ComputeTransitionValue(neighbors, type);
                Get2DManhattanNeighbors(ChunkManager.SliceMode.X, neighbors, x, y, z);
                var transitionLeftRight = ComputeTransitionValue(neighbors, type);
                return new BoxTransition()
                {
                    Front = (TransitionTexture) transitionFrontBack,
                    Back = (TransitionTexture) transitionFrontBack,
                    Left = (TransitionTexture) transitionLeftRight,
                    Right = (TransitionTexture) transitionLeftRight
                };
            }
        }

        private static int ComputeTransitionValue(Voxel[] neighbors, VoxelType type)
        {
            int value = 0;
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != null && !neighbors[i].IsEmpty && neighbors[i].Type == type)
                {
                    value += manhattan2DMultipliers[i];
                }
            }
            return value;
        }

        public void Get2DManhattanNeighbors(Voxel[] neighbors, int x, int y, int z)
        {
            List<Vector3> succ = Manhattan2DSuccessors;
            int count = succ.Count;
            bool isInterior = IsInterior(x, y, z);
            for (int i = 0; i < count; i++)
            {
                Vector3 successor = succ[i];
                int nx = (int)successor.X + x;
                int ny = (int)successor.Y + y;
                int nz = (int)successor.Z + z;

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

        public void Get2DManhattanNeighbors(ChunkManager.SliceMode slice, Voxel[] neighbors, int x, int y, int z)
        {
            List<Vector3> succ = Manhattan2DSuccessors;
            int count = succ.Count;
            bool isInterior = IsInterior(x, y, z);
            for (int i = 0; i < count; i++)
            {
                Vector3 successor = succ[i];

                switch (slice)
                {
                    // X held constant, so we're looking at the ZY plane
                    case ChunkManager.SliceMode.X:
                            successor = new Vector3(0, succ[i].Z, succ[i].X);
                        break;
                     //Y held constant. Just do what we did before.
                    case ChunkManager.SliceMode.Y:
                        break;

                    // Z held constant, so we're looking at the XY plane
                    case ChunkManager.SliceMode.Z:
                        successor = new Vector3(succ[i].X, succ[i].Z, 0);
                        break;
                }
                int nx = (int)successor.X + x;
                int ny = (int)successor.Y + y;
                int nz = (int)successor.Z + z;

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
                int nx = (int)successor.X + x;
                int ny = (int)successor.Y + y;
                int nz = (int)successor.Z + z;

                if (isInterior || IsCellValid(nx, ny, nz))
                {
                    if (toReturn[i] == null)
                    {
                        toReturn[i] = MakeVoxel(nx, ny, nz);
                    }
                    else
                    {
                        toReturn[i].Chunk = this;
                        toReturn[i].GridPosition = new Vector3(nx, ny, nz);
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
                        toReturn[i] = null;
                        continue;
                    }

                    VoxelChunk chunk = Manager.ChunkData.ChunkMap[chunkID];
                    if (toReturn[i] == null)
                    {
                        toReturn[i] = chunk.MakeVoxel(nx, ny, nz);
                    }
                    else
                    {
                        toReturn[i].Chunk = chunk;
                        toReturn[i].GridPosition = new Vector3(nx, ny, nz);
                    }
                }
            }
        }

        public void GetNeighborsVertex(VoxelVertex vertex, int x, int y, int z, List<Voxel> toReturn)
        {
            GetNeighborsSuccessors(VertexSuccessors[vertex], x, y, z, toReturn);
        }

        public void GetNeighborsVertexDiag(VoxelVertex vertex, int x, int y, int z, List<Voxel> toReturn)
        {
            GetNeighborsSuccessors(VertexSuccessorsDiag[vertex], x, y, z, toReturn);
        }

        public IEnumerable<Voxel> GetNeighborsEuclidean(Voxel v)
        {
            Vector3 gridCoord = v.GridPosition;
            return GetNeighborsEuclidean((int)gridCoord.X, (int)gridCoord.Y, (int)gridCoord.Z);
        }

        public IEnumerable<Voxel> GetNeighborsEuclidean(int x, int y, int z)
        {
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

                        int nx = (int) dx + x;
                        int ny = (int) dy + y;
                        int nz = (int) dz + z;

                        if (isInterior || IsCellValid(nx, ny, nz))
                        {
                            yield return MakeVoxel(nx, ny, nz);
                        }
                        else
                        {
                            Voxel otherVox = new Voxel();
                            if (Manager.ChunkData.GetVoxel(this, new Vector3(nx, ny, nz) + Origin + Vector3.One * 0.5f, ref otherVox))
                            {
                                yield return otherVox;
                            }
                        }
                    }
                }
            }
            ;
        }


        public List<Voxel> AllocateVoxels(int num)
        {
            List<Voxel> toReturn = new List<Voxel>();
            for (int i = 0; i < num; i++)
            {
                toReturn.Add(MakeVoxel(0, 0, 0));
            }

            return toReturn;
        }

        public void GetNeighborsManhattan(int x, int y, int z, List<Voxel> neighbors)
        {
            GetNeighborsSuccessors(ManhattanSuccessors, x, y, z, neighbors);
        }

        public void NotifyTotalRebuild(bool neighbors)
        {
            ShouldRebuild = true;
            ShouldRecalculateLighting = true;
            ShouldRebuildWater = true;
            ReconstructRamps = true;

            if (neighbors)
            {
                foreach (VoxelChunk chunk in Neighbors.Values)
                {
                    chunk.ShouldRebuild = true;
                    chunk.ShouldRecalculateLighting = true;
                    chunk.ShouldRebuildWater = true;
                }
            }
        }

        private bool IsEmpty(Voxel v)
        {
            return v == null || v.IsEmpty;
        }

        public bool HasNoNeighbors(Voxel v)
        {
            Vector3 pos = v.Position;
            Vector3 gridPos = v.GridPosition;

            if (!Manager.ChunkData.ChunkMap.ContainsKey(v.ChunkID))
            {
                return false;
            }

            VoxelChunk chunk = Manager.ChunkData.ChunkMap[v.ChunkID];
            Point3 gridPoint = new Point3(gridPos);

            bool interior = Voxel.IsInteriorPoint(gridPoint, chunk);


            if (interior)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3 neighbor = ManhattanSuccessors[i];
                    int index = Data.IndexAt(gridPoint.X + (int)neighbor.X, gridPoint.Y + (int)neighbor.Y, gridPoint.Z + (int)neighbor.Z);

                    if (Data.Types[index] != 0)
                    {
                        return false;
                    }
                }
            }
            else
            {

                Voxel atPos = new Voxel();
                for (int i = 0; i < 6; i++)
                {
                    Vector3 neighbor = ManhattanSuccessors[i];

                    if (!IsGridPositionValid(neighbor + gridPos))
                    {
                        if (Manager.ChunkData.GetNonNullVoxelAtWorldLocation(pos + neighbor, ref atPos) && !atPos.IsEmpty)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        int index = Data.IndexAt(gridPoint.X + (int)neighbor.X, gridPoint.Y + (int)neighbor.Y, gridPoint.Z + (int)neighbor.Z);

                        if (Data.Types[index] != 0)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }


        public bool IsCompletelySurrounded(Voxel v, bool ramps)
        {
            if (!Manager.ChunkData.ChunkMap.ContainsKey(v.ChunkID))
            {
                return false;
            }

            Vector3 pos = v.Position;
            VoxelChunk chunk = Manager.ChunkData.ChunkMap[v.ChunkID];
            Point3 gridPoint = new Point3(v.GridPosition);
            bool interior = Voxel.IsInteriorPoint(gridPoint, chunk);


            if (interior)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3 neighbor = ManhattanSuccessors[i];
                    int neighborIndex = Data.IndexAt(gridPoint.X + (int)neighbor.X, gridPoint.Y + (int)neighbor.Y,
                        gridPoint.Z + (int)neighbor.Z);
                    if (Data.Types[neighborIndex] == 0 || (ramps && Data.RampTypes[neighborIndex] != RampType.None))
                    {
                        return false;
                    }
                }
            }
            else
            {
                Voxel atPos = new Voxel();
                for (int i = 0; i < 6; i++)
                {
                    Vector3 neighbor = ManhattanSuccessors[i];

                    if (!Manager.ChunkData.GetNonNullVoxelAtWorldLocationCheckFirst(chunk, pos + neighbor, ref atPos) || atPos.IsEmpty || (ramps && atPos.RampType != RampType.None))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool IsCompletelySurrounded(Voxel v)
        {
            return IsCompletelySurrounded(v, false);
        }

        public void GetNeighborsManhattan(Voxel v, List<Voxel> toReturn)
        {
            GetNeighborsManhattan((int)v.GridPosition.X, (int)v.GridPosition.Y, (int)v.GridPosition.Z, toReturn);
        }


        public Vector3 GridToWorld(Vector3 gridCoord)
        {
            return gridCoord + Origin;
        }

        //-------------------------

        public List<Voxel> GetVoxelsIntersecting(BoundingBox box)
        {
            if (!GetBoundingBox().Intersects(box) && GetBoundingBox().Contains(box) != ContainmentType.Disjoint)
            {
                return new List<Voxel>();
            }
            else
            {
                BoundingBox myBox = GetBoundingBox();
                List<Voxel> toReturn = new List<Voxel>();
                for (float x = Math.Max(box.Min.X, myBox.Min.X); x < Math.Min(box.Max.X, myBox.Max.X); x++)
                {
                    for (float y = Math.Max(box.Min.Y, myBox.Min.Y); y < Math.Min(box.Max.Y, myBox.Max.Y); y++)
                    {
                        for (float z = Math.Max(box.Min.Z, myBox.Min.Z); z < Math.Min(box.Max.Z, myBox.Max.Z); z++)
                        {
                            Vector3 grid = new Vector3(x, y, z) - Origin;
                            Voxel vox = MakeVoxel((int)grid.X, (int)grid.Y, (int)grid.Z);
                            toReturn.Add(vox);
                        }
                    }
                }

                return toReturn;
            }
        }

        #endregion neighbors

    }

}
