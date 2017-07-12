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
using System.Linq;

namespace DwarfCorp
{
    /// <summary>
    /// A 3D grid of voxels, water, and light.
    /// </summary>
    public class VoxelChunk : IBoundedObject
    {
        //Todo: Use actions
        public delegate void VoxelDestroyed(LocalVoxelCoordinate voxelID);
        public event VoxelDestroyed OnVoxelDestroyed;

        public delegate void VoxelExplored(LocalVoxelCoordinate voxelID);

        public event VoxelExplored OnVoxelExplored;

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
        public ConcurrentDictionary<VoxelHandle, byte> Springs { get; set; }

        public bool IsVisible { get; set; }
        public bool ShouldRebuild { get; set; }
        public bool IsRebuilding { get; set; }
        public Vector3 Origin { get; set; }
        public bool RenderWireframe { get; set; }
        public ChunkManager Manager { get; set; }
        public bool IsActive { get; set; }
        public bool FirstWaterIter { get; set; }

        public Mutex PrimitiveMutex { get; set; }
        public bool ShouldRecalculateLighting { get; set; }
        public bool ShouldRebuildWater { get; set; }

        public static byte m_fogOfWar = 1;
        public List<DynamicLight> DynamicLights { get; set; }


        private static bool staticsInitialized = false;
        private static Vector3[] vertexDeltas = new Vector3[8];
        private static Vector3[] faceDeltas = new Vector3[6];

        public static readonly Dictionary<VoxelVertex, List<GlobalVoxelOffset>> VertexSuccessors = new Dictionary<VoxelVertex, List<GlobalVoxelOffset>>();
        public static readonly Dictionary<BoxFace, VoxelVertex[]> FaceVertices = new Dictionary<BoxFace, VoxelVertex[]>();
        public static List<List<Point3>> EuclideanSuccessorsByVoxelPosition;


        //public static ColorGradient MSunGradient = new ColorGradient(new Color(70, 70, 70), new Color(255, 254, 224), 255);
        //public static ColorGradient MAmbientGradient = new ColorGradient(new Color(50, 50, 50), new Color(255, 255, 255), 255);
        //public static ColorGradient MCaveGradient = new ColorGradient(new Color(8, 12, 17), new Color(41, 54, 76), 255);
        //public static ColorGradient MTorchGradient = null;
        public bool LightingCalculated { get; set; }
        private bool firstRebuild = true;

