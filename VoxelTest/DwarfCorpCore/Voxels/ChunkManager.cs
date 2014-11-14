﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorpCore;
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
        public List<Point3> ToGenerate { get; set; }

        private Thread GeneratorThread { get; set; }

        private Thread RebuildThread { get; set; }
        private Thread RebuildLiquidThread { get; set; }


        private static readonly AutoResetEvent WaterUpdateEvent = new AutoResetEvent(true);
        private static readonly AutoResetEvent NeedsGenerationEvent = new AutoResetEvent(false);
        private static readonly AutoResetEvent NeedsRebuildEvent = new AutoResetEvent(false);
        private static readonly AutoResetEvent NeedsLiquidEvent = new AutoResetEvent(false);

        private Thread WaterThread { get; set; }

        private readonly Timer generateChunksTimer = new Timer(0.5f, false);
        private readonly Timer visibilityChunksTimer = new Timer(0.03f, false);
        private readonly Timer waterUpdateTimer = new Timer(0.1f, false);

        public BoundingBox Bounds { get; set; }

        public float DrawDistance
        {
            get { return GameSettings.Default.ChunkDrawDistance; }
            set
            {
                GameSettings.Default.ChunkDrawDistance = value;
                drawDistSq = value * value;
            }
        }

        protected float drawDistSq = 0;

        public float DrawDistanceSquared
        {
            get { return drawDistSq; }
            set
            {
                drawDistSq = value;
                GameSettings.Default.ChunkDrawDistance = (float) Math.Sqrt(value);
            }
        }

        public float RemoveDistance
        {
            get { return GameSettings.Default.ChunkUnloadDistance; }
            set { GameSettings.Default.ChunkDrawDistance = value; }
        }

        public float GenerateDistance
        {
            get { return GameSettings.Default.ChunkGenerateDistance; }
            set { GameSettings.Default.ChunkDrawDistance = value; }
        }

        public GraphicsDevice Graphics { get; set; }

        public bool PauseThreads { get; set; }

        public enum SliceMode
        {
            X,
            Y,
            Z
        }


        public bool ExitThreads { get; set; }

        private Camera camera = null;

        public ComponentManager Components { get { return PlayState.ComponentManager; }}
        public ContentManager Content { get; set; }

        private readonly HashSet<VoxelChunk> visibleSet = new HashSet<VoxelChunk>();

        public WaterManager Water { get; set; }

        public List<DynamicLight> DynamicLights { get; set; }

        public ChunkData ChunkData
        {
            get { return chunkData; }
        }

        public ChunkManager(ContentManager content, 
            uint chunkSizeX, uint chunkSizeY, uint chunkSizeZ, 
            Camera camera, GraphicsDevice graphics, Texture2D tilemap, 
            Texture2D illumMap, Texture2D sunMap, Texture2D ambientMap, 
            Texture2D torchMap, ChunkGenerator chunkGen, int maxChunksX, int maxChunksY, int maxChunksZ)
        {
            ExitThreads = false;
            drawDistSq = DrawDistance * DrawDistance;
            Content = content;

            chunkData = new ChunkData(chunkSizeX, chunkSizeY, chunkSizeZ, 1.0f / chunkSizeX, 1.0f / chunkSizeY, 1.0f / chunkSizeZ, tilemap, illumMap, this);
            ChunkData.ChunkMap = new ConcurrentDictionary<Point3, VoxelChunk>();
            RenderList = new ConcurrentQueue<VoxelChunk>();
            RebuildList = new ConcurrentQueue<VoxelChunk>();
            RebuildLiquidsList = new ConcurrentQueue<VoxelChunk>();
            ChunkGen = chunkGen;


            GeneratedChunks = new ConcurrentQueue<VoxelChunk>();
            GeneratorThread = new Thread(GenerateThread);

            RebuildThread = new Thread(RebuildVoxelsThread);
            RebuildLiquidThread = new Thread(RebuildLiquidsThread);

            WaterThread = new Thread(UpdateWaterThread);

            ToGenerate = new List<Point3>();
            Graphics = graphics;

            chunkGen.Manager = this;

            ChunkData.MaxViewingLevel = chunkSizeY;

            GameSettings.Default.ChunkGenerateTime = 0.5f;
            generateChunksTimer = new Timer(GameSettings.Default.ChunkGenerateTime, false);
            GameSettings.Default.ChunkRebuildTime = 0.1f;
            Timer rebuildChunksTimer = new Timer(GameSettings.Default.ChunkRebuildTime, false);
            GameSettings.Default.VisibilityUpdateTime = 0.05f;
            visibilityChunksTimer = new Timer(GameSettings.Default.VisibilityUpdateTime, false);
            generateChunksTimer.HasTriggered = true;
            rebuildChunksTimer.HasTriggered = true;
            visibilityChunksTimer.HasTriggered = true;
            this.camera = camera;

            Water = new WaterManager(this);

            ChunkData.SunMap = sunMap;
            ChunkData.AmbientMap = ambientMap;
            ChunkData.TorchMap = torchMap;

            DynamicLights = new List<DynamicLight>();

            ChunkData.Slice = SliceMode.Y;
            PauseThreads = false;

            WorldSize = new Point3(maxChunksX, maxChunksY, maxChunksZ);

            Vector3 maxBounds = new Vector3(maxChunksX * chunkSizeX * 0.5f, maxChunksY * chunkSizeY * 0.5f, maxChunksZ * chunkSizeZ * 0.5f);
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

            try
            {
                while (!DwarfGame.ExitGame && !ExitThreads)
                {
                    EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                    if (wh == Program.ShutdownEvent)
                    {
                        break;
                    }

                    if (!PauseThreads)
                    {
                        Water.UpdateWater();
                    }
                }
            }
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }
        }

        public void RebuildLiquidsThread()
        {
            EventWaitHandle[] waitHandles =
            {
                NeedsLiquidEvent,
                Program.ShutdownEvent
            };

            try
            {
                bool shouldExit = false;
                while (!shouldExit && !DwarfGame.ExitGame && !ExitThreads)
                {
                    EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                    if (wh == Program.ShutdownEvent)
                    {
                        break;
                    }

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
                            chunk.RebuildLiquids(Graphics);
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
                }
            }
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }

        }


        public void RebuildVoxelsThread()
        {
            EventWaitHandle[] waitHandles =
            {
                NeedsRebuildEvent,
                Program.ShutdownEvent
            };

            try
            {
                while (!DwarfGame.ExitGame && !ExitThreads)
                {
                    EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                    if (wh == Program.ShutdownEvent)
                    {
                        break;
                    }
                    {
                        if (PauseThreads)
                        {
                            continue;
                        }

                        Dictionary<Point3, VoxelChunk> toRebuild = new Dictionary<Point3, VoxelChunk>();
                        bool calculateRamps = GameSettings.Default.CalculateRamps;
                        while (RebuildList.Count > 0)
                        {
                            VoxelChunk chunk = null;

                            if (!RebuildList.TryDequeue(out chunk))
                            {
                                break;
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

                        if (calculateRamps)
                        {
                            foreach (VoxelChunk chunk in toRebuild.Select(chunkPair => chunkPair.Value))
                            {
                                chunk.UpdateRamps();
                            }
                        }


                        foreach (VoxelChunk chunk in toRebuild.Select(chunkPair => chunkPair.Value).Where(chunk => chunk.ShouldRecalculateLighting))
                        {
                            chunk.CalculateGlobalLight();
                        }

                        foreach (VoxelChunk chunk in toRebuild.Select(chunkPair => chunkPair.Value))
                        {
                            if (chunk.RebuildPending && chunk.ShouldRebuild)
                            {
                                chunk.UpdateMaxViewingLevel();

                                if (chunk.ShouldRecalculateLighting)
                                {
                                    chunk.CalculateVertexLighting();
                                }
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
                }
            }
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
                throw;
            }
           
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

            float dA = (a.Origin - camera.Position + new Vector3(a.SizeX / 2.0f, a.SizeY / 2.0f, a.SizeZ / 2.0f)).LengthSquared();
            float dB = (b.Origin - camera.Position + new Vector3(b.SizeX / 2.0f, b.SizeY / 2.0f, b.SizeZ / 2.0f)).LengthSquared();

            if(dA < dB)
            {
                return -1;
            }

            return 1;
        }


        public void UpdateRenderList(Camera camera)
        {
            while(RenderList.Count > 0)
            {
                VoxelChunk result;
                if(!RenderList.TryDequeue(out result))
                {
                    break;
                }
            }


            foreach(VoxelChunk chunk in visibleSet)
            {
                BoundingBox box = chunk.GetBoundingBox();

                if((camera.Position - (box.Min + box.Max) * 0.5f).Length() < DrawDistance)
                {
                    chunk.IsVisible = true;
                    RenderList.Enqueue(chunk);
                }
                else
                {
                    chunk.IsVisible = false;
                }
            }
        }

        public List<VoxelChunk> RebuildTest = new List<VoxelChunk>();
        private readonly ChunkData chunkData;

        public void UpdateRebuildList()
        {
            List<VoxelChunk> toRebuild = new List<VoxelChunk>();
            List<VoxelChunk> toRebuildLiquids = new List<VoxelChunk>();
            RebuildTest = toRebuild;
            foreach(VoxelChunk chunk in ChunkData.ChunkMap.Select(chunks => chunks.Value))
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

        public bool BoundingBoxIntersectsWorld(BoundingBox box)
        {
            HashSet<VoxelChunk> chunksIntersecting = new HashSet<VoxelChunk>();
            GetChunksIntersecting(box, chunksIntersecting);

            return chunksIntersecting.Count > 0 || GeneratedChunks.Any(chunk => chunk.GetBoundingBox().Intersects(box));
        }

        public void GenerateThread()
        {
            EventWaitHandle[] waitHandles =
            {
                NeedsGenerationEvent,
                Program.ShutdownEvent
            };

            try
            {
                while (!ExitThreads)
                {
                    EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                    //GeneratorLock.WaitOne();


                    if (!PauseThreads && ToGenerate != null && ToGenerate.Count > 0)
                    {
                        Point3 box = ToGenerate[0];

                        if (!ChunkData.ChunkMap.ContainsKey(box))
                        {
                            Vector3 worldPos = new Vector3(box.X * ChunkData.ChunkSizeX, box.Y * ChunkData.ChunkSizeY, box.Z * ChunkData.ChunkSizeZ);
                            VoxelChunk chunk = ChunkGen.GenerateChunk(worldPos, (int)ChunkData.ChunkSizeX, (int)ChunkData.ChunkSizeY, (int)ChunkData.ChunkSizeZ, Components, Content, Graphics);
                            Drawer3D.DrawBox(chunk.GetBoundingBox(), Color.Red, 0.1f);
                            chunk.ShouldRebuild = true;
                            chunk.ShouldRecalculateLighting = true;
                            GeneratedChunks.Enqueue(chunk);
                        }

                        ToGenerate.Remove(box);
                    }


                    //GeneratorLock.ReleaseMutex();
                    if (wh == Program.ShutdownEvent)
                    {
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }

           
        }



        public void SimpleRender(GraphicsDevice graphicsDevice, Effect effect, Texture2D tilemap)
        {
            effect.Parameters["xIllumination"].SetValue(ChunkData.IllumMap);
            effect.Parameters["xTexture"].SetValue(tilemap);
            effect.Parameters["xSunGradient"].SetValue(ChunkData.SunMap);
            effect.Parameters["xAmbientGradient"].SetValue(ChunkData.AmbientMap);
            effect.Parameters["xTorchGradient"].SetValue(ChunkData.TorchMap);
            effect.Parameters["xTint"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            effect.Parameters["SelfIllumination"].SetValue(0);
            foreach (KeyValuePair<Point3, VoxelChunk> chunk in ChunkData.ChunkMap)
            {
                chunk.Value.Render(tilemap, ChunkData.IllumMap, ChunkData.SunMap, ChunkData.AmbientMap, ChunkData.TorchMap, graphicsDevice, effect, Matrix.Identity);
            }
        }

        public BoundingBox GetVisibileBoundingBox()
        {
            List<BoundingBox> toAdd = ChunkData.ChunkMap.Select(chunk => chunk.Value.GetBoundingBox()).ToList();
            return MathFunctions.GetBoundingBox(toAdd);
        }

        public void RenderAll(Camera renderCamera, GameTime gameTime, GraphicsDevice graphicsDevice, Effect effect, Matrix worldMatrix, Texture2D tilemap)
        {
            effect.Parameters["xIllumination"].SetValue(ChunkData.IllumMap);
            effect.Parameters["xTexture"].SetValue(tilemap);
            effect.Parameters["xSunGradient"].SetValue(ChunkData.SunMap);
            effect.Parameters["xAmbientGradient"].SetValue(ChunkData.AmbientMap);
            effect.Parameters["xTorchGradient"].SetValue(ChunkData.TorchMap);
            effect.Parameters["xTint"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            effect.Parameters["SelfIllumination"].SetValue(1);
            foreach (KeyValuePair<Point3, VoxelChunk> chunk in ChunkData.ChunkMap)
            {
                if(renderCamera.GetFrustrum().Intersects(chunk.Value.GetBoundingBox()))
                {
                    chunk.Value.Render(tilemap, ChunkData.IllumMap, ChunkData.SunMap, ChunkData.AmbientMap, ChunkData.TorchMap, graphicsDevice, effect, worldMatrix);
                }
            }
        }

        public void Render(Camera renderCamera, GameTime gameTime, GraphicsDevice graphicsDevice, Effect effect, Matrix worldMatrix)
        {
            effect.Parameters["xIllumination"].SetValue(ChunkData.IllumMap);
            effect.Parameters["xTexture"].SetValue(ChunkData.Tilemap);
            effect.Parameters["xSunGradient"].SetValue(ChunkData.SunMap);
            effect.Parameters["xAmbientGradient"].SetValue(ChunkData.AmbientMap);
            effect.Parameters["xTorchGradient"].SetValue(ChunkData.TorchMap);
            effect.Parameters["xTint"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            effect.Parameters["SelfIllumination"].SetValue(1);
            foreach(VoxelChunk chunk in RenderList)
            {
                chunk.Render(ChunkData.Tilemap, ChunkData.IllumMap, ChunkData.SunMap, ChunkData.AmbientMap, ChunkData.TorchMap, graphicsDevice, effect, worldMatrix);
            }

        }

        public void GenerateInitialChunks(OrbitCamera camera, Point3 origin, ref string message)
        {
            float origBuildRadius = GenerateDistance;
            GenerateDistance = origBuildRadius * 2.0f;

            int i = 0;
            int iters = WorldSize.X * WorldSize.Y * WorldSize.Z;
            for (int dx = origin.X - WorldSize.X/2; dx <= origin.X + WorldSize.X/2; dx++)
            {
                for (int dy = origin.Y - WorldSize.Y/2; dy <= origin.Y + WorldSize.Y/2; dy++)
                {
                    for (int dz = origin.Z - WorldSize.Z/2; dz <= origin.Z + WorldSize.Z/2; dz++)
                    {
                        message = "Generating : " + (i + 1) + "/" + iters;
                        i++;

                        Point3 box = new Point3(dx, dy, dz);

                        if (!ChunkData.ChunkMap.ContainsKey(box))
                        {
                            Vector3 worldPos = new Vector3(box.X * ChunkData.ChunkSizeX, box.Y * ChunkData.ChunkSizeY, box.Z * ChunkData.ChunkSizeZ);
                            VoxelChunk chunk = ChunkGen.GenerateChunk(worldPos, (int)ChunkData.ChunkSizeX, (int)ChunkData.ChunkSizeY, (int)ChunkData.ChunkSizeZ, Components, Content, Graphics);
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
                                    RecalculateBounds();
                                }
                            }
                        }
                    }
                }
            }

            UpdateRebuildList();
            GenerateDistance = origBuildRadius;

            while(GeneratedChunks.Count > 0)
            {
                VoxelChunk gen = null;
                if(!GeneratedChunks.TryDequeue(out gen))
                {
                    break;
                }
            }

            ChunkData.ChunkManager.CreateGraphics(ref message, ChunkData);
        }

        private void RecalculateBounds()
        {
            List<BoundingBox> boxes = ChunkData.ChunkMap.Select(chunkPair => chunkPair.Value.GetBoundingBox()).ToList();
            Bounds = MathFunctions.GetBoundingBox(boxes);
        }


        public void GetChunksIntersecting(BoundingBox box, HashSet<VoxelChunk> chunks)
        {
            chunks.Clear();
            Point3 minChunk = ChunkData.GetChunkID(box.Min);
            Point3 maxChunk = ChunkData.GetChunkID(box.Max);
            Point3 key = new Point3(0, 0, 0);
            for (key.X = minChunk.X; key.X <= maxChunk.X; key.X++) 
            {
                for (key.Y = minChunk.Y; key.Y <= maxChunk.Y; key.Y++)
                {
                    for (key.Z = minChunk.Z; key.Z <= maxChunk.Z; key.Z++)
                    {
                        if (ChunkData.ChunkMap.ContainsKey(key))
                        {
                            chunks.Add(ChunkData.ChunkMap[key]);
                        }
                    }
                }
            }
    }

        public void GetChunksIntersecting(BoundingFrustum frustum, HashSet<VoxelChunk> chunks)
        {
            chunks.Clear();
            BoundingBox frustumBox = MathFunctions.GetBoundingBox(frustum.GetCorners());
            GetChunksIntersecting(frustumBox, chunks);

            chunks.RemoveWhere(chunk => frustum.Contains(chunk.GetBoundingBox()) == ContainmentType.Disjoint);
        }

        public void Update(GameTime gameTime, Camera camera, GraphicsDevice g)
        {
            UpdateRenderList(camera);

            if(waterUpdateTimer.Update(gameTime))
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
                    ChunkData.RecomputeNeighbors();
                }

                foreach(VoxelChunk chunk in GeneratedChunks)
                {
                    if(!ChunkData.ChunkMap.ContainsKey(chunk.ID))
                    {
                        ChunkData.AddChunk(chunk);
                        ChunkGen.GenerateVegetation(chunk, Components, Content, Graphics);
                        ChunkGen.GenerateFauna(chunk, Components, Content, Graphics, PlayState.ComponentManager.Factions);
                        List<VoxelChunk> adjacents = ChunkData.GetAdjacentChunks(chunk);
                        foreach(VoxelChunk c in adjacents)
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


            visibilityChunksTimer.Update(gameTime);
            if(visibilityChunksTimer.HasTriggered)
            {
                visibleSet.Clear();
                GetChunksIntersecting(camera.GetFrustrum(), visibleSet);
                //RemoveDistantBlocks(camera);
            }


            foreach(VoxelChunk chunk in ChunkData.ChunkMap.Values)
            {
                chunk.Update(gameTime);
            }

            Water.Splash(gameTime);
            Water.HandleTransfers(gameTime);
        }

        public List<Voxel> GetVoxelsIntersecting(BoundingBox box)
        {
            HashSet<VoxelChunk> intersects = new HashSet<VoxelChunk>();
            GetChunksIntersecting(box, intersects);

            List<Voxel> toReturn = new List<Voxel>();

            foreach (VoxelChunk chunk in intersects)
            {
                toReturn.AddRange(chunk.GetVoxelsIntersecting(box));
            }

            return toReturn;
        }

        public void CreateGraphics(ref string message, ChunkData chunkData)
        {
            message = "Creating Graphics";
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

            message = "Creating Graphics : Updating Max Viewing Level";
            foreach(VoxelChunk chunk in toRebuild)
            {
                chunk.UpdateMaxViewingLevel();
            }

            message = "Creating Graphics : Updating Ramps";
            foreach(VoxelChunk chunk in toRebuild.Where(chunk => GameSettings.Default.CalculateRamps))
            {
                chunk.UpdateRamps();
            }

            message = "Creating Graphics : Calculating lighting ";
            int j = 0;
            foreach(VoxelChunk chunk in toRebuild)
            {
                j++;
                message = "Creating Graphics : Calculating lighting " + j + "/" + toRebuild.Count;
                if(chunk.ShouldRecalculateLighting)
                {
                    chunk.CalculateGlobalLight();
                    chunk.ShouldRecalculateLighting = false;
                }
            }

            j = 0;
            foreach(VoxelChunk chunk in toRebuild)
            {
                j++;
                message = "Creating Graphics : Calculating vertex light " + j + "/" + toRebuild.Count;
                chunk.CalculateVertexLighting();
            }

            message = "Creating Graphics: Building Vertices";
            j = 0;
            foreach(VoxelChunk chunk in toRebuild)
            {
                j++;
                message = "Creating Graphics : Building Vertices " + j + "/" + toRebuild.Count;

                if(!chunk.ShouldRebuild)
                {
                    continue;
                }

                chunk.Rebuild(Graphics);
                chunk.ShouldRebuild = false;
                chunk.RebuildPending = false;
                chunk.RebuildLiquidPending = false;
            }

            chunkData.RecomputeNeighbors();


            message = "Cleaning Up.";
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