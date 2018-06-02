// ChunkManager.cs
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp
{

    /// <summary>
    /// Responsible for keeping track of and accessing large collections of
    /// voxels. There is intended to be only one chunk manager. Essentially,
    /// it is a virtual memory lookup table for the world's voxels. It imitates
    /// a gigantic 3D array.
    /// </summary>
    public class ChunkManager
    {
        private Queue<VoxelChunk> RebuildQueue = new Queue<VoxelChunk>();
        private Mutex RebuildQueueLock = new Mutex();
        private AutoResetEvent RebuildEvent = new AutoResetEvent(true);
        public bool NeedsMinimapUpdate = true;

        public void InvalidateChunk(VoxelChunk Chunk)
        {
            RebuildQueueLock.WaitOne();
            RebuildEvent.Set();
            if (!RebuildQueue.Contains(Chunk))
                RebuildQueue.Enqueue(Chunk);
            RebuildQueueLock.ReleaseMutex();
        }

        public VoxelChunk PopInvalidChunk()
        {
            VoxelChunk result = null;
            RebuildQueueLock.WaitOne();
            if (RebuildQueue.Count > 0)
                result = RebuildQueue.Dequeue();
            RebuildQueueLock.ReleaseMutex();
            return result;
        }

        public Point3 WorldSize { get; set; }

        private List<VoxelChangeEvent> ChangedVoxels = new List<VoxelChangeEvent>();

        public void NotifyChangedVoxel(VoxelChangeEvent Change)
        {
            lock (ChangedVoxels)
            {
                ChangedVoxels.Add(Change);
            }
        }

        public ChunkGenerator ChunkGen { get; set; }

        private Thread RebuildThread { get; set; }
        private Thread ChunkUpdateThread { get; set; }
        private AutoScaleThread WaterUpdateThread;

        public BoundingBox Bounds { get; set; }

        public bool PauseThreads { get; set; }

        public bool ExitThreads { get; set; }

        public WorldManager World { get; set; }
        public ContentManager Content { get; set; }

        public WaterManager Water { get; set; }

        public Timer ChunkUpdateTimer = new Timer(0.1f, false, Timer.TimerMode.Real);

        // Todo: Move this.
        public bool IsAboveCullPlane(BoundingBox Box)
        {
            return Box.Min.Y > (World.Master.MaxViewingLevel + 5);
        }

        public ChunkData ChunkData
        {
            get { return chunkData; }
        }

        public ChunkManager(ContentManager content, 
            WorldManager world,
            ChunkGenerator chunkGen, int maxChunksX, int maxChunksY, int maxChunksZ)
        {
            WorldSize = new Point3(maxChunksX, maxChunksY, maxChunksZ);

            World = world;
            ExitThreads = false;
            Content = content;

            chunkData = new ChunkData(maxChunksX, maxChunksZ, 0, 0);             

            ChunkGen = chunkGen;

            RebuildThread = new Thread(RebuildVoxelsThread) { IsBackground = true };
            RebuildThread.Name = "RebuildVoxels";

            WaterUpdateThread = new AutoScaleThread(this, (f) => Water.UpdateWater());
            this.ChunkUpdateThread = new Thread(UpdateChunks) { IsBackground = true, Name = "Update Chunks" };

            chunkGen.Manager = this;

            GameSettings.Default.ChunkGenerateTime = 0.5f;
            GameSettings.Default.ChunkRebuildTime = 0.1f;
            Timer rebuildChunksTimer = new Timer(GameSettings.Default.ChunkRebuildTime, false, Timer.TimerMode.Real);
            GameSettings.Default.VisibilityUpdateTime = 0.05f;
            rebuildChunksTimer.HasTriggered = true;

            Water = new WaterManager(this);

            PauseThreads = false;

            Vector3 maxBounds = new Vector3(
                maxChunksX * VoxelConstants.ChunkSizeX / 2.0f,
                maxChunksY * VoxelConstants.ChunkSizeY / 2.0f, 
                maxChunksZ * VoxelConstants.ChunkSizeZ / 2.0f);
            Vector3 minBounds = -maxBounds;
            Bounds = new BoundingBox(minBounds, maxBounds);

        }

        public void StartThreads()
        {
            RebuildThread.Start();
            WaterUpdateThread.Start();
            ChunkUpdateThread.Start();
        }

        public void RebuildVoxelsThread()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

#if CREATE_CRASH_LOGS
            try
#endif
            {
                while (!DwarfGame.ExitGame && !ExitThreads)
                {
                    RebuildEvent.WaitOne();
                    VoxelChunk chunk = null;
                    do
                    {
                        chunk = PopInvalidChunk();
                        if (chunk != null)
                        {
                            chunk.Rebuild(GameState.Game.GraphicsDevice);
                            NeedsMinimapUpdate = true;
                        }
                    }
                    while (chunk != null);
                    
                }
            }
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
                throw;
            }
#endif           
        }

        private readonly ChunkData chunkData;


        // Todo: Why isn't this part of the chunk generator?
        public void GenerateOres()
        {
            foreach (VoxelType type in VoxelLibrary.GetTypes())
            {
                if (type.SpawnClusters || type.SpawnVeins)
                {
                    int numEvents = (int)MathFunctions.Rand(75*(1.0f - type.Rarity), 100*(1.0f - type.Rarity));
                    for (int i = 0; i < numEvents; i++)
                    {
                        BoundingBox clusterBounds = new BoundingBox
                        {
                            Max = new Vector3(Bounds.Max.X, type.MaxSpawnHeight, Bounds.Max.Z),
                            Min = new Vector3(Bounds.Min.X, Math.Max(type.MinSpawnHeight, 2), Bounds.Min.Z)
                        };

                        if (type.SpawnClusters)
                        {

                            OreCluster cluster = new OreCluster()
                            {
                                Size =
                                    new Vector3(MathFunctions.Rand(type.ClusterSize*0.25f, type.ClusterSize),
                                        MathFunctions.Rand(type.ClusterSize*0.25f, type.ClusterSize),
                                        MathFunctions.Rand(type.ClusterSize*0.25f, type.ClusterSize)),
                                Transform = MathFunctions.RandomTransform(clusterBounds),
                                Type = type
                            };
                            ChunkGen.GenerateCluster(cluster, ChunkData);
                        }

                        if (type.SpawnVeins)
                        {
                            OreVein vein = new OreVein()
                            {
                                Length = MathFunctions.Rand(type.VeinLength*0.75f, type.VeinLength*1.25f),
                                Start = MathFunctions.RandVector3Box(clusterBounds),
                                Type = type
                            };
                            ChunkGen.GenerateVein(vein, ChunkData);
                        }
                    }
                }
            }
        }

        // Todo: Move to ChunkGenerator
        public void GenerateInitialChunks(GlobalChunkCoordinate origin, Action<String> SetLoadingMessage)
        {
            var initialChunkCoordinates = new List<GlobalChunkCoordinate>();

            for (int dx = 0; dx < WorldSize.X; dx++)
                for (int dz = 0; dz < WorldSize.Z; dz++)
                    initialChunkCoordinates.Add(new GlobalChunkCoordinate(dx, 0, dz));
                    
            SetLoadingMessage("Generating Chunks...");

            foreach (var box in initialChunkCoordinates)
            {
                Vector3 worldPos = new Vector3(
                    box.X * VoxelConstants.ChunkSizeX,
                    box.Y * VoxelConstants.ChunkSizeY,
                    box.Z * VoxelConstants.ChunkSizeZ);
                VoxelChunk chunk = ChunkGen.GenerateChunk(worldPos, World);
                ChunkData.AddChunk(chunk);
            }

            // This is critical at the beginning to allow trees to spawn on ramps correctly,
            // and also to ensure no inconsistencies in chunk geometry due to ramps.
            foreach (var chunk in ChunkData.ChunkMap)
            {
                ChunkGen.GenerateCaves(chunk, World);
                for (var i = 0; i < VoxelConstants.ChunkSizeY; ++i)
                {
                    // Update corner ramps on all chunks so that they don't have seams when they 
                    // are initially built.
                    //VoxelListPrimitive.UpdateCornerRamps(chunk, i);
                    chunk.InvalidateSlice(i);
                }

            }
            RecalculateBounds();
            SetLoadingMessage("Generating Ores...");

            GenerateOres();
            NeedsMinimapUpdate = true;
        }

        private void RecalculateBounds()
        {
            List<BoundingBox> boxes = ChunkData.GetChunkEnumerator().Select(c => c.GetBoundingBox()).ToList();
            Bounds = MathFunctions.GetBoundingBox(boxes);
        }

        private IEnumerable<VoxelChunk> EnumerateAdjacentChunks(VoxelChunk Chunk)
        {
            for (int dx = -1; dx < 2; dx++)
                for (int dz = -1; dz < 2; dz++)
                    if (dx != 0 || dz != 0)
                    {
                        var adjacentCoord = new GlobalChunkCoordinate(
                            Chunk.ID.X + dx, 0, Chunk.ID.Z + dz);
                        if (ChunkData.CheckBounds(adjacentCoord))
                            yield return ChunkData.GetChunk(adjacentCoord);
                    }
        }

        public void UpdateChunks()
        {
            while(true)
            {
                if (!DwarfTime.LastTime.IsPaused)
                {
                    ChunkUpdateTimer.Update(DwarfTime.LastTime);
                    if (ChunkUpdateTimer.HasTriggered)
                    {
                        ChunkUpdate.RunUpdate(this);
                    }
                }
                Thread.Sleep(100);
            }
        }

        public void Update(DwarfTime gameTime, Camera camera, GraphicsDevice g)
        {
            foreach (var chunk in ChunkData.GetChunkEnumerator())
                chunk.RecieveNewPrimitive(gameTime);

            List<VoxelChangeEvent> localList = null;
            lock (ChangedVoxels)
            {
                localList = ChangedVoxels;
                ChangedVoxels = new List<VoxelChangeEvent>();
            }

            foreach (var voxel in localList)
            {
                // Todo: Can this be a more generic mechanism?
                if (voxel.Type == VoxelChangeEventType.VoxelTypeChanged && voxel.NewVoxelType == VoxelLibrary.emptyType.ID)
                {
                    var guardDesignation = World.PlayerFaction.Designations.GetVoxelDesignation(voxel.Voxel, DesignationType.Guard);
                    if (guardDesignation != null)
                        World.Master.TaskManager.CancelTask(guardDesignation.Task);
                }
                
                // Todo: Design generic voxel hooks and run here.
                
                var box = voxel.Voxel.GetBoundingBox();
                var hashmap = World.EnumerateIntersectingObjects(box, CollisionType.Both);

                foreach (var intersectingBody in hashmap)
                {
                    var listener = intersectingBody as IVoxelListener;
                    if (listener != null)
                        listener.OnVoxelChanged(voxel);
                }
            }
        }

        public void UpdateBounds()
        {
            var boundingBoxes = chunkData.GetChunkEnumerator().Select(c => c.GetBoundingBox());
            Bounds = MathFunctions.GetBoundingBox(boundingBoxes);
        }

        public void Destroy()
        {
            PauseThreads = true;
            ExitThreads = true;
            RebuildThread.Abort();
            WaterUpdateThread.Join();
            ChunkUpdateThread.Abort();
        }

        public List<Body> KillVoxel(VoxelHandle Voxel)
        {
            if (World.Master != null)
                World.Master.Faction.OnVoxelDestroyed(Voxel);

            if (!Voxel.IsValid || Voxel.IsEmpty)
                return null;

            if (World.ParticleManager != null)
            {
                World.ParticleManager.Trigger(Voxel.Type.ParticleType, 
                    Voxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
                World.ParticleManager.Trigger("puff", 
                    Voxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
            }

            Voxel.Type.ExplosionSound.Play(Voxel.WorldPosition);

            List<Body> emittedResources = null;
            if (Voxel.Type.ReleasesResource)
            {
                if (MathFunctions.Rand() < Voxel.Type.ProbabilityOfRelease)
                {
                    emittedResources = new List<Body>
                    {
                        EntityFactory.CreateEntity<Body>(Voxel.Type.ResourceToRelease + " Resource",
                            Voxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f))
                    };
                }
            }

            Voxel.Type = VoxelLibrary.emptyType;

            return emittedResources;
        }

    }
}