        public bool RebuildPending { get; set; }
        public bool RebuildLiquidPending { get; set; }
        public GlobalChunkCoordinate ID { get; set; }

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
                var successors = new List<GlobalVoxelOffset>();
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
                                successors.Add(new GlobalVoxelOffset(dx, dy, dz));
                            }
                        }
                    }
                }

                VertexSuccessors[vertex] = successors;
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

        public void NotifyExplored(LocalVoxelCoordinate voxel)
        {
            if (OnVoxelExplored != null)
            {
                OnVoxelExplored.Invoke(voxel);
            }
        }

        public void NotifyDestroyed(LocalVoxelCoordinate voxel)
        {
            if (OnVoxelDestroyed != null)
            {
                OnVoxelDestroyed.Invoke(voxel);
            }
        }

        public VoxelChunk(ChunkManager manager, Vector3 origin, GlobalChunkCoordinate id)
        {
            FirstWaterIter = true;
            ID = id;
            Origin = origin;
            Data = VoxelData.Allocate();
            IsVisible = true;
            ShouldRebuild = true;
            Primitive = new VoxelListPrimitive();
            RenderWireframe = false;
            Manager = manager;
            IsActive = true;

            InitializeStatics();
            PrimitiveMutex = new Mutex();
            ShouldRecalculateLighting = true;
            DynamicLights = new List<DynamicLight>();
            Liquids = new Dictionary<LiquidType, LiquidPrimitive>();
            Liquids[LiquidType.Water] = new LiquidPrimitive(LiquidType.Water);
            Liquids[LiquidType.Lava] = new LiquidPrimitive(LiquidType.Lava);
            ShouldRebuildWater = true;
            Springs = new ConcurrentDictionary<VoxelHandle, byte>();

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
                Vector3 max = new Vector3(VoxelConstants.ChunkSizeX, VoxelConstants.ChunkSizeY, VoxelConstants.ChunkSizeZ) + Origin;
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
                float m = VoxelConstants.ChunkSizeY * 0.5f;
                m_boundingSphere = new BoundingSphere(Origin + new Vector3(VoxelConstants.ChunkSizeX, VoxelConstants.ChunkSizeY, VoxelConstants.ChunkSizeZ) * 0.5f, (float)Math.Sqrt(3 * m * m));
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

        [Obsolete]
        public VoxelHandle MakeVoxel(int x, int y, int z)
        {
            return new VoxelHandle(new LocalVoxelCoordinate(x, y, z), this);
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
                VoxelHandle v = MakeVoxel(0, 0, 0);
                VoxelHandle voxelBelow = MakeVoxel(0, 0, 0);
                for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
                {
                    for (int y = 1; y < Math.Min(Manager.ChunkData.MaxViewingLevel + 1, VoxelConstants.ChunkSizeY - 1); y++)
                    {
                        for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                        {
                            v.GridPosition = new LocalVoxelCoordinate(x, y, z);
                            voxelBelow.GridPosition = new LocalVoxelCoordinate(x, y - 1, z);

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

                            float value = MoteNoise.Noise(v.WorldPosition.X * moteData.RegionScale, v.WorldPosition.Y * moteData.RegionScale, v.WorldPosition.Z * moteData.RegionScale);
                            float s = MoteScaleNoise.Noise(v.WorldPosition.X * moteData.RegionScale, v.WorldPosition.Y * moteData.RegionScale, v.WorldPosition.Z * moteData.RegionScale) * moteData.MoteScale;

                            if (!(Math.Abs(value) > moteData.SpawnThreshold))
                            {
                                continue;
                            }

                            Vector3 smallNoise = ClampVector(VertexNoise.GetRandomNoiseVector(v.WorldPosition * moteData.RegionScale * 20.0f) * 20.0f, 0.4f);
                            smallNoise.Y = 0.0f;
                            grassPositions.Add(v.WorldPosition + new Vector3(0.5f, 1.0f + s * 0.5f + vOffset, 0.5f) + smallNoise);
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
            //debug
            //Drawer3D.DrawBox(GetBoundingBox(), Color.White, 0.1f);

            if (g == null || g.IsDisposed)
            {
                return;
            }
            IsRebuilding = true;

            VoxelListPrimitive primitive = new VoxelListPrimitive();
            primitive.InitializeFromChunk(this);

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


        public bool GetVoxelAtWorldLocation(Vector3 worldLocation, ref VoxelHandle voxel)
        {
            Vector3 grid = WorldToGrid(worldLocation);

            bool valid = IsCellValid(MathFunctions.FloorInt(grid.X), MathFunctions.FloorInt(grid.Y), MathFunctions.FloorInt(grid.Z));

            if (!valid) return false;

            voxel.Chunk = this;
            voxel.GridPosition = new LocalVoxelCoordinate(MathFunctions.FloorInt(grid.X), MathFunctions.FloorInt(grid.Y), MathFunctions.FloorInt(grid.Z));
            return true;
        }

        // Same as GetVoxelAtWorldLocation except skips the part where it has to check to see if it is in range.
        // If you don't know for sure it is valid use the other function.
        public bool GetVoxelAtValidWorldLocation(GlobalVoxelCoordinate worldLocation, ref VoxelHandle voxel)
        {
            voxel.Chunk = this;
            voxel.GridPosition = worldLocation.GetLocalVoxelCoordinate();
            return true;
        }

        public bool IsGridPositionValid(LocalVoxelCoordinate grid)
        {
            return IsCellValid(grid.X, grid.Y, grid.Z);
        }

        public bool IsWorldLocationValid(Vector3 worldLocation)
        {
            Vector3 grid = WorldToGrid(worldLocation);

            return IsCellValid((int)grid.X, (int)grid.Y, (int)grid.Z);
        }

        public bool IsCellValid(int x, int y, int z)
        {
            return x >= 0 
                && y >= 0 
                && z >= 0 
                && x < VoxelConstants.ChunkSizeX 
                && y < VoxelConstants.ChunkSizeY 
                && z < VoxelConstants.ChunkSizeZ;
        }

        public bool IsCellValid(Point3 point)
        {
            return IsCellValid(point.X, point.Y, point.Z);
        }

        #endregion transformations

        #region lighting





        public byte GetIntensity(DynamicLight light, byte lightIntensity, VoxelHandle voxel)
        {
            Vector3 vertexPos = voxel.WorldPosition;
            Vector3 diff = vertexPos - (light.Position + new Vector3(0.5f, 0.5f, 0.5f));
            float dist = diff.LengthSquared() * 2;

            return (byte)(int)((Math.Min(1.0f / (dist + 0.0001f), 1.0f)) * (float)light.Intensity);
        }


        public static void CalculateVertexLight(VoxelHandle vox, VoxelVertex face,
            ChunkManager chunks, List<VoxelHandle> neighbors, ref VertexColorInfo color)
        {
            float numHit = 1;
            float numChecked = 1;

            color.DynamicColor = 0;
            color.SunColor += vox.Chunk.Data.SunColors[vox.Index];

            foreach (var c in DwarfCorp.Neighbors.EnumerateVertexNeighbors(vox.Coordinate, face))
            {
                var v = new TemporaryVoxelHandle(chunks.ChunkData, c);
                if (!v.IsValid) continue;

                color.SunColor += v.SunColor;
                if (!v.IsEmpty || !v.IsExplored)
                {
                    if (v.Type.EmitsLight) color.DynamicColor = 255;
                    numHit += 1;
                    numChecked += 1;
                }
                else
                    numChecked += 1;
            }
            
            float proportionHit = numHit / numChecked;
            color.AmbientColor = (int)Math.Min((1.0f - proportionHit) * 255.0f, 255);
            color.SunColor = (int)Math.Min((float)color.SunColor / (float)numChecked, 255);
        }

        public void ResetSunlight(byte sunColor)
        {
            for (int i = 0; i < VoxelConstants.ChunkVoxelCount; i++)
                Data.SunColors[i] = sunColor;
        }

        public void UpdateSunlight(byte sunColor)
        {
            LightingCalculated = false;

            if (!Manager.ChunkData.ChunkMap.ContainsKey(ID))
            {
                return;
            }

            ResetSunlight(0);
            VoxelHandle reference = MakeVoxel(0, 0, 0);

            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    bool rayHit = false;
                    for (int y = VoxelConstants.ChunkSizeY - 1; y >= 0; y--)
                    {
                        if (rayHit)
                        {
                            break;
                        }
                        reference.GridPosition = new LocalVoxelCoordinate(x, y, z);
                        int index = VoxelConstants.DataIndexOf(reference.GridPosition);
                        if (Data.Types[index] == 0 || VoxelLibrary.GetVoxelType(Data.Types[index]).IsTransparent)
                        {
                            Data.SunColors[index] = sunColor;
                            continue;
                        }

                        if (y >= VoxelConstants.ChunkSizeY - 1)
                        {
                            continue;
                        }

                        Data.SunColors[reference.Index] = sunColor;
                        rayHit = true;
                    }
                }
            }
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
                    if (Data.Types[VoxelConstants.DataIndexOf(new LocalVoxelCoordinate(x, h, z))] != 0)
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
                    if (Data.Types[VoxelConstants.DataIndexOf(new LocalVoxelCoordinate(x, h, z))] != 0 
                        || Data.Water[VoxelConstants.DataIndexOf(new LocalVoxelCoordinate(x, h, z))].WaterLevel > 1)
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

            int mx = VoxelConstants.ChunkSizeX;
            int my = VoxelConstants.ChunkSizeY;
            int mz = VoxelConstants.ChunkSizeZ;
            VoxelHandle voxel = MakeVoxel(0, 0, 0);
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

                        voxel.GridPosition = new LocalVoxelCoordinate(x, y, z);
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

        // Function does not need any chunk specific information so it is static.
        public static int SuccessorToEuclidianLookupKey(GlobalVoxelOffset successor)
        {
            return SuccessorToEuclidianLookupKey(MathFunctions.FloorInt(successor.X), MathFunctions.FloorInt(successor.Y), MathFunctions.FloorInt(successor.Z));
        }

        public static int SuccessorToEuclidianLookupKey(Point3 successor)
        {
            return SuccessorToEuclidianLookupKey(successor.X, successor.Y, successor.Z);
        }

        // Todo: %KILL%
        public static int SuccessorToEuclidianLookupKey(int x, int y, int z)
        {
            Debug.Assert(x >= -1 && x <= 1, "X coordinate of successor must be between -1 and 1");
            Debug.Assert(y >= -1 && y <= 1, "Y coordinate of successor must be between -1 and 1");
            Debug.Assert(z >= -1 && z <= 1, "Z coordinate of successor must be between -1 and 1");

            return (x + 1) + (y + 1) * 3 + (z + 1) * 9;
        }

        public bool IsInterior(int x, int y, int z)
        {
            return x != 0 
                && y != 0 
                && z != 0 
                && x != VoxelConstants.ChunkSizeX - 1 
                && y != VoxelConstants.ChunkSizeY - 1 
                && z != VoxelConstants.ChunkSizeZ - 1;
        }

        public void NotifyTotalRebuild(bool neighbors)
        {
            ShouldRebuild = true;
            ShouldRecalculateLighting = true;
            ShouldRebuildWater = true;
            ReconstructRamps = true;

            if (neighbors)
            {
                // Enumerator works in world space - this is the ONLY place where we
                //  enumerate neighbors in CHUNK space, so just hack it!
                foreach (var n in DwarfCorp.Neighbors.EnumerateManhattanNeighbors(
                    new GlobalVoxelCoordinate(ID.X, ID.Y, ID.Z)))
                {
                    var chunkCoord = new GlobalChunkCoordinate(n.X, n.Y, n.Z);
                    VoxelChunk chunk;
                    if (Manager.ChunkData.ChunkMap.TryGetValue(chunkCoord, out chunk))
                    {
                        chunk.ShouldRebuild = true;
                        chunk.ShouldRecalculateLighting = true;
                        chunk.ShouldRebuildWater = true;
                    }
                }
            }
        }

        public bool HasNoNeighbors(VoxelHandle V)
        {
            if (!Manager.ChunkData.ChunkMap.ContainsKey(V.ChunkID))
                return false;

            foreach (var neighborCoordinate in DwarfCorp.Neighbors.EnumerateManhattanNeighbors(V.Coordinate))
            {
                var voxelHandle = new TemporaryVoxelHandle(Manager.ChunkData, neighborCoordinate);
                if (!voxelHandle.IsValid) continue;
                if (!voxelHandle.IsEmpty) return false;
            }

            return true;
        }

        public bool IsCompletelySurrounded(VoxelHandle V)
        {
            if (!Manager.ChunkData.ChunkMap.ContainsKey(V.ChunkID))
                return false;

            foreach (var neighborCoordinate in DwarfCorp.Neighbors.EnumerateManhattanNeighbors(V.Coordinate))
            {
                var voxelHandle = new TemporaryVoxelHandle(Manager.ChunkData, neighborCoordinate);
                if (!voxelHandle.IsValid) return false;
                if (voxelHandle.IsEmpty) return false;
            }

            return true;
        }

        public Vector3 GridToWorld(Vector3 gridCoord)
        {
            return gridCoord + Origin;
        }

        public List<VoxelHandle> GetVoxelsIntersecting(BoundingBox box)
        {
            if (!GetBoundingBox().Intersects(box) && GetBoundingBox().Contains(box) != ContainmentType.Disjoint)
            {
                return new List<VoxelHandle>();
            }
            else
            {
                BoundingBox myBox = GetBoundingBox();
                List<VoxelHandle> toReturn = new List<VoxelHandle>();
                for (float x = Math.Max(box.Min.X, myBox.Min.X); x < Math.Min(box.Max.X, myBox.Max.X); x++)
                {
                    for (float y = Math.Max(box.Min.Y, myBox.Min.Y); y < Math.Min(box.Max.Y, myBox.Max.Y); y++)
                    {
                        for (float z = Math.Max(box.Min.Z, myBox.Min.Z); z < Math.Min(box.Max.Z, myBox.Max.Z); z++)
                        {
                            Vector3 grid = new Vector3(x, y, z) - Origin;
                            VoxelHandle vox = MakeVoxel((int)grid.X, (int)grid.Y, (int)grid.Z);
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
