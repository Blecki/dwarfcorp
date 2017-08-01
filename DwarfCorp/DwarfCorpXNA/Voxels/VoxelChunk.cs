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
        public bool ShouldRebuildWater { get; set; }

        public List<DynamicLight> DynamicLights { get; set; }


        private static bool staticsInitialized = false;
        private static Vector3[] vertexDeltas = new Vector3[8];
        private static Vector3[] faceDeltas = new Vector3[6];

        private bool firstRebuild = true;

        public bool RebuildPending { get; set; }
        public bool RebuildLiquidPending { get; set; }
        public GlobalChunkCoordinate ID { get; set; }

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
            DynamicLights = new List<DynamicLight>();

            Liquids = new Dictionary<LiquidType, LiquidPrimitive>();
            Liquids[LiquidType.Water] = new LiquidPrimitive(LiquidType.Water);
            Liquids[LiquidType.Lava] = new LiquidPrimitive(LiquidType.Lava);
            ShouldRebuildWater = true;

            IsRebuilding = false;
            RebuildPending = false;
            RebuildLiquidPending = false;
        }
       
        public static VoxelVertex GetNearestDelta(Vector3 position)
        {
            float bestDist = float.MaxValue;
            VoxelVertex bestKey = 0;
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

            NotifyChangedComponents();
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
    }
}
