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

        public List<DynamicLight> DynamicLights { get; set; }


        private static bool staticsInitialized = false;
        private static Vector3[] vertexDeltas = new Vector3[8];
        private static Vector3[] faceDeltas = new Vector3[6];

        public static readonly Dictionary<BoxFace, VoxelVertex[]> FaceVertices = new Dictionary<BoxFace, VoxelVertex[]>();

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

            staticsInitialized = true;
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

            IsRebuilding = false;
            LightingCalculated = false;
            RebuildPending = false;
            RebuildLiquidPending = false;
            ReconstructRamps = true;
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

                for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
                {
                    for (int y = 1; y < Math.Min(Manager.ChunkData.MaxViewingLevel + 1, VoxelConstants.ChunkSizeY - 1); y++)
                    {
                        for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                        {
                            var v = new TemporaryVoxelHandle(this, new LocalVoxelCoordinate(x, y, z));
                            var voxelBelow = new TemporaryVoxelHandle(this, new LocalVoxelCoordinate(x, y - 1, z));

                            if (v.IsEmpty || voxelBelow.IsEmpty
                                || v.Type.Name != grassType || !v.IsVisible
                                || voxelBelow.WaterCell.WaterLevel != 0)
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
                    grassColors, grassScales, Manager.World.ComponentManager, Manager.Content, Manager.Graphics, Motes[moteData.Name], moteData.Asset, moteData.Name);
            }
        }

        public void UpdateRamps()
        {
            if (ReconstructRamps || firstRebuild)
            {
                UpdateCornerRamps(this);
                ReconstructRamps = false;
            }
        }

        public static void UpdateCornerRamps(VoxelChunk chunk)
        {
            List<VoxelVertex> top = new List<VoxelVertex>()
            {
                VoxelVertex.FrontTopLeft,
                VoxelVertex.FrontTopRight,
                VoxelVertex.BackTopLeft,
                VoxelVertex.BackTopRight
            };

            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int y = 0; y < VoxelConstants.ChunkSizeY; y++)
                {
                    for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                    {
                        var v = new TemporaryVoxelHandle(chunk, new LocalVoxelCoordinate(x, y, z));
                        bool isTop = false;

                        if (y < VoxelConstants.ChunkSizeY - 1)
                        {
                            var vAbove = new TemporaryVoxelHandle(chunk, new LocalVoxelCoordinate(x, y + 1, z));

                            isTop = vAbove.IsEmpty;
                        }

                        if (v.IsEmpty || !v.IsVisible || !isTop || !v.Type.CanRamp)
                        {
                            v.RampType = RampType.None;
                            continue;
                        }
                        v.RampType = RampType.None;

                        foreach (VoxelVertex bestKey in top)
                        {
                            // If there are no empty neighbors, no slope.
                            if (!Neighbors.EnumerateVertexNeighbors2D(v.Coordinate, bestKey)
                                .Any(n =>
                                {
                                    var handle = new TemporaryVoxelHandle(chunk.Manager.ChunkData, n);
                                    return !handle.IsValid || handle.IsEmpty;
                                }))
                                continue;

                            switch (bestKey)
                            {
                                case VoxelVertex.FrontTopLeft:
                                    v.RampType |= RampType.TopBackLeft;
                                    break;
                                case VoxelVertex.FrontTopRight:
                                    v.RampType |= RampType.TopBackRight;
                                    break;
                                case VoxelVertex.BackTopLeft:
                                    v.RampType |= RampType.TopFrontLeft;
                                    break;
                                case VoxelVertex.BackTopRight:
                                    v.RampType |= RampType.TopFrontRight;
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private static void SetTop(RampType rampType, Dictionary<VoxelVertex, float> top)
        {
            float r = 0.5f;
            List<VoxelVertex> keys = top.Keys.ToList();
            foreach (VoxelVertex vert in keys)
            {
                top[vert] = 1.0f;
            }
            if (rampType.HasFlag(RampType.TopFrontLeft))
            {
                top[VoxelVertex.FrontTopLeft] = r;
            }

            if (rampType.HasFlag(RampType.TopFrontRight))
            {
                top[VoxelVertex.FrontTopRight] = r;
            }

            if (rampType.HasFlag(RampType.TopBackLeft))
            {
                top[VoxelVertex.BackTopLeft] = r;
            }
            if (rampType.HasFlag(RampType.TopBackRight))
            {
                top[VoxelVertex.BackTopRight] = r;
            }
        }

        // This insanity creates a map from voxel faces to ramp types, determining
        // whether or not the voxel on a face should draw that face given that its neighbor
        // has a certain ramp type.
        // This allows faces to be drawn next to ramps (for example, if dirt is next to stone,
        // the dirt ramps, revealing part of the stone face that needs to be drawn).
        public static void CreateFaceDrawMap()
        {
            BoxFace[] faces = (BoxFace[])Enum.GetValues(typeof(BoxFace));

            List<RampType> ramps = new List<RampType>()
            {
                RampType.None,
                RampType.TopFrontLeft,
                RampType.TopFrontRight,
                RampType.TopBackRight,
                RampType.TopBackLeft,
                RampType.Front,
                RampType.All,
                RampType.Back,
                RampType.Left,
                RampType.Right,
                RampType.TopFrontLeft | RampType.TopBackRight,
                RampType.TopFrontRight | RampType.TopBackLeft,
                RampType.TopBackLeft | RampType.TopBackRight | RampType.TopFrontLeft,
                RampType.TopBackLeft | RampType.TopBackRight | RampType.TopFrontRight,
                RampType.TopFrontLeft | RampType.TopFrontRight | RampType.TopBackLeft,
                RampType.TopFrontLeft | RampType.TopFrontRight | RampType.TopBackRight
            };

            Dictionary<VoxelVertex, float> myTop = new Dictionary<VoxelVertex, float>()
            {
                {
                    VoxelVertex.BackTopLeft, 0.0f
                },
                {
                    VoxelVertex.BackTopRight,  0.0f
                },
                {
                    VoxelVertex.FrontTopLeft,  0.0f
                },
                {
                    VoxelVertex.FrontTopRight,  0.0f
                }
            };

            Dictionary<VoxelVertex, float> theirTop = new Dictionary<VoxelVertex, float>()
            {
                {
                    VoxelVertex.BackTopLeft,  0.0f
                },
                {
                    VoxelVertex.BackTopRight,  0.0f
                },
                {
                    VoxelVertex.FrontTopLeft,  0.0f
                },
                {
                    VoxelVertex.FrontTopRight, 0.0f
                }
            };

            foreach (RampType myRamp in ramps)
            {
                SetTop(myRamp, myTop);
                foreach (RampType neighborRamp in ramps)
                {
                    SetTop(neighborRamp, theirTop);
                    foreach (BoxFace neighborFace in faces)
                    {

                        if (neighborFace == BoxFace.Bottom || neighborFace == BoxFace.Top)
                        {
                            GeometricPrimitive.FaceDrawMap[(int)neighborFace, (int)myRamp, (int)neighborRamp] = false;
                            continue;
                        }

                        float my1 = 0.0f;
                        float my2 = 0.0f;
                        float their1 = 0.0f;
                        float their2 = 0.0f;

                        switch (neighborFace)
                        {
                            case BoxFace.Back:
                                my1 = myTop[VoxelVertex.BackTopLeft];
                                my2 = myTop[VoxelVertex.BackTopRight];
                                their1 = theirTop[VoxelVertex.FrontTopLeft];
                                their2 = theirTop[VoxelVertex.FrontTopRight];
                                break;
                            case BoxFace.Front:
                                my1 = myTop[VoxelVertex.FrontTopLeft];
                                my2 = myTop[VoxelVertex.FrontTopRight];
                                their1 = theirTop[VoxelVertex.BackTopLeft];
                                their2 = theirTop[VoxelVertex.BackTopRight];
                                break;
                            case BoxFace.Left:
                                my1 = myTop[VoxelVertex.FrontTopLeft];
                                my2 = myTop[VoxelVertex.BackTopLeft];
                                their1 = theirTop[VoxelVertex.FrontTopRight];
                                their2 = theirTop[VoxelVertex.BackTopRight];
                                break;
                            case BoxFace.Right:
                                my1 = myTop[VoxelVertex.FrontTopRight];
                                my2 = myTop[VoxelVertex.BackTopRight];
                                their1 = theirTop[VoxelVertex.FrontTopLeft];
                                their2 = theirTop[VoxelVertex.BackTopLeft];
                                break;
                            default:
                                break;
                        }

                        GeometricPrimitive.FaceDrawMap[(int)neighborFace, (int)myRamp, (int)neighborRamp] = (their1 < my1 || their2 < my2);
                    }
                }
            }
        }

        // Todo: Move
        public static bool ShouldRamp(VoxelVertex vertex, RampType rampType)
        {
            bool toReturn = false;

            if ((rampType & RampType.TopFrontRight) == RampType.TopFrontRight)
            {
                toReturn = (vertex == VoxelVertex.BackTopRight);
            }

            if ((rampType & RampType.TopBackRight) == RampType.TopBackRight)
            {
                toReturn = toReturn || (vertex == VoxelVertex.FrontTopRight);
            }

            if ((rampType & RampType.TopFrontLeft) == RampType.TopFrontLeft)
            {
                toReturn = toReturn || (vertex == VoxelVertex.BackTopLeft);
            }

            if ((rampType & RampType.TopBackLeft) == RampType.TopBackLeft)
            {
                toReturn = toReturn || (vertex == VoxelVertex.FrontTopLeft);
            }

            return toReturn;
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

        public byte GetIntensity(DynamicLight light, byte lightIntensity, TemporaryVoxelHandle voxel)
        {
            Vector3 vertexPos = voxel.WorldPosition;
            Vector3 diff = vertexPos - (light.Position + new Vector3(0.5f, 0.5f, 0.5f));
            float dist = diff.LengthSquared() * 2;

            return (byte)(int)((Math.Min(1.0f / (dist + 0.0001f), 1.0f)) * (float)light.Intensity);
        }


        public static void CalculateVertexLight(TemporaryVoxelHandle vox, VoxelVertex face,
            ChunkManager chunks, ref VertexColorInfo color)
        {
            float numHit = 1;
            float numChecked = 1;

            color.DynamicColor = 0;
            color.SunColor += vox.SunColor;

            foreach (var c in Neighbors.EnumerateVertexNeighbors(vox.Coordinate, face))
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

            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    var y = VoxelConstants.ChunkSizeY - 1;

                    for (; y >= 0; y--)
                    {
                        int index = VoxelConstants.DataIndexOf(new LocalVoxelCoordinate(x, y, z));
                        Data.SunColors[index] = sunColor;

                        if (Data.Types[index] != 0 && !VoxelLibrary.GetVoxelType(Data.Types[index]).IsTransparent)
                            break;
                    }

                    for (y -= 1; y >= 0; y--)
                    {
                        int index = VoxelConstants.DataIndexOf(new LocalVoxelCoordinate(x, y, z));
                        Data.SunColors[index] = 0;
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

        public bool IsInterior(int x, int y, int z)
        {
            return x != 0 
                && y != 0 
                && z != 0 
                && x != VoxelConstants.ChunkSizeX - 1 
                && y != VoxelConstants.ChunkSizeY - 1 
                && z != VoxelConstants.ChunkSizeZ - 1;
        }

        public Vector3 GridToWorld(Vector3 gridCoord)
        {
            return gridCoord + Origin;
        }
    }
}
