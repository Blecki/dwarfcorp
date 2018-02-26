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
    public partial class VoxelChunk : IBoundedObject
    {
        public VoxelData Data { get; set; }

        public VoxelListPrimitive Primitive { get; set; }
        public VoxelListPrimitive NewPrimitive = null;
        public Dictionary<LiquidType, LiquidPrimitive> Liquids { get; set; }
        public bool NewPrimitiveReceived = false;
        public bool NewLiquidReceived = false;
        
        public bool IsVisible { get; set; }
        public Vector3 Origin { get; set; }
        public bool RenderWireframe { get; set; }
        public ChunkManager Manager { get; set; }

        public Mutex PrimitiveMutex { get; set; }

        public List<DynamicLight> DynamicLights { get; set; }

        private static bool staticsInitialized = false;
        private static Vector3[] faceDeltas = new Vector3[6];

        public GlobalChunkCoordinate ID { get; set; }

        public void InvalidateSlice(int Y)
        {
            if (Y < 0 || Y >= VoxelConstants.ChunkSizeY) throw new InvalidOperationException();

            lock (Data.SliceCache)
            {
                Data.SliceCache[Y] = null;
                Manager.InvalidateChunk(this);
            }
        }

        #region statics

        public static void InitializeStatics()
        {
            if (staticsInitialized)
            {
                return;
            }
            
            faceDeltas[(int)BoxFace.Top] = new Vector3(0.5f, 0.0f, 0.5f);
            faceDeltas[(int)BoxFace.Bottom] = new Vector3(0.5f, 1.0f, 0.5f);
            faceDeltas[(int)BoxFace.Left] = new Vector3(1.0f, 0.5f, 0.5f);
            faceDeltas[(int)BoxFace.Right] = new Vector3(0.0f, 0.5f, 0.5f);
            faceDeltas[(int)BoxFace.Front] = new Vector3(0.5f, 0.5f, 0.0f);
            faceDeltas[(int)BoxFace.Back] = new Vector3(0.5f, 0.5f, 1.0f);
            

            staticsInitialized = true;
        }

        #endregion

        public VoxelChunk(ChunkManager manager, Vector3 origin, GlobalChunkCoordinate id)
        {
            ID = id;
            Origin = origin;
            Data = VoxelData.Allocate();
            IsVisible = true;
            Primitive = new VoxelListPrimitive();
            RenderWireframe = false;
            Manager = manager;

            InitializeStatics();
            PrimitiveMutex = new Mutex();
            DynamicLights = new List<DynamicLight>();

            Liquids = new Dictionary<LiquidType, LiquidPrimitive>();
            Liquids[LiquidType.Water] = new LiquidPrimitive(LiquidType.Water);
            Liquids[LiquidType.Lava] = new LiquidPrimitive(LiquidType.Lava);
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

        public void RecieveNewPrimitive(DwarfTime t)
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
        }

        public void Rebuild(GraphicsDevice g)
        {
            if (g == null || g.IsDisposed)
                return;
            VoxelListPrimitive primitive = new VoxelListPrimitive();
            primitive.InitializeFromChunk(this);

            var changedMessage = new Message(Message.MessageType.OnChunkModified, "Chunk Modified");
            foreach (var c in Manager.World.CollisionManager.EnumerateIntersectingObjects(GetBoundingBox(),
                CollisionManager.CollisionType.Both).OfType<GameComponent>())
                c.ReceiveMessageRecursive(changedMessage);
        }

        public void Destroy(GraphicsDevice device)
        {
            if (Primitive != null)
            {
                Primitive.ResetBuffer(device);
            }
        }

        public void CalculateInitialSunlight()
        {
            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    for (int y = VoxelConstants.ChunkSizeY - 1; y >= 0; y--)
                    {
                        LocalVoxelCoordinate coord = new LocalVoxelCoordinate(x, y, z);
                        VoxelHandle voxel = new VoxelHandle(this, coord) {SunColor = 255};
                        if (!voxel.IsEmpty && !voxel.Type.IsTransparent) break;
                    }
                }
            }
        }
    }
}
