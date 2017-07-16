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
        public ConcurrentQueue<VoxelChunk> RenderList { get; set; }
        public ConcurrentQueue<VoxelChunk> RebuildList { get; set; }
        public ConcurrentQueue<VoxelChunk> RebuildLiquidsList { get; set; }

        public Point3 WorldSize { get; set; } 

        public ChunkGenerator ChunkGen { get; set; }
        public ConcurrentQueue<VoxelChunk> GeneratedChunks { get; set; }
        public List<GlobalChunkCoordinate> ToGenerate { get; set; }

        private Thread GeneratorThread { get; set; }

        private Thread RebuildThread { get; set; }
        private Thread RebuildLiquidThread { get; set; }


        private static readonly AutoResetEvent WaterUpdateEvent = new AutoResetEvent(true);
        private static readonly AutoResetEvent NeedsGenerationEvent = new AutoResetEvent(false);
        private static readonly AutoResetEvent NeedsRebuildEvent = new AutoResetEvent(false);
        private static readonly AutoResetEvent NeedsLiquidEvent = new AutoResetEvent(false);

        private Thread WaterThread { get; set; }

        private readonly Timer generateChunksTimer = new Timer(0.5f, false, Timer.TimerMode.Real);
        private readonly Timer waterUpdateTimer = new Timer(0.15f, false, Timer.TimerMode.Real);

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

        public ChunkData ChunkData
        {
            get { return chunkData; }
        }

        public List<TemporaryVoxelHandle> KilledVoxels { get; set; }

        public ChunkManager(ContentManager content, 
            WorldManager world,
            Camera camera, GraphicsDevice graphics,
            ChunkGenerator chunkGen, int maxChunksX, int maxChunksY, int maxChunksZ)
        {
            World = world;
            KilledVoxels = new List<TemporaryVoxelHandle>();
            ExitThreads = false;
            drawDistSq = DrawDistance * DrawDistance;
            Content = content;

            chunkData = new ChunkData(this);
            RenderList = new ConcurrentQueue<VoxelChunk>();
            RebuildList = new ConcurrentQueue<VoxelChunk>();
            RebuildLiquidsList = new ConcurrentQueue<VoxelChunk>();
            ChunkGen = chunkGen;

            GeneratedChunks = new ConcurrentQueue<VoxelChunk>();
            GeneratorThread = new Thread(GenerateThread);
            GeneratorThread.Name = "Generate";

            RebuildThread = new Thread(RebuildVoxelsThread);
            RebuildThread.Name = "RebuildVoxels";
            RebuildLiquidThread = new Thread(RebuildLiquidsThread);
            RebuildLiquidThread.Name = "RebuildLiquids";

            WaterThread = new Thread(UpdateWaterThread);
            WaterThread.Name = "UpdateWater";

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

            WorldSize = new Point3(maxChunksX, maxChunksY, maxChunksZ);

            Vector3 maxBounds = new Vector3(
                maxChunksX * VoxelConstants.ChunkSizeX / 2.0f,
                maxChunksY * VoxelConstants.ChunkSizeY / 2.0f, 
                maxChunksZ * VoxelConstants.ChunkSizeZ / 2.0f);
            Vector3 minBounds = -maxBounds;
            Bounds = new BoundingBox(minBounds, maxBounds);
        }

        public void StartThreads()
        {
            GeneratorThread.Start();
            RebuildThread.Start();
            WaterThread.Start();
            RebuildLiquidThread.Start();
        }

        public void UpdateWaterThread()
        {
            EventWaitHandle[] waitHandles =
            {
                WaterUpdateEvent,
                Program.ShutdownEvent
            };

            GamePerformance.Instance.RegisterThreadLoopTracker("UpdateWater", GamePerformance.ThreadIdentifier.UpdateWater);
#if CREATE_CRASH_LOGS
            try
#endif
            {
                while (!DwarfGame.ExitGame && !ExitThreads)
                {
                    EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                    GamePerformance.Instance.PreThreadLoop(GamePerformance.ThreadIdentifier.UpdateWater);
                    GamePerformance.Instance.EnterZone("UpdateWater");
                    if (wh == Program.ShutdownEvent)
                    {
                        break;
                    }

                    if (!PauseThreads)
                    {
                        Water.UpdateWater();
                    }
                    GamePerformance.Instance.PostThreadLoop(GamePerformance.ThreadIdentifier.UpdateWater);
                    GamePerformance.Instance.ExitZone("UpdateWater");
                }
            }
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }
#endif
        }

        public void RebuildLiquidsThread()
        {
            EventWaitHandle[] waitHandles =
            {
                NeedsLiquidEvent,
                Program.ShutdownEvent
            };

            GamePerformance.Instance.RegisterThreadLoopTracker("RebuildLiquids", GamePerformance.ThreadIdentifier.RebuildWater);

#if CREATE_CRASH_LOGS
            try
#endif
            {
                bool shouldExit = false;
                while (!shouldExit && !DwarfGame.ExitGame && !ExitThreads)
                {
                    EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                    if (wh == Program.ShutdownEvent)
                    {
                        break;
                    }

                    GamePerformance.Instance.PreThreadLoop(GamePerformance.ThreadIdentifier.RebuildWater);
                    while (!PauseThreads && RebuildLiquidsList.Count > 0)
                    {
                        VoxelChunk chunk = null;

                        //LiquidLock.WaitOne();
                        if (!RebuildLiquidsList.TryDequeue(out chunk))
                        {
                            //LiquidLock.ReleaseMutex();
                            break;
                        }
                        //LiquidLock.ReleaseMutex();

                        if (chunk == null)
                        {
                            continue;
                        }

                        try
                        {
                            chunk.RebuildLiquids();
                            chunk.RebuildLiquidPending = false;
                            chunk.ShouldRebuildWater = false;
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e.Message);
                            shouldExit = true;
                            break;
                        }
                    }
                    GamePerformance.Instance.PostThreadLoop(GamePerformance.ThreadIdentifier.RebuildWater);
                }
            }
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }
#endif

        }

        public void RebuildVoxelsThread()
        {
            EventWaitHandle[] waitHandles =
            {
                NeedsRebuildEvent,
                Program.ShutdownEvent
            };

            GamePerformance.Instance.RegisterThreadLoopTracker("RebuildVoxels", GamePerformance.ThreadIdentifier.RebuildVoxels);

#if CREATE_CRASH_LOGS
            try
#endif
            {
                while (!DwarfGame.ExitGame && !ExitThreads)
                {
                    EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                    GamePerformance.Instance.PreThreadLoop(GamePerformance.ThreadIdentifier.RebuildVoxels);
                    GamePerformance.Instance.EnterZone("RebuildVoxels");

                    if (wh == Program.ShutdownEvent)
                    {
                        break;
                    }
                    {
                        if (PauseThreads)
                        {
                            continue;
                        }

                        var toRebuild = new Dictionary<GlobalChunkCoordinate, VoxelChunk>();
                        bool calculateRamps = GameSettings.Default.CalculateRamps;

                        lock (RebuildList)
                        {
                            while (RebuildList.Count > 0)
                            {
                                VoxelChunk chunk = null;

                                if (!RebuildList.TryDequeue(out chunk))
                                {
                                    continue;
                                }

                                if (chunk == null)
                                {
                                    continue;
                                }

                                toRebuild[chunk.ID] = chunk;

                                if (PauseThreads)
                                {
                                    break;
                                }
                            }
                        }

                        if (calculateRamps)
                        {
                            foreach (VoxelChunk chunk in toRebuild.Select(chunkPair => chunkPair.Value))
                            {
                                chunk.UpdateRamps();
                            }
                        }

                        foreach (
                            VoxelChunk chunk in
                                toRebuild.Select(chunkPair => chunkPair.Value)
                                    .Where(chunk => chunk.ShouldRecalculateLighting))
                        {
                            chunk.CalculateGlobalLight();
                        }

                        foreach (VoxelChunk chunk in toRebuild.Select(chunkPair => chunkPair.Value))
                        {
                            if (chunk.RebuildPending && chunk.ShouldRebuild)
                            {
                                /*
                                if (chunk.ShouldRecalculateLighting)
                                {
                                    chunk.CalculateVertexLighting();
                                }
                                 */
                                chunk.Rebuild(Graphics);
                                chunk.ShouldRebuild = false;
                                chunk.RebuildPending = false;
                                chunk.ShouldRecalculateLighting = false;
                            }
                            else
                            {
                                chunk.RebuildPending = false;
                            }
                        }
                        
                    }
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

        public int CompareChunkDistance(VoxelChunk a, VoxelChunk b)
        {
            if(a == b || !a.IsVisible && !b.IsVisible)
            {
                return 0;
            }

            if(!a.IsVisible)
            {
                return 1;
            }

            if(!b.IsVisible)
            {
                return -1;
            }

            float dA = (a.Origin - camera.Position + new Vector3(VoxelConstants.ChunkSizeX / 2.0f, VoxelConstants.ChunkSizeY / 2.0f, VoxelConstants.ChunkSizeZ / 2.0f)).LengthSquared();
            float dB = (b.Origin - camera.Position + new Vector3(VoxelConstants.ChunkSizeX / 2.0f, VoxelConstants.ChunkSizeY / 2.0f, VoxelConstants.ChunkSizeZ / 2.0f)).LengthSquared();

            if (!camera.GetFrustrum().Intersects(a.GetBoundingBox()))
            {
                dA *= 100;
            }

            if (!camera.GetFrustrum().Intersects(b.GetBoundingBox()))
            {
                dB *= 100;
            }


            if(dA < dB)
            {
                return -1;
            }

            return 1;
        }

        private readonly ChunkData chunkData;

        public void UpdateRebuildList()
        {
            List<VoxelChunk> toRebuild = new List<VoxelChunk>();
            List<VoxelChunk> toRebuildLiquids = new List<VoxelChunk>();

            foreach (VoxelChunk chunk in ChunkData.ChunkMap.Select(chunks => chunks.Value))
            {
                if(chunk.ShouldRebuild && ! chunk.RebuildPending)
                {
                    toRebuild.Add(chunk);
                    chunk.RebuildPending = true;
                }

                if(chunk.ShouldRebuildWater && ! chunk.RebuildLiquidPending)
                {
                    toRebuildLiquids.Add(chunk);
                    chunk.RebuildLiquidPending = true;
                }
            }


            if(toRebuild.Count > 0)
            {
                toRebuild.Sort(CompareChunkDistance);
                //RebuildLock.WaitOne();
                foreach(VoxelChunk chunk in toRebuild)
                {
                    RebuildList.Enqueue(chunk);
                }
                //RebuildLock.ReleaseMutex();
            }

            if(toRebuildLiquids.Count > 0)
            {
                toRebuildLiquids.Sort(CompareChunkDistance);

                //LiquidLock.WaitOne();
                foreach(VoxelChunk chunk in toRebuildLiquids.Where(chunk => !RebuildLiquidsList.Contains(chunk)))
                {
                    RebuildLiquidsList.Enqueue(chunk);
                }
                //LiquidLock.ReleaseMutex();
            }


            if(RebuildList.Count > 0)
            {
                NeedsRebuildEvent.Set();
            }

            if(RebuildLiquidsList.Count > 0)
            {
                NeedsLiquidEvent.Set();
            }
        }

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
                            if (!ChunkData.ChunkMap.ContainsKey(box))
                            {
                                Vector3 worldPos = new Vector3(
                                    box.X * VoxelConstants.ChunkSizeX, 
                                    box.Y * VoxelConstants.ChunkSizeY,
                                    box.Z * VoxelConstants.ChunkSizeZ);
                                VoxelChunk chunk = ChunkGen.GenerateChunk(worldPos, World);
                                Drawer3D.DrawBox(chunk.GetBoundingBox(), Color.Red, 0.1f);
                                chunk.ShouldRebuild = true;
                                chunk.ShouldRecalculateLighting = true;
                                GeneratedChunks.Enqueue(chunk);
                            }
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
            float origBuildRadius = GenerateDistance;
            GenerateDistance = origBuildRadius * 2.0f;

            var initialChunkCoordinates = new List<GlobalChunkCoordinate>();
            for (int dx = origin.X - WorldSize.X/2 + 1; dx < origin.X + WorldSize.X/2; dx++)
            {
                for (int dy = origin.Y - WorldSize.Y/2; dy <= origin.Y + WorldSize.Y/2; dy++)
                {
                    for (int dz = origin.Z - WorldSize.Z/2 + 1; dz < origin.Z + WorldSize.Z/2; dz++)
                    {
                        initialChunkCoordinates.Add(new GlobalChunkCoordinate(dx, dy, dz));
                    }
                }
            }

            SetLoadingMessage("Generating Chunks...");

            foreach(var box in initialChunkCoordinates)
            {
                if (!ChunkData.ChunkMap.ContainsKey(box))
                {
                    Vector3 worldPos = new Vector3(
                        box.X * VoxelConstants.ChunkSizeX, 
                        box.Y * VoxelConstants.ChunkSizeY, 
                        box.Z * VoxelConstants.ChunkSizeZ);
                    VoxelChunk chunk = ChunkGen.GenerateChunk(worldPos, World);
                    chunk.ShouldRebuild = true;
                    chunk.ShouldRecalculateLighting = true;
                    chunk.IsVisible = true;
                    chunk.ResetSunlight(0);
                    GeneratedChunks.Enqueue(chunk);
                    foreach (VoxelChunk chunk2 in GeneratedChunks)
                    {
                        if (!ChunkData.ChunkMap.ContainsKey(chunk2.ID))
                        {
                            ChunkData.AddChunk(chunk2);
                        }
                    }
                }
            }

            RecalculateBounds();
            SetLoadingMessage("Generating Ores...");

            GenerateOres();

            SetLoadingMessage("Fog of war...");
            // We are going to force fog of war to be on for the first reveal then reset it back to it's previous setting after.
            // This is a pseudo hack to stop worlds created with Fog of War off then looking awful if it is turned back on.
            bool fogOfWar = GameSettings.Default.FogofWar;
            GameSettings.Default.FogofWar = true;
            VoxelHelpers.Reveal(ChunkData, new TemporaryVoxelHandle(
                ChunkData, new GlobalVoxelCoordinate(0, VoxelConstants.ChunkSizeY - 1, 0)));
GameSettings.Default.FogofWar = fogOfWar;

            //UpdateRebuildList();
            GenerateDistance = origBuildRadius;

            while(GeneratedChunks.Count > 0)
            {
                VoxelChunk gen = null;
                if(!GeneratedChunks.TryDequeue(out gen))
                {
                    break;
                }
            }

            ChunkData.ChunkManager.CreateGraphics(SetLoadingMessage, ChunkData);
        }

        private void RecalculateBounds()
        {
            List<BoundingBox> boxes = ChunkData.ChunkMap.Select(chunkPair => chunkPair.Value.GetBoundingBox()).ToList();
            Bounds = MathFunctions.GetBoundingBox(boxes);
        }

        private IEnumerable<VoxelChunk> EnumerateAdjacentChunks(VoxelChunk Chunk)
        {
            for (int dx = -1; dx < 2; dx++)
                for (int dz = -1; dz < 2; dz++)
                    if (dx != 0 || dz != 0)
                    {
                        VoxelChunk adjacent;
                        if (ChunkData.ChunkMap.TryGetValue(new GlobalChunkCoordinate(
                            Chunk.ID.X + dx, 0, Chunk.ID.Z + dz), out adjacent))
                            yield return adjacent;
                    }
        }

        public void Update(DwarfTime gameTime, Camera camera, GraphicsDevice g)
        {
            if (waterUpdateTimer.Update(gameTime))
            {
                WaterUpdateEvent.Set();
            }

            UpdateRebuildList();

            generateChunksTimer.Update(gameTime);
            if(generateChunksTimer.HasTriggered)
            {
                if(ToGenerate.Count > 0)
                {
                    NeedsGenerationEvent.Set();
                }

                foreach(VoxelChunk chunk in GeneratedChunks)
                {
                    if(!ChunkData.ChunkMap.ContainsKey(chunk.ID))
                    {
                        ChunkData.AddChunk(chunk);
                        foreach(var c in EnumerateAdjacentChunks(chunk))
                        {
                            c.ShouldRecalculateLighting = true;
                            c.ShouldRebuild = true;
                        }
                        RecalculateBounds();
                    }
                }

                while(GeneratedChunks.Count > 0)
                {
                    VoxelChunk gen = null;
                    if(!GeneratedChunks.TryDequeue(out gen))
                    {
                        break;
                    }
                }
            }

            foreach(VoxelChunk chunk in ChunkData.ChunkMap.Values)
            {
                chunk.Update(gameTime);
            }

            Water.Splash(gameTime);
            Water.HandleTransfers(gameTime);

            var affectedVoxels = new HashSet<GlobalVoxelCoordinate>();

            foreach (var voxel in KilledVoxels)
            {
                affectedVoxels.Add(voxel.Coordinate);
                voxel.Chunk.NotifyDestroyed(voxel.Coordinate.GetLocalVoxelCoordinate());
            }

            VoxelHelpers.Reveal(ChunkData, KilledVoxels);

            lock (RebuildList)
            {
                foreach (var affected in affectedVoxels)
                    ChunkData.NotifyRebuild(affected);
            }

            KilledVoxels.Clear();
        }

        // Todo: %Kill% or %Lift% - only used by voxel selector.
        public IEnumerable<VoxelHandle> GetVoxelsIntersecting(IEnumerable<Vector3> positions)
        {
            foreach (Vector3 vec in positions)
            {
                VoxelHandle vox = new VoxelHandle();
                bool success = ChunkData.GetVoxel(vec, ref vox);
                if (success)
                {
                    yield return vox;
                }
            }
        }

        public void CreateGraphics(Action<String> SetLoadingMessage, ChunkData chunkData)
        {
            SetLoadingMessage("Creating Graphics");
            List<VoxelChunk> toRebuild = new List<VoxelChunk>();

            while(RebuildList.Count > 0)
            {
                VoxelChunk chunk = null;
                if(!RebuildList.TryDequeue(out chunk))
                {
                    break;
                }

                if(chunk == null)
                {
                    continue;
                }

                toRebuild.Add(chunk);
            }

            SetLoadingMessage("Updating Ramps");
            foreach (var chunk in toRebuild.Where(chunk => GameSettings.Default.CalculateRamps))
            {
                  chunk.UpdateRamps();
            }

            SetLoadingMessage("Calculating lighting ");
            int j = 0;
            foreach(var chunk in toRebuild)
            {
                j++;
                if (chunk.ShouldRecalculateLighting)
                {
                    chunk.CalculateGlobalLight();
                    chunk.ShouldRecalculateLighting = false;
                }
            }

            j = 0;
            SetLoadingMessage("Calculating vertex light ...");
            foreach(VoxelChunk chunk in toRebuild)
            {
                j++;
                //chunk.CalculateVertexLighting();
            };

            SetLoadingMessage("Building Vertices...");
            j = 0;
            foreach(var  chunk in toRebuild)
            {
                j++;
                //SetLoadingMessage("Building Vertices " + j + "/" + toRebuild.Count);

                if (!chunk.ShouldRebuild)
                {
                    return;
                }

                chunk.Rebuild(Graphics);
                chunk.ShouldRebuild = false;
                chunk.RebuildPending = false;
                chunk.RebuildLiquidPending = false;
            };

            SetLoadingMessage("Cleaning Up.");
        }
        
        public void UpdateBounds()
        {
            List<BoundingBox> boundingBoxes = chunkData.ChunkMap.Select(chunkPair => chunkPair.Value.GetBoundingBox()).ToList();
            Bounds = MathFunctions.GetBoundingBox(boundingBoxes);
        }

        public void Destroy()
        {
            PauseThreads = true;
            ExitThreads = true;
            GeneratorThread.Join();
            RebuildLiquidThread.Join();
            RebuildThread.Join();
            WaterThread.Join();
            ChunkData.ChunkMap.Clear();
        }
    }
}
