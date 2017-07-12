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
        private readonly Timer visibilityChunksTimer = new Timer(0.03f, false, Timer.TimerMode.Real);
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
        public ComponentManager Components { get { return World.ComponentManager; }}
        public ContentManager Content { get; set; }

        private readonly HashSet<VoxelChunk> visibleSet = new HashSet<VoxelChunk>();

        public WaterManager Water { get; set; }

        public List<DynamicLight> DynamicLights { get; set; }

        public ChunkData ChunkData
        {
            get { return chunkData; }
        }

        public List<VoxelHandle> KilledVoxels { get; set; }

        public ChunkManager(ContentManager content, 
            WorldManager world,
            uint chunkSizeX, uint chunkSizeY, uint chunkSizeZ, 
            Camera camera, GraphicsDevice graphics,
            ChunkGenerator chunkGen, int maxChunksX, int maxChunksY, int maxChunksZ)
        {
            World = world;
            KilledVoxels = new List<VoxelHandle>();
            ExitThreads = false;
            drawDistSq = DrawDistance * DrawDistance;
            Content = content;

            chunkData = new ChunkData(chunkSizeX, chunkSizeY, chunkSizeZ, 1.0f / chunkSizeX, 1.0f / chunkSizeY, 1.0f / chunkSizeZ, this);
            ChunkData.ChunkMap = new ConcurrentDictionary<GlobalChunkCoordinate, VoxelChunk>();
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

            ChunkData.MaxViewingLevel = chunkSizeY;

            GameSettings.Default.ChunkGenerateTime = 0.5f;
            generateChunksTimer = new Timer(GameSettings.Default.ChunkGenerateTime, false, Timer.TimerMode.Real);
            GameSettings.Default.ChunkRebuildTime = 0.1f;
            Timer rebuildChunksTimer = new Timer(GameSettings.Default.ChunkRebuildTime, false, Timer.TimerMode.Real);
            GameSettings.Default.VisibilityUpdateTime = 0.05f;
            visibilityChunksTimer = new Timer(GameSettings.Default.VisibilityUpdateTime, false, Timer.TimerMode.Real);
            generateChunksTimer.HasTriggered = true;
            rebuildChunksTimer.HasTriggered = true;
            visibilityChunksTimer.HasTriggered = true;
            this.camera = camera;

            Water = new WaterManager(this);

            DynamicLights = new List<DynamicLight>();

            ChunkData.Slice = SliceMode.Y;
            PauseThreads = false;

            WorldSize = new Point3(maxChunksX, maxChunksY, maxChunksZ);

            Vector3 maxBounds = new Vector3(maxChunksX * chunkSizeX / 2.0f, maxChunksY * chunkSizeY / 2.0f, maxChunksZ * chunkSizeZ / 2.0f);
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

            float dA = (a.Origin - camera.Position + new Vector3(a.SizeX / 2.0f, a.SizeY / 2.0f, a.SizeZ / 2.0f)).LengthSquared();
            float dB = (b.Origin - camera.Position + new Vector3(b.SizeX / 2.0f, b.SizeY / 2.0f, b.SizeZ / 2.0f)).LengthSquared();

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
                                Vector3 worldPos = new Vector3(box.X*ChunkData.ChunkSizeX, box.Y*ChunkData.ChunkSizeY,
                                    box.Z*ChunkData.ChunkSizeZ);
                                VoxelChunk chunk = ChunkGen.GenerateChunk(worldPos, (int) ChunkData.ChunkSizeX,
                                    (int) ChunkData.ChunkSizeY, (int) ChunkData.ChunkSizeZ, World, Content,
                                    Graphics);
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



        public void SimpleRender(GraphicsDevice graphicsDevice, Shader effect, Texture2D tilemap)
        {
            effect.SelfIlluminationTexture = ChunkData.IllumMap;
            effect.MainTexture = tilemap;
            effect.SunlightGradient = ChunkData.SunMap;
            effect.AmbientOcclusionGradient = ChunkData.AmbientMap;
            effect.TorchlightGradient = ChunkData.TorchMap;
            effect.LightRampTint = Color.White;
            effect.VertexColorTint = Color.White;
            effect.SelfIlluminationEnabled = false;
            effect.EnableShadows = false;
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (var chunk in ChunkData.ChunkMap)
                {
                    chunk.Value.Render(Graphics);
                }
            }
        }

        public BoundingBox GetVisibileBoundingBox()
        {
            List<BoundingBox> toAdd = ChunkData.ChunkMap.Select(chunk => chunk.Value.GetBoundingBox()).ToList();
            return MathFunctions.GetBoundingBox(toAdd);
        }

        public void RenderAll(Camera renderCamera, DwarfTime gameTime, GraphicsDevice graphicsDevice, Shader effect, Matrix worldMatrix, Texture2D tilemap)
        {
            effect.SelfIlluminationTexture = ChunkData.IllumMap;
            effect.MainTexture = tilemap;
            effect.SunlightGradient = ChunkData.SunMap;
            effect.AmbientOcclusionGradient = ChunkData.AmbientMap;
            effect.TorchlightGradient = ChunkData.TorchMap;
            effect.LightRampTint = Color.White;
            effect.VertexColorTint = Color.White;
            effect.SelfIlluminationEnabled = true;
            effect.EnableShadows = false;

			BoundingFrustum cameraFrustrum = renderCamera.GetFrustrum();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (var chunk in ChunkData.ChunkMap)
                {
                    if (cameraFrustrum.Intersects(chunk.Value.GetBoundingBox()))
                    {
                        chunk.Value.Render(Graphics);
                    }
                }
            }
            effect.SelfIlluminationEnabled = false;
        }

        public void RenderSelectionBuffer(Shader effect, GraphicsDevice graphicsDevice,
            Matrix viewmatrix)
        {
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.SelectionBuffer];
            effect.MainTexture = ChunkData.Tilemap;
            effect.World = Matrix.Identity;
            effect.View = viewmatrix;
            effect.SelectionBufferColor = Vector4.Zero;
            List<VoxelChunk> renderListCopy = RenderList.ToArray().ToList();

            foreach (VoxelChunk chunk in renderListCopy)
            {
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    chunk.Render(Graphics);
                }
            }
        }

        public void RenderShadowmap(Shader effect,
                                    GraphicsDevice graphicsDevice, 
                                    ShadowRenderer shadowRenderer,
                                    Matrix worldMatrix, 
                                    Texture2D tilemap)
        {
            Vector3[] corners = new Vector3[8];
            Camera tempCamera = new Camera(World, camera.Target, camera.Position, camera.FOV, camera.AspectRatio, camera.NearPlane, 30);
            tempCamera.GetFrustrum().GetCorners(corners);
            BoundingBox cameraBox = MathFunctions.GetBoundingBox(corners);
            cameraBox = cameraBox.Expand(1.0f);
            effect.World = worldMatrix;
            effect.MainTexture = tilemap;
            shadowRenderer.SetupViewProj(cameraBox);
            shadowRenderer.PrepareEffect(effect, false);
            shadowRenderer.BindShadowmapEffect(effect);
            shadowRenderer.BindShadowmap(graphicsDevice);

            List<VoxelChunk> renderListCopy = RenderList.ToArray().ToList();

            foreach (VoxelChunk chunk in renderListCopy)
            {
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    chunk.Render(Graphics);
                }
            }
            shadowRenderer.UnbindShadowmap(graphicsDevice);
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.Textured];
            effect.SelfIlluminationEnabled = false;
        }

        public void RenderLightmaps(Camera renderCamera, DwarfTime gameTime, GraphicsDevice graphicsDevice,
            Shader effect, Matrix worldMatrix)
        {
            RasterizerState state = RasterizerState.CullNone;
            RasterizerState origState = graphicsDevice.RasterizerState;

            effect.CurrentTechnique = effect.Techniques[Shader.Technique.Lightmap];
            effect.SelfIlluminationTexture = ChunkData.IllumMap;
            effect.MainTexture = ChunkData.Tilemap;
            effect.SunlightGradient = ChunkData.SunMap;
            effect.AmbientOcclusionGradient = ChunkData.AmbientMap;
            effect.TorchlightGradient = ChunkData.TorchMap;
            effect.LightRampTint = Color.White;
            effect.VertexColorTint = Color.White;
            effect.SelfIlluminationEnabled = true;
            effect.EnableShadows = GameSettings.Default.UseDynamicShadows;
            effect.EnableLighting = true;
            graphicsDevice.RasterizerState = state;
            Graphics.BlendState = BlendState.NonPremultiplied;
            List<VoxelChunk> renderListCopy = RenderList.ToArray().ToList();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (VoxelChunk chunk in renderListCopy)
                {
                    Graphics.SetRenderTarget(chunk.Primitive.Lightmap);
                    Graphics.Clear(ClearOptions.Target, Color.Black, 0.0f, 0);
                    chunk.Render(Graphics);
                }
            }
            Graphics.SetRenderTarget(null);
            effect.SelfIlluminationEnabled = false;
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.Textured];
            graphicsDevice.RasterizerState = origState;
        }

        public void Render(Camera renderCamera, DwarfTime gameTime, GraphicsDevice graphicsDevice, Shader effect, Matrix worldMatrix)
        {
            if (GameSettings.Default.UseLightmaps)
            {
                effect.CurrentTechnique = effect.Techniques[Shader.Technique.TexturedWithLightmap];
                effect.EnableShadows = false;
            }
            else
            {
                effect.CurrentTechnique = effect.Techniques[Shader.Technique.Textured];
                effect.EnableShadows = GameSettings.Default.UseDynamicShadows;
            }
            effect.SelfIlluminationTexture = ChunkData.IllumMap;
            effect.MainTexture = ChunkData.Tilemap;
            effect.SunlightGradient = ChunkData.SunMap;
            effect.AmbientOcclusionGradient = ChunkData.AmbientMap;
            effect.TorchlightGradient = ChunkData.TorchMap;
            effect.LightRampTint = Color.White;
            effect.VertexColorTint = Color.White;
            effect.SelfIlluminationEnabled = true;
            effect.EnableShadows = GameSettings.Default.UseDynamicShadows;
            effect.World = Matrix.Identity;
            effect.EnableLighting = true;
            List<VoxelChunk> renderListCopy = RenderList.ToArray().ToList();

            foreach (VoxelChunk chunk in renderListCopy)
            {
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    if (GameSettings.Default.UseLightmaps && chunk.Primitive.Lightmap != null)
                    {
                        effect.LightMap = chunk.Primitive.Lightmap;
                        effect.PixelSize = new Vector2(1.0f/chunk.Primitive.Lightmap.Width,
                            1.0f/chunk.Primitive.Lightmap.Height);
                    }
                    pass.Apply();
                    chunk.Render(Graphics);
                }
            }
            effect.SelfIlluminationEnabled = false;
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.Textured];
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

            var boxes = new List<GlobalChunkCoordinate>();
            for (int dx = origin.X - WorldSize.X/2 + 1; dx < origin.X + WorldSize.X/2; dx++)
            {
                for (int dy = origin.Y - WorldSize.Y/2; dy <= origin.Y + WorldSize.Y/2; dy++)
                {
                    for (int dz = origin.Z - WorldSize.Z/2 + 1; dz < origin.Z + WorldSize.Z/2; dz++)
                    {
                        boxes.Add(new GlobalChunkCoordinate(dx, dy, dz));
                    }
                }
            }

            SetLoadingMessage("Generating Chunks...");

            foreach(var box in boxes)
            {
                if (!ChunkData.ChunkMap.ContainsKey(box))
                {
                    Vector3 worldPos = new Vector3(box.X * ChunkData.ChunkSizeX, box.Y * ChunkData.ChunkSizeY, box.Z * ChunkData.ChunkSizeZ);
                    VoxelChunk chunk = ChunkGen.GenerateChunk(worldPos, (int)ChunkData.ChunkSizeX, (int)ChunkData.ChunkSizeY, (int)ChunkData.ChunkSizeZ, World, Content, Graphics);
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
            chunkData.RecomputeNeighbors();
            SetLoadingMessage("Generating Ores...");

            GenerateOres();

            SetLoadingMessage("Fog of war...");
            // We are going to force fog of war to be on for the first reveal then reset it back to it's previous setting after.
            // This is a pseudo hack to stop worlds created with Fog of War off then looking awful if it is turned back on.
            bool fogOfWar = GameSettings.Default.FogofWar;
            GameSettings.Default.FogofWar = true;
            ChunkData.Reveal(GeneratedChunks.First().MakeVoxel(0, (int)ChunkData.ChunkSizeY - 1, 0));
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


        public void GetChunksIntersecting(BoundingBox box, HashSet<VoxelChunk> chunks)
        {
            chunks.Clear();
            var minChunk = ChunkData.GetChunkID(box.Min);
            var maxChunk = ChunkData.GetChunkID(box.Max);
            for (var x = minChunk.X; x <= maxChunk.X; ++x)
                for (var y = minChunk.Y; y <= maxChunk.Y; ++y)
                    for (var z = minChunk.Z; z <= maxChunk.Z; ++z)
                    {
                        VoxelChunk chunk;
                        if (ChunkData.ChunkMap.TryGetValue(new GlobalChunkCoordinate(x, y, z), out chunk))
                            chunks.Add(chunk);
                    }
        }

        public void GetChunksIntersecting(BoundingFrustum frustum, HashSet<VoxelChunk> chunks)
        {
            chunks.Clear();
            BoundingBox frustumBox = MathFunctions.GetBoundingBox(frustum.GetCorners());
            GetChunksIntersecting(frustumBox, chunks);

            chunks.RemoveWhere(chunk => frustum.Contains(chunk.GetBoundingBox()) == ContainmentType.Disjoint);
        }

        public void Update(DwarfTime gameTime, Camera camera, GraphicsDevice g)
        {
            UpdateRenderList(camera);

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
                    ChunkData.RecomputeNeighbors();
                }

                foreach(VoxelChunk chunk in GeneratedChunks)
                {
                    if(!ChunkData.ChunkMap.ContainsKey(chunk.ID))
                    {
                        ChunkData.AddChunk(chunk);
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

            HashSet<VoxelChunk> affectedChunks = new HashSet<VoxelChunk>();
            
            foreach (VoxelHandle voxel in KilledVoxels)
            {
                affectedChunks.Add(voxel.Chunk);
                voxel.Chunk.NotifyDestroyed(voxel.GridPosition);
                foreach (var neighbor in Neighbors.EnumerateAllNeighbors(new GlobalVoxelCoordinate(
                    voxel.ChunkID, new LocalVoxelCoordinate((int)voxel.GridPosition.X, (int)voxel.GridPosition.Y, (int)voxel.GridPosition.Z))))
                {
                    VoxelChunk chunk;
                    if (chunkData.ChunkMap.TryGetValue(neighbor.GetGlobalChunkCoordinate(), out chunk))
                        affectedChunks.Add(chunk);
                }
            }

            if (GameSettings.Default.FogofWar)
            {
                ChunkData.Reveal(KilledVoxels);
            }

            lock (RebuildList)
            {
                foreach (VoxelChunk affected in affectedChunks)
                {
                    affected.NotifyTotalRebuild(false);
                }
            }
            KilledVoxels.Clear();
        }

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

        public List<VoxelHandle> GetVoxelsIntersecting(BoundingBox box)
        {
            HashSet<VoxelChunk> intersects = new HashSet<VoxelChunk>();
            GetChunksIntersecting(box, intersects);

            List<VoxelHandle> toReturn = new List<VoxelHandle>();

            foreach (VoxelChunk chunk in intersects)
            {
                toReturn.AddRange(chunk.GetVoxelsIntersecting(box));
            }

            return toReturn;
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

        /// <summary>
        /// Does a n-iteration breadth-first search starting from the seed.
        /// </summary>
        /// <param name="seed">The seed.</param>
        /// <param name="radiusSquared">The squared number of voxels to search.</param>
        /// <param name="fn">The function. Returns the first voxel matching this function.</param>
        /// <returns>the first voxel matching fn.</returns>
        public VoxelHandle BreadthFirstSearch(VoxelHandle seed, float radiusSquared, Func<VoxelHandle, bool> fn)
        {
            Queue<VoxelHandle> queue = new Queue<VoxelHandle>();
            queue.Enqueue(seed);
            HashSet<VoxelHandle> visited = new HashSet<VoxelHandle>();
            List<VoxelHandle> neighbors = new List<VoxelHandle>(6);
            while (queue.Count > 0)
            {
                VoxelHandle curr = queue.Dequeue();
                if (fn(curr))
                {
                    return curr;
                }
                
                if((curr.Position - seed.Position).LengthSquared() < radiusSquared)
                {
                    curr.Chunk.GetNeighborsManhattan((int)curr.GridPosition.X, (int)curr.GridPosition.Y, (int)curr.GridPosition.Z, neighbors);

                    foreach (VoxelHandle voxel in neighbors)
                    {
                        if (voxel != null && !visited.Contains(voxel))
                        {
                            queue.Enqueue(new VoxelHandle(voxel.GridPosition, voxel.Chunk));
                        }
                    }
                }
                //Drawer3D.DrawBox(curr.GetBoundingBox(), Color.White, 0.01f);
                visited.Add(curr);
            }
            return null;
        }

        public List<VoxelHandle> BreadthFirstSearch(VoxelHandle seed, float radiusSquared, bool searchEmpty = true)
        {
            Queue<VoxelHandle> queue = new Queue<VoxelHandle>();
            queue.Enqueue(seed);
            List<VoxelHandle> outList = new List<VoxelHandle>();
            Func<VoxelHandle, bool> searchQuery = (VoxelHandle v) => v.IsEmpty;
            if (!searchEmpty)
            {
                searchQuery = v => !v.IsEmpty;
            }
            

            while (queue.Count > 0)
            {
                VoxelHandle curr = queue.Dequeue();
                if (curr != null && searchQuery(curr) && !outList.Contains(curr) && (curr.Position - seed.Position).LengthSquared() < radiusSquared)
                {
                    outList.Add(curr);
                    List<VoxelHandle> neighbors = new List<VoxelHandle>(6);
                    curr.Chunk.GetNeighborsManhattan((int)curr.GridPosition.X, (int)curr.GridPosition.Y, (int)curr.GridPosition.Z, neighbors);

                    foreach (VoxelHandle voxel in neighbors)
                    {
                        if (voxel != null && !outList.Contains(voxel)  && searchQuery(voxel))
                        {
                            queue.Enqueue(voxel);
                        }
                    }
                }
            }
            return outList;
        }
    }

}
