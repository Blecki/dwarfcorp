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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DwarfCorp
{
    /// <summary>
    /// A 3D grid of voxels, water, and light.
    /// </summary>
    public partial class VoxelChunk
    {
        public VoxelData Data { get; set; }

        public VoxelListPrimitive Primitive { get; set; }
        public VoxelListPrimitive NewPrimitive = null;
        public Dictionary<LiquidType, LiquidPrimitive> Liquids { get; set; }
        public bool NewPrimitiveReceived = false;
        public bool NewLiquidReceived = false;
        
        public GlobalVoxelCoordinate Origin { get; set; }
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

        public VoxelChunk(ChunkManager manager, GlobalChunkCoordinate id)
        {
            ID = id;
            Origin = new GlobalVoxelCoordinate(id, new LocalVoxelCoordinate(0,0,0));
            Data = VoxelData.Allocate();
            Primitive = new VoxelListPrimitive();
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
                Vector3 max = new Vector3(VoxelConstants.ChunkSizeX, VoxelConstants.ChunkSizeY, VoxelConstants.ChunkSizeZ) + Origin.ToVector3();
                m_boundingBox = new BoundingBox(Origin.ToVector3(), max);
                m_boundingBoxCreated = true;
            }

            return m_boundingBox;
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
            Primitive.Render(device);
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
            DesignationSet designations = null;
            if (Manager.World.Master != null)
            {
                designations = Manager.World.PlayerFaction.Designations;
            }
            primitive.InitializeFromChunk(this, designations, Manager.World.DesignationDrawer, Manager.World);
            var changedMessage = new Message(Message.MessageType.OnChunkModified, "Chunk Modified");
            foreach (var c in Manager.World.EnumerateIntersectingObjects(GetBoundingBox(), CollisionType.Both))
                c.ReceiveMessageLater(changedMessage);
        }

        public void Destroy()
        {
            if (Primitive != null)
            {
                if (Primitive.VertexBuffer != null)
                    Primitive.VertexBuffer.Dispose();
                if (Primitive.IndexBuffer != null)
                 Primitive.IndexBuffer.Dispose();
            }
        }
    }
}
