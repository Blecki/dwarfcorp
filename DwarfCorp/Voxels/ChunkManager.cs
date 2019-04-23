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

        private Thread RebuildThread { get; set; }
        private Thread ChunkUpdateThread { get; set; }
        private AutoScaleThread WaterUpdateThread;

        public BoundingBox Bounds { get; set; }

        public bool PauseThreads { get; set; }

        public bool ExitThreads { get; set; }

        public WorldManager World { get; set; }
        public ContentManager Content { get; set; }

        public WaterManager Water { get; set; }

        public Timer ChunkUpdateTimer = new Timer(0.1f, false, Timer.TimerMode.Game);

        // Todo: Move this.
        public bool IsAboveCullPlane(BoundingBox Box)
        {
            return Box.Min.Y > (World.Master.MaxViewingLevel + 5);
        }

        public ChunkData ChunkData
        {
            get { return chunkData; }
        }

        public VoxelHandle CreateVoxelHandle(GlobalVoxelCoordinate Coordinate)
        {
            return new VoxelHandle(ChunkData, Coordinate);
        }

        public ChunkManager(ContentManager content, 
            WorldManager world, Point3 WorldSizeInChunks)
        {
            this.WorldSize = WorldSizeInChunks;

            World = world;
            ExitThreads = false;
            Content = content;

            chunkData = new ChunkData(Point3.Zero, WorldSize);             

            RebuildThread = new Thread(RebuildVoxelsThread) { IsBackground = true };
            RebuildThread.Name = "RebuildVoxels";

            WaterUpdateThread = new AutoScaleThread(this, (f) => Water.UpdateWater());
            this.ChunkUpdateThread = new Thread(UpdateChunks) { IsBackground = true, Name = "Update Chunks" };

            GameSettings.Default.VisibilityUpdateTime = 0.05f;

            Water = new WaterManager(this);

            PauseThreads = false;

            Vector3 maxBounds = new Vector3(
                WorldSize.X * VoxelConstants.ChunkSizeX / 2.0f,
                WorldSize.Y * VoxelConstants.ChunkSizeY / 2.0f, 
                WorldSize.Z * VoxelConstants.ChunkSizeZ / 2.0f);
            Vector3 minBounds = -maxBounds; // Todo: Can this just be 0,0,0?
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
            Console.Out.WriteLine("Starting chunk regeneration thread.");
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var liveChunks = new List<VoxelChunk>();

#if !DEBUG
            try
#endif
            {
                while (!DwarfGame.ExitGame && !ExitThreads)
                {
                    try
                    {
                        RebuildEvent.WaitOne();
                    }
                    catch (ThreadAbortException exception)
                    {
                        continue;
                    }

                    VoxelChunk chunk = null;

                    do
                    {
                        chunk = PopInvalidChunk();
                        if (chunk != null)
                        {
                            if (!chunk.Visible) continue; // Don't bother rebuilding chunks that won't be rendered.
                            chunk.Rebuild(GameState.Game.GraphicsDevice);

                            liveChunks.Add(chunk);
                            while (liveChunks.Count() > GameSettings.Default.MaxLiveChunks)
                            {
                                liveChunks.Sort((a, b) => a.RenderCycleWhenLastVisible - b.RenderCycleWhenLastVisible);
                                if (liveChunks[0].Visible) break;
                                liveChunks[0].DiscardPrimitive();
                                liveChunks.RemoveAt(0);
                            }

                            NeedsMinimapUpdate = true;
                        }
                    }
                    while (chunk != null);

                    PerformanceMonitor.SetMetric("VISIBLE CHUNKS", liveChunks.Count);
                }
            }
#if !DEBUG
            catch (Exception exception)
            {
                Console.Out.WriteLine("Chunk regeneration thread exited due to an exception.");
                ProgramData.WriteExceptionLog(exception);
                throw;
            }
#endif       
            Console.Out.WriteLine(String.Format("Chunk regeneration thread exited cleanly Exit Game: {0} Exit Thread: {1}.", DwarfGame.ExitGame, ExitThreads));
        }

        private readonly ChunkData chunkData;

        public void GenerateAllGeometry()
        {
            while (RebuildQueue.Count > 0)
            {
                var chunk = RebuildQueue.Dequeue();
                chunk.Rebuild(GameState.Game.GraphicsDevice);
            }
        }

        public void RecalculateBounds()
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
            while(!ExitThreads && !DwarfGame.ExitGame)
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
            List<VoxelChangeEvent> localList = null;
            lock (ChangedVoxels)
            {
                localList = ChangedVoxels;
                ChangedVoxels = new List<VoxelChangeEvent>();
            }

            foreach (var voxel in localList)
            {
                var box = voxel.Voxel.GetBoundingBox();
                var hashmap = World.EnumerateIntersectingObjects(box, CollisionType.Both);

                foreach (var intersectingBody in hashmap)
                {
                    var listener = intersectingBody as IVoxelListener;
                    if (listener != null)
                        listener.OnVoxelChanged(voxel);
                }

                World.Master.TaskManager.OnVoxelChanged(voxel);
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
            RebuildEvent.Set();
            RebuildThread.Join();
            WaterUpdateThread.Join();
            ChunkUpdateThread.Join();
            foreach (var item in ChunkData.ChunkMap)
            {
                item.Destroy();
            }
        }
    }
}
