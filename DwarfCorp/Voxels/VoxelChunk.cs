using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DwarfCorp
{
    public partial class VoxelChunk
    {
        public VoxelData Data { get; set; }
        public int RenderCycleWhenLastVisible = 0;
        public bool Visible = false;

        public VoxelListPrimitive Primitive = null;
        public Dictionary<LiquidType, LiquidPrimitive> Liquids { get; set; }
        public bool NewLiquidReceived = false;
        
        public GlobalVoxelCoordinate Origin { get; set; }
        public ChunkManager Manager { get; set; }

        public Mutex PrimitiveMutex { get; set; }

        public List<DynamicLight> DynamicLights { get; set; }

        public GlobalChunkCoordinate ID { get; set; }

        public void InvalidateSlice(int LocalY)
        {
            if (LocalY < 0 || LocalY >= VoxelConstants.ChunkSizeY) throw new InvalidOperationException();

            lock (Data.SliceCache)
            {
                Data.SliceCache[LocalY] = null;
                Manager.InvalidateChunk(this);
            }
        }

        public void DiscardPrimitive()
        {
            PrimitiveMutex.WaitOne();
            if (Primitive != null)
                Primitive.Dispose();
            Primitive = null;
            for (var y = 0; y < VoxelConstants.ChunkSizeY; ++y)
                Data.SliceCache[y] = null;
            PrimitiveMutex.ReleaseMutex();
        }

        public VoxelChunk(ChunkManager manager, GlobalChunkCoordinate id)
        {
            ID = id;
            Origin = new GlobalVoxelCoordinate(id, new LocalVoxelCoordinate(0,0,0));
            Data = VoxelData.Allocate();
            Manager = manager;

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
        
        public void Render(GraphicsDevice device)
        {
            PrimitiveMutex.WaitOne();
            if (Primitive != null) Primitive.Render(device);
            PrimitiveMutex.ReleaseMutex();
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
                designations = Manager.World.PlayerFaction.Designations;
            primitive.InitializeFromChunk(this, designations, Manager.World.DesignationDrawer, Manager.World);

            PrimitiveMutex.WaitOne();
            if (Primitive != null)
                Primitive.Dispose();
            Primitive = primitive;
            PrimitiveMutex.ReleaseMutex();

            // Todo: This can be tossed over into the other voxel event system and handled there.
            var changedMessage = new Message(Message.MessageType.OnChunkModified, "Chunk Modified");
            foreach (var c in Manager.World.EnumerateIntersectingObjects(GetBoundingBox(), CollisionType.Both))
                c.ReceiveMessageLater(changedMessage);
        }

        public void Destroy()
        {
            if (Primitive != null)
                Primitive.Dispose();
        }
    }
}
