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
    public partial class ChunkManager
    {
        private Queue<VoxelChunk> RebuildQueue = new Queue<VoxelChunk>();
        private Mutex RebuildQueueLock = new Mutex();
        private AutoResetEvent RebuildEvent = new AutoResetEvent(true);
        public bool NeedsMinimapUpdate = true;

        private Queue<GlobalChunkCoordinate> InvalidColumns = new Queue<GlobalChunkCoordinate>();
        private Mutex InvalidColumnLock = new Mutex();

        public void InvalidateChunk(VoxelChunk Chunk)
        {
            RebuildQueueLock.WaitOne();
            RebuildEvent.Set();
            if (!RebuildQueue.Contains(Chunk))
                RebuildQueue.Enqueue(Chunk);
            RebuildQueueLock.ReleaseMutex();

            EnqueueInvalidColumn(Chunk.ID.X, Chunk.ID.Z);
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

        public void EnqueueInvalidColumn(int X, int Z)
        {
            var columnCoordinate = new GlobalChunkCoordinate(X, 0, Z);
            InvalidColumnLock.WaitOne();
            if (!InvalidColumns.Contains(columnCoordinate))
                InvalidColumns.Enqueue(columnCoordinate);
            InvalidColumnLock.ReleaseMutex();
        }

        public GlobalChunkCoordinate? PopInvalidColumn()
        {
            GlobalChunkCoordinate? result = null;
            InvalidColumnLock.WaitOne();
            if (InvalidColumns.Count > 0)
                result = InvalidColumns.Dequeue();
            InvalidColumnLock.ReleaseMutex();
            return result;
        }

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
            return Box.Min.Y > (World.Renderer.PersistentSettings.MaxViewingLevel + 5);
        }

        public VoxelHandle CreateVoxelHandle(GlobalVoxelCoordinate Coordinate)
        {
            return new VoxelHandle(this, Coordinate);
        }

        public ChunkManager(ContentManager Content, WorldManager World)
        {
            this.Content = Content;
            this.World = World;

            ExitThreads = false;

            InitializeChunkMap(Point3.Zero, World.WorldSizeInChunks);             

            RebuildThread = new Thread(RebuildVoxelsThread) { IsBackground = true };
            RebuildThread.Name = "RebuildVoxels";

            WaterUpdateThread = new AutoScaleThread(this, (f) => Water.UpdateWater());
            this.ChunkUpdateThread = new Thread(UpdateChunks) { IsBackground = true, Name = "Update Chunks" };

            GameSettings.Current.VisibilityUpdateTime = 0.05f;

            Water = new WaterManager(this);

            PauseThreads = false;

            Vector3 maxBounds = new Vector3(
                World.WorldSizeInChunks.X * VoxelConstants.ChunkSizeX / 2.0f,
                World.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY / 2.0f,
                World.WorldSizeInChunks.Z * VoxelConstants.ChunkSizeZ / 2.0f);
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

                            if (liveChunks.Count() > GameSettings.Current.MaxLiveChunks)
                            {
                                liveChunks.Sort((a, b) => a.RenderCycleWhenLastVisible - b.RenderCycleWhenLastVisible);

                                while (liveChunks.Count() > GameSettings.Current.MaxLiveChunks)
                                {
                                    if (liveChunks[0].Visible) break;
                                    liveChunks[0].DiscardPrimitive();
                                    liveChunks.RemoveAt(0);
                                }
                            }

                            NeedsMinimapUpdate = true; // Soon to be redundant.
                        }
                    }
                    while (chunk != null);

                    PerformanceMonitor.SetMetric("VISIBLE CHUNKS", liveChunks.Count);
                }
            }
#if !DEBUG
            catch (Exception exception)
            {
                Console.Out.WriteLine("Chunk regeneration thread encountered an exception.");
                ProgramData.WriteExceptionLog(exception);
                //throw;
            }
#endif       
            Console.Out.WriteLine(String.Format("Chunk regeneration thread exited cleanly Exit Game: {0} Exit Thread: {1}.", DwarfGame.ExitGame, ExitThreads));
        }

        public void RecalculateBounds()
        {
            List<BoundingBox> boxes = GetChunkEnumerator().Select(c => c.GetBoundingBox()).ToList();
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
                        if (CheckBounds(adjacentCoord))
                            yield return GetChunk(adjacentCoord);
                    }
        }

        public void UpdateChunks()
        {
            while(!ExitThreads && !DwarfGame.ExitGame)
            {
                if (!DwarfTime.LastTimeX.IsPaused)
                {
                    ChunkUpdateTimer.Update(DwarfTime.LastTimeX);
                    if (ChunkUpdateTimer.HasTriggered)
                    {
                        ChunkUpdate.RunUpdate(this);
                    }
                }
                Thread.Sleep(100);
            }
        }

        public List<VoxelChangeEvent> GetAndClearChangedVoxelList()
        {
            List<VoxelChangeEvent> localList = null;
            lock (ChangedVoxels)
            {
                localList = ChangedVoxels;
                ChangedVoxels = new List<VoxelChangeEvent>();
            }
            return localList;
        }

        public void UpdateBounds()
        {
            var boundingBoxes = GetChunkEnumerator().Select(c => c.GetBoundingBox());
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
            foreach (var item in ChunkMap)
                item.Destroy();
        }
    }
}
