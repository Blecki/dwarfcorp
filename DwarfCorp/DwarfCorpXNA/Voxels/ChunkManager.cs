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
        //Todo: This belongs in WorldManager!
        private Splasher Splasher;

        private Queue<VoxelChunk> RebuildQueue = new Queue<VoxelChunk>();
        private Mutex RebuildQueueLock = new Mutex();

        public void InvalidateChunk(VoxelChunk Chunk)
        {
            RebuildQueueLock.WaitOne();
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
        public List<GlobalChunkCoordinate> ToGenerate { get; set; }

        private Thread GeneratorThread { get; set; }

        private Thread RebuildThread { get; set; }
        private AutoScaleThread WaterUpdateThread;

        private static readonly AutoResetEvent NeedsGenerationEvent = new AutoResetEvent(false);

        private readonly Timer generateChunksTimer = new Timer(0.5f, false, Timer.TimerMode.Real);
        

        public BoundingBox Bounds { get; set; }

        public float DrawDistance
        {
            get { return GameSettings.Default.ChunkDrawDistance; }
        }

        protected float drawDistSq = 0;

        public float DrawDistanceSquared
        {
            get { return drawDistSq; }
        }

        public float GenerateDistance
        {
            get { return GameSettings.Default.ChunkGenerateDistance; }
            set { GameSettings.Default.ChunkGenerateDistance = value; }
        }

        // Todo: %KILL% Wrong spot for this, but too large to move currently.
        public GraphicsDevice Graphics { get; set; }

        public bool PauseThreads { get; set; }

        public enum SliceMode
        {
            X,
            Y,
            Z
        }
        
        public bool ExitThreads { get; set; }

        public Camera camera = null;
        public WorldManager World { get; set; }
        public ContentManager Content { get; set; }

        public WaterManager Water { get; set; }

        public bool IsAboveCullPlane(BoundingBox Box)
        {
            return Box.Min.Y > (ChunkData.MaxViewingLevel + 5);
        }

        public ChunkData ChunkData
        {
            get { return chunkData; }
        }

        public ChunkManager(ContentManager content, 
            WorldManager world,
            Camera camera, GraphicsDevice graphics,
            ChunkGenerator chunkGen, int maxChunksX, int maxChunksY, int maxChunksZ)
        {
            WorldSize = new Point3(maxChunksX, maxChunksY, maxChunksZ);

            World = world;
            ExitThreads = false;
            drawDistSq = DrawDistance * DrawDistance;
            Content = content;

            chunkData = new ChunkData(this, maxChunksX, maxChunksZ, 0, 0);             

            ChunkGen = chunkGen;

            GeneratorThread = new Thread(GenerateThread);
            GeneratorThread.Name = "Generate";

            RebuildThread = new Thread(RebuildVoxelsThread);
            RebuildThread.Name = "RebuildVoxels";

            WaterUpdateThread = new AutoScaleThread(this, GamePerformance.ThreadIdentifier.UpdateWater,
                (f) => Water.UpdateWater());

            ToGenerate = new List<GlobalChunkCoordinate>();
            Graphics = graphics;

            chunkGen.Manager = this;

            GameSettings.Default.ChunkGenerateTime = 0.5f;
            generateChunksTimer = new Timer(GameSettings.Default.ChunkGenerateTime, false, Timer.TimerMode.Real);
            GameSettings.Default.ChunkRebuildTime = 0.1f;
            Timer rebuildChunksTimer = new Timer(GameSettings.Default.ChunkRebuildTime, false, Timer.TimerMode.Real);
            GameSettings.Default.VisibilityUpdateTime = 0.05f;
            generateChunksTimer.HasTriggered = true;
            rebuildChunksTimer.HasTriggered = true;
            this.camera = camera;

            Water = new WaterManager(this);

            PauseThreads = false;

            Vector3 maxBounds = new Vector3(
                maxChunksX * VoxelConstants.ChunkSizeX / 2.0f,
                maxChunksY * VoxelConstants.ChunkSizeY / 2.0f, 
                maxChunksZ * VoxelConstants.ChunkSizeZ / 2.0f);
            Vector3 minBounds = -maxBounds;
            Bounds = new BoundingBox(minBounds, maxBounds);

            Splasher = new Splasher(this);
        }

        public void StartThreads()
        {
            GeneratorThread.Start();
            RebuildThread.Start();
            WaterUpdateThread.Start();
        }

        public void RebuildVoxelsThread()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            GamePerformance.Instance.RegisterThreadLoopTracker("RebuildVoxels", GamePerformance.ThreadIdentifier.RebuildVoxels);

#if CREATE_CRASH_LOGS
            try
#endif
            {
                while (!DwarfGame.ExitGame && !ExitThreads)
                {
                    GamePerformance.Instance.PreThreadLoop(GamePerformance.ThreadIdentifier.RebuildVoxels);
                    GamePerformance.Instance.EnterZone("RebuildVoxels");

                    var chunk = PopInvalidChunk();
                    if (chunk != null)
                        chunk.Rebuild(Graphics);
                    else
                        System.Threading.Thread.Yield();
                    
                    GamePerformance.Instance.PostThreadLoop(GamePerformance.ThreadIdentifier.RebuildVoxels);
                    GamePerformance.Instance.ExitZone("RebuildVoxels");
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

        public void GenerateThread()
        {
            EventWaitHandle[] waitHandles =
            {
                NeedsGenerationEvent,
                Program.ShutdownEvent
            };

#if CREATE_CRASH_LOGS
            try
#endif
            {
                while (!ExitThreads)
                {
                    EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                    //GeneratorLock.WaitOne();

                    if (!PauseThreads && ToGenerate != null && ToGenerate.Count > 0)
                    {
 
                        System.Threading.Tasks.Parallel.ForEach(ToGenerate, box =>
                        {
                            //if (!ChunkData.CheckBounds(box))
                            //{
                                Vector3 worldPos = new Vector3(
                                    box.X * VoxelConstants.ChunkSizeX, 
                                    box.Y * VoxelConstants.ChunkSizeY,
                                    box.Z * VoxelConstants.ChunkSizeZ);
                                VoxelChunk chunk = ChunkGen.GenerateChunk(worldPos, World);
                                //Drawer3D.DrawBox(chunk.GetBoundingBox(), Color.Red, 0.1f, false);
                            //}
                        });
                        ToGenerate.Clear();
                    }


                    //GeneratorLock.ReleaseMutex();
                    if (wh == Program.ShutdownEvent)
                    {
                        break;
                    }
                }
            }
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }
#endif           
        }
        
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
                            Min = new Vector3(Bounds.Min.X, type.MinSpawnHeight, Bounds.Min.Z)
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

        public void GenerateInitialChunks(GlobalChunkCoordinate origin, Action<String> SetLoadingMessage)
        {
            // todo: Since the world isn't infinite we can get rid of this.
            float origBuildRadius = GenerateDistance;
            GenerateDistance = origBuildRadius * 2.0f;

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
                chunk.IsVisible = true;
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

            GenerateDistance = origBuildRadius;
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

        public void Update(DwarfTime gameTime, Camera camera, GraphicsDevice g)
        {
            generateChunksTimer.Update(gameTime);
            if(generateChunksTimer.HasTriggered)
            {
                if(ToGenerate.Count > 0)
                {
                    NeedsGenerationEvent.Set();
                }
            }

            foreach (var chunk in ChunkData.GetChunkEnumerator())
                chunk.RecieveNewPrimitive(gameTime);

            // Todo: This belongs up in world manager.
            Splasher.Splash(gameTime, Water.GetSplashQueue());
            Splasher.HandleTransfers(gameTime, Water.GetTransferQueue());

            if (!gameTime.IsPaused)
                ChunkUpdate.RunUpdate(this);

            List<VoxelChangeEvent> localList = null;
            lock (ChangedVoxels)
            {
                localList = ChangedVoxels;
                ChangedVoxels = new List<VoxelChangeEvent>();
            }

            foreach (var voxel in localList)
            {
                var box = voxel.Voxel.GetBoundingBox();
                var hashmap = new HashSet<IBoundedObject>(World.CollisionManager.EnumerateIntersectingObjects(box, CollisionManager.CollisionType.Both));

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
            GeneratorThread.Join();
            RebuildThread.Join();
            WaterUpdateThread.Join();
            //ChunkData.ChunkMap.Clear();
        }

        public List<Body> KillVoxel(VoxelHandle Voxel)
        {
            if (!Voxel.IsValid || Voxel.IsEmpty)
                return null;

            if (World.ParticleManager != null)
            {
                World.ParticleManager.Trigger(Voxel.Type.ParticleType, 
                    Voxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
                World.ParticleManager.Trigger("puff", 
                    Voxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
            }

            if (World.Master != null)
                World.Master.Faction.OnVoxelDestroyed(Voxel);

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
