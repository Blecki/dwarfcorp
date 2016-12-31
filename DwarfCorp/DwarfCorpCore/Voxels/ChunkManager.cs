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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     Responsible for keeping track of and accessing large collections of
    ///     voxels. There is intended to be only one chunk manager. Essentially,
    ///     it is a virtual memory lookup table for the world's voxels. It imitates
    ///     a gigantic 3D array.
    /// </summary>
    public class ChunkManager
    {
        /// <summary>
        /// Directions that the user can slice in.
        /// </summary>
        public enum SliceMode
        {
            X,
            Y,
            Z
        }

        /// <summary>
        /// Called every time we want to update the flow of water/lava.
        /// </summary>
        private static readonly AutoResetEvent WaterUpdateEvent = new AutoResetEvent(true);
        /// <summary>
        /// Called every time we want to generate new chunks.
        /// </summary>
        private static readonly AutoResetEvent NeedsGenerationEvent = new AutoResetEvent(false);
        /// <summary>
        /// Called every time we want to rebuild chunk vertex buffers.
        /// </summary>
        private static readonly AutoResetEvent NeedsRebuildEvent = new AutoResetEvent(false);
        /// <summary>
        /// Called every time we want to rebuild liquid vertex buffers.
        /// </summary>
        private static readonly AutoResetEvent NeedsLiquidEvent = new AutoResetEvent(false);
        /// <summary>
        /// The camera
        /// </summary>
        private readonly Camera camera;
        /// <summary>
        /// The chunk data (holds the actual data for chunks).
        /// </summary>
        private readonly ChunkData chunkData;
        /// <summary>
        /// Timer which when triggered causes new chunks to be generated.
        /// </summary>
        private readonly Timer generateChunksTimer = new Timer(0.5f, false, Timer.TimerMode.Real);
        /// <summary>
        /// Timer which when triggered frustum culls voxel chunks.
        /// </summary>
        private readonly Timer visibilityChunksTimer = new Timer(0.03f, false, Timer.TimerMode.Real);
        /// <summary>
        /// The set of all Voxel Chunks to draw this frame.
        /// </summary>
        private readonly HashSet<VoxelChunk> visibleSet = new HashSet<VoxelChunk>();
        /// <summary>
        /// Every time this triggers, water/lava gets updated.
        /// </summary>
        private readonly Timer waterUpdateTimer = new Timer(0.1f, false, Timer.TimerMode.Real);
        /// <summary>
        /// The square of the maximum draw distance in voxels.
        /// </summary>
        protected float drawDistSq = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkManager"/> class.
        /// </summary>
        /// <param name="content">The content manager.</param>
        /// <param name="chunkSizeX">The number of voxels in a chunk in the X direction.</param>
        /// <param name="chunkSizeY">The number of voxels in a chunk in the Y direction.</param>
        /// <param name="chunkSizeZ">The number of voxels in a chunk in the Z direction.</param>
        /// <param name="camera">The camera.</param>
        /// <param name="graphics">The graphics.</param>
        /// <param name="chunkGen">The chunk generator.</param>
        /// <param name="maxChunksX">The maximum number of chunks in x.</param>
        /// <param name="maxChunksY">The maximum number of chunks in y.</param>
        /// <param name="maxChunksZ">The maximum number of chunks in z.</param>
        public ChunkManager(ContentManager content,
            uint chunkSizeX, uint chunkSizeY, uint chunkSizeZ,
            Camera camera, GraphicsDevice graphics,
            ChunkGenerator chunkGen, int maxChunksX, int maxChunksY, int maxChunksZ)
        {
            KilledVoxels = new List<Voxel>();
            ExitThreads = false;
            drawDistSq = DrawDistance*DrawDistance;
            Content = content;

            chunkData = new ChunkData(chunkSizeX, chunkSizeY, chunkSizeZ, 1.0f/chunkSizeX, 1.0f/chunkSizeY,
                1.0f/chunkSizeZ, this);
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
            generateChunksTimer = new Timer(GameSettings.Default.ChunkGenerateTime, false, Timer.TimerMode.Real);
            GameSettings.Default.ChunkRebuildTime = 0.1f;
            var rebuildChunksTimer = new Timer(GameSettings.Default.ChunkRebuildTime, false, Timer.TimerMode.Real);
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

            var maxBounds = new Vector3(maxChunksX*chunkSizeX*0.5f, maxChunksY*chunkSizeY*0.5f,
                maxChunksZ*chunkSizeZ*0.5f);
            Vector3 minBounds = -maxBounds;
            Bounds = new BoundingBox(minBounds, maxBounds);
        }

        /// <summary>
        /// The current list of chunks to render.
        /// </summary>
        /// <value>
        /// The render list.
        /// </value>
        public ConcurrentQueue<VoxelChunk> RenderList { get; set; }
        /// <summary>
        /// The current list of chunks whose vertex buffers are to be rebuilt.
        /// </summary>
        /// <value>
        /// The rebuild list.
        /// </value>
        public ConcurrentQueue<VoxelChunk> RebuildList { get; set; }
        /// <summary>
        /// The current list of chunks whose liquid (water/lava) vertex buffers are to be rebuilt.
        /// </summary>
        /// <value>
        /// The rebuild liquids list.
        /// </value>
        public ConcurrentQueue<VoxelChunk> RebuildLiquidsList { get; set; }

        /// <summary>
        /// Gets or sets the size of the world in chunks.
        /// </summary>
        /// <value>
        /// The size of the world in chunks.
        /// </value>
        public Point3 WorldSize { get; set; }

        /// <summary>
        /// Gets or sets the chunk generator.
        /// </summary>
        /// <value>
        /// The chunk generator.
        /// </value>
        public ChunkGenerator ChunkGen { get; set; }
        /// <summary>
        /// List of chunks that were newly generated.
        /// </summary>
        /// <value>
        /// The generated chunks.
        /// </value>
        public ConcurrentQueue<VoxelChunk> GeneratedChunks { get; set; }
        /// <summary>
        /// List of new chunk IDs to generate.
        /// </summary>
        /// <value>
        /// To generate.
        /// </value>
        public List<Point3> ToGenerate { get; set; }

        /// <summary>
        /// Thread responsible for generating new chunks.
        /// </summary>
        /// <value>
        /// The generator thread.
        /// </value>
        private Thread GeneratorThread { get; set; }

        /// <summary>
        /// Thread responsible for rebuilding chunks' vertex buffers.
        /// </summary>
        /// <value>
        /// The rebuild thread.
        /// </value>
        private Thread RebuildThread { get; set; }
        /// <summary>
        /// Thread responsible for rebuildling chunks' liquid (water/lava) vertex buffers.
        /// </summary>
        /// <value>
        /// The rebuild liquid thread.
        /// </value>
        private Thread RebuildLiquidThread { get; set; }

        /// <summary>
        /// Thread for updating the flow of water.
        /// </summary>
        /// <value>
        /// The water thread.
        /// </value>
        private Thread WaterThread { get; set; }

        /// <summary>
        /// The bounding box of the whole world.
        /// </summary>
        /// <value>
        /// The bounds.
        /// </value>
        public BoundingBox Bounds { get; set; }

        /// <summary>
        /// Gets or sets the draw distance in voxels. Chunks are not drawn beyond this distance.
        /// </summary>
        /// <value>
        /// The draw distance.
        /// </value>
        public float DrawDistance
        {
            get { return GameSettings.Default.ChunkDrawDistance; }
            set
            {
                GameSettings.Default.ChunkDrawDistance = value;
                drawDistSq = value*value;
            }
        }

        /// <summary>
        /// Gets or sets the draw distance squared.
        /// </summary>
        /// <value>
        /// The draw distance squared.
        /// </value>
        public float DrawDistanceSquared
        {
            get { return drawDistSq; }
            set
            {
                drawDistSq = value;
                GameSettings.Default.ChunkDrawDistance = (float) Math.Sqrt(value);
            }
        }

        /// <summary>
        /// Chunks are deleted from memory if they are further than this amount from the camera.
        /// </summary>
        /// <value>
        /// The remove distance.
        /// </value>
        public float RemoveDistance
        {
            get { return GameSettings.Default.ChunkUnloadDistance; }
            set { GameSettings.Default.ChunkDrawDistance = value; }
        }

        /// <summary>
        /// New chunks are generated within this distance to the camera.
        /// </summary>
        /// <value>
        /// The generate distance.
        /// </value>
        public float GenerateDistance
        {
            get { return GameSettings.Default.ChunkGenerateDistance; }
            set { GameSettings.Default.ChunkDrawDistance = value; }
        }

        /// <summary>
        /// Gets or sets the graphics device.
        /// </summary>
        /// <value>
        /// The graphics.
        /// </value>
        public GraphicsDevice Graphics { get; set; }

        /// <summary>
        /// If this value is true, the generation/update threads are paused.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [pause threads]; otherwise, <c>false</c>.
        /// </value>
        public bool PauseThreads { get; set; }

        /// <summary>
        /// If this value is true, the generation/update threads are destroyed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [exit threads]; otherwise, <c>false</c>.
        /// </value>
        public bool ExitThreads { get; set; }

        /// <summary>
        /// Gets the component manager.
        /// </summary>
        /// <value>
        /// The components.
        /// </value>
        public ComponentManager Components
        {
            get { return PlayState.ComponentManager; }
        }

        /// <summary>
        /// Gets or sets the content manager.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        public ContentManager Content { get; set; }

        /// <summary>
        /// Gets or sets the water manager.
        /// </summary>
        /// <value>
        /// The water.
        /// </value>
        public WaterManager Water { get; set; }

        /// <summary>
        /// Gets or sets the dynamic lights.
        /// </summary>
        /// <value>
        /// The dynamic lights.
        /// </value>
        public List<DynamicLight> DynamicLights { get; set; }

        /// <summary>
        /// Gets the chunk data (containing the actual data inside chunks)
        /// </summary>
        /// <value>
        /// The chunk data.
        /// </value>
        public ChunkData ChunkData
        {
            get { return chunkData; }
        }

        /// <summary>
        /// Gets or sets the list of voxels destroyed during this frame.
        /// </summary>
        /// <value>
        /// The killed voxels.
        /// </value>
        public List<Voxel> KilledVoxels { get; set; }

        /// <summary>
        /// Starts the threads.
        /// </summary>
        public void StartThreads()
        {
            GeneratorThread.Start();
            RebuildThread.Start();
            WaterThread.Start();
            RebuildLiquidThread.Start();
        }

        /// <summary>
        /// Updates the water thread.
        /// </summary>
        public void UpdateWaterThread()
        {
            EventWaitHandle[] waitHandles =
            {
                WaterUpdateEvent,
                Program.ShutdownEvent
            };

#if CREATE_CRASH_LOGS
            try
#endif
            {
                while (!DwarfGame.ExitGame && !ExitThreads)
                {
                    // Wait until we've been told to update.
                    EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                    // Wait for a program shutdown...
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
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }
#endif
        }

        /// <summary>
        /// Thread that rebuilds liquid vertex buffers.
        /// </summary>
        public void RebuildLiquidsThread()
        {
            EventWaitHandle[] waitHandles =
            {
                NeedsLiquidEvent,
                Program.ShutdownEvent
            };

#if CREATE_CRASH_LOGS
            try
#endif
            {
                bool shouldExit = false;
                while (!shouldExit && !DwarfGame.ExitGame && !ExitThreads)
                {
                    // Wait until we get an update event.
                    EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                    if (wh == Program.ShutdownEvent)
                    {
                        break;
                    }

                    while (!PauseThreads && RebuildLiquidsList.Count > 0)
                    {
                        VoxelChunk chunk = null;
                        // Get the next chunk to rebuild.
                        if (!RebuildLiquidsList.TryDequeue(out chunk))
                        {
                            break;
                        }

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
                }
            }
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }
#endif
        }

        /// <summary>
        /// Thread that rebuilds the voxel vertex buffers.
        /// </summary>
        public void RebuildVoxelsThread()
        {
            EventWaitHandle[] waitHandles =
            {
                NeedsRebuildEvent,
                Program.ShutdownEvent
            };

#if CREATE_CRASH_LOGS
            try
#endif
            {
                while (!DwarfGame.ExitGame && !ExitThreads)
                {
                    // Wait until we've been told to rebuild the voxels primities.
                    EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                    // If shutting down, break.
                    if (wh == Program.ShutdownEvent)
                    {
                        break;
                    }
                    {
                        if (PauseThreads)
                        {
                            continue;
                        }
                        // Create a dictionary from chunk type to whether or not it should be rebuilt.
                        var toRebuild = new Dictionary<Point3, VoxelChunk>();
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

                        // First, calculate new ramp states for all voxels.
                        if (calculateRamps)
                        {
                            foreach (VoxelChunk chunk in toRebuild.Select(chunkPair => chunkPair.Value))
                            {
                                chunk.UpdateRamps();
                            }
                        }


                        // Then, calculate the sunlight values.
                        foreach (
                            VoxelChunk chunk in
                                toRebuild.Select(chunkPair => chunkPair.Value)
                                    .Where(chunk => chunk.ShouldRecalculateLighting))
                        {
                            chunk.CalculateGlobalLight();
                        }

                        // Now, for each voxel chunk, try to rebuild its vertex buffer.
                        foreach (VoxelChunk chunk in toRebuild.Select(chunkPair => chunkPair.Value))
                        {
                            if (chunk.RebuildPending && chunk.ShouldRebuild)
                            {
                                // Calculate light for each vertex.
                                if (chunk.ShouldRecalculateLighting)
                                {
                                    chunk.CalculateVertexLighting();
                                }
                                // Rebuild the chunk.
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
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
                throw;
            }
#endif
        }

        /// <summary>
        /// Comparator that compares the distance to the camera of two chunks a and b.
        /// </summary>
        /// <param name="a">The first chunk</param>
        /// <param name="b">The second chunk.</param>
        /// <returns>Comparison between a and b's distance.</returns>
        public int CompareChunkDistance(VoxelChunk a, VoxelChunk b)
        {
            if (a == b || !a.IsVisible && !b.IsVisible)
            {
                return 0;
            }

            if (!a.IsVisible)
            {
                return 1;
            }

            if (!b.IsVisible)
            {
                return -1;
            }

            float dA =
                (a.Origin - camera.Position + new Vector3(a.SizeX/2.0f, a.SizeY/2.0f, a.SizeZ/2.0f)).LengthSquared();
            float dB =
                (b.Origin - camera.Position + new Vector3(b.SizeX/2.0f, b.SizeY/2.0f, b.SizeZ/2.0f)).LengthSquared();

            if (dA < dB)
            {
                return -1;
            }

            return 1;
        }

        /// <summary>
        /// Updates a list of voxel chunks to render given a camera
        /// </summary>
        /// <param name="camera">The camera.</param>
        public void UpdateRenderList(Camera camera)
        {
            // Remove everything from the current render list.
            while (RenderList.Count > 0)
            {
                VoxelChunk result;
                if (!RenderList.TryDequeue(out result))
                {
                    break;
                }
            }

            // Determine which chunks are visible to the camera.
            foreach (VoxelChunk chunk in visibleSet)
            {
                BoundingBox box = chunk.GetBoundingBox();

                if ((camera.Position - (box.Min + box.Max)*0.5f).Length() < DrawDistance)
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

        /// <summary>
        /// Updates the list of chunks to rebuild.
        /// </summary>
        public void UpdateRebuildList()
        {
            var toRebuild = new List<VoxelChunk>();
            var toRebuildLiquids = new List<VoxelChunk>();

            // Construct a list of chunks to rebuild.
            foreach (VoxelChunk chunk in ChunkData.ChunkMap.Select(chunks => chunks.Value))
            {
                if (chunk.ShouldRebuild && ! chunk.RebuildPending)
                {
                    toRebuild.Add(chunk);
                    chunk.RebuildPending = true;
                }

                if (chunk.ShouldRebuildWater && ! chunk.RebuildLiquidPending)
                {
                    toRebuildLiquids.Add(chunk);
                    chunk.RebuildLiquidPending = true;
                }
            }


            // Sort the rebuild list by distance to the camera (so that nearer chunks get updated first)
            if (toRebuild.Count > 0)
            {
                toRebuild.Sort(CompareChunkDistance);
                foreach (VoxelChunk chunk in toRebuild)
                {
                    RebuildList.Enqueue(chunk);
                }
            }

            // Do the same for chunks whose liquid vertex buffers need to be rebuilt as well.
            if (toRebuildLiquids.Count > 0)
            {
                toRebuildLiquids.Sort(CompareChunkDistance);

                foreach (VoxelChunk chunk in toRebuildLiquids.Where(chunk => !RebuildLiquidsList.Contains(chunk)))
                {
                    RebuildLiquidsList.Enqueue(chunk);
                }
            }

            if (RebuildList.Count > 0)
            {
                NeedsRebuildEvent.Set();
            }

            if (RebuildLiquidsList.Count > 0)
            {
                NeedsLiquidEvent.Set();
            }
        }

        /// <summary>
        /// Determines whether or not the given bounding box intersects any existing chunk.
        /// </summary>
        /// <param name="box">The box.</param>
        /// <returns>true if the box intersects the world, false otherwise.</returns>
        public bool BoundingBoxIntersectsWorld(BoundingBox box)
        {
            var chunksIntersecting = new HashSet<VoxelChunk>();
            GetChunksIntersecting(box, chunksIntersecting);

            return chunksIntersecting.Count > 0 || GeneratedChunks.Any(chunk => chunk.GetBoundingBox().Intersects(box));
        }

        /// <summary>
        /// Thread that generates new chunks.
        /// </summary>
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
                    // Wait for the periodic signal to generate new chunks.
                    EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                    // If we are to generate chunks...
                    if (!PauseThreads && ToGenerate != null && ToGenerate.Count > 0)
                    {
                        // Determine if the chunk already exists.
                        Point3 box = ToGenerate[0];

                        // If it does not generate a new chunk there.
                        if (!ChunkData.ChunkMap.ContainsKey(box))
                        {
                            var worldPos = new Vector3(box.X*ChunkData.ChunkSizeX, box.Y*ChunkData.ChunkSizeY,
                                box.Z*ChunkData.ChunkSizeZ);
                            VoxelChunk chunk = ChunkGen.GenerateChunk(worldPos, (int) ChunkData.ChunkSizeX,
                                (int) ChunkData.ChunkSizeY, (int) ChunkData.ChunkSizeZ, Components, Content, Graphics);
                            Drawer3D.DrawBox(chunk.GetBoundingBox(), Color.Red, 0.1f);
                            chunk.ShouldRebuild = true;
                            chunk.ShouldRecalculateLighting = true;
                            GeneratedChunks.Enqueue(chunk);
                        }

                        ToGenerate.Remove(box);
                    }

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


        /// <summary>
        /// Renders all of the chunks using the given shader.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="effect">The effect.</param>
        /// <param name="tilemap">The tilemap.</param>
        public void SimpleRender(GraphicsDevice graphicsDevice, Effect effect, Texture2D tilemap)
        {
            effect.Parameters["xIllumination"].SetValue(ChunkData.IllumMap);
            effect.Parameters["xTexture"].SetValue(tilemap);
            effect.Parameters["xSunGradient"].SetValue(ChunkData.SunMap);
            effect.Parameters["xAmbientGradient"].SetValue(ChunkData.AmbientMap);
            effect.Parameters["xTorchGradient"].SetValue(ChunkData.TorchMap);
            effect.Parameters["xTint"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            effect.Parameters["SelfIllumination"].SetValue(0);
            effect.Parameters["xEnableShadows"].SetValue(0);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (var chunk in ChunkData.ChunkMap)
                {
                    chunk.Value.Render(Graphics);
                }
            }
        }

        /// <summary>
        /// Gets a bounding box whih is the union of all chunk bounding boxes.
        /// </summary>
        public BoundingBox GetVisibileBoundingBox()
        {
            List<BoundingBox> toAdd = ChunkData.ChunkMap.Select(chunk => chunk.Value.GetBoundingBox()).ToList();
            return MathFunctions.GetBoundingBox(toAdd);
        }

        /// <summary>
        /// Renders all the chunks which intersect the given camera's frustum.
        /// </summary>
        /// <param name="renderCamera">The camera to render from.</param>
        /// <param name="gameTime">The game time.</param>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="effect">The shader.</param>
        /// <param name="worldMatrix">The world matrix.</param>
        /// <param name="tilemap">The tilemap.</param>
        public void RenderAll(Camera renderCamera, DwarfTime gameTime, GraphicsDevice graphicsDevice, Effect effect,
            Matrix worldMatrix, Texture2D tilemap)
        {
            effect.Parameters["xIllumination"].SetValue(ChunkData.IllumMap);
            effect.Parameters["xTexture"].SetValue(tilemap);
            effect.Parameters["xSunGradient"].SetValue(ChunkData.SunMap);
            effect.Parameters["xAmbientGradient"].SetValue(ChunkData.AmbientMap);
            effect.Parameters["xTorchGradient"].SetValue(ChunkData.TorchMap);
            effect.Parameters["xTint"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            effect.Parameters["SelfIllumination"].SetValue(1);
            effect.Parameters["xEnableShadows"].SetValue(0);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (var chunk in ChunkData.ChunkMap)
                {
                    if (renderCamera.GetFrustrum().Intersects(chunk.Value.GetBoundingBox()))
                    {
                        chunk.Value.Render(Graphics);
                    }
                }
            }
            effect.Parameters["SelfIllumination"].SetValue(0);
        }

        /// <summary>
        /// Renders a shadow map for all the visible chunks.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="shadowRenderer">The shadow renderer.</param>
        /// <param name="worldMatrix">The world matrix.</param>
        /// <param name="tilemap">The tilemap.</param>
        public void RenderShadowmap(Effect effect,
            GraphicsDevice graphicsDevice,
            ShadowRenderer shadowRenderer,
            Matrix worldMatrix,
            Texture2D tilemap)
        {
            var corners = new Vector3[8];
            var tempCamera = new Camera(camera.Target, camera.Position, camera.FOV, camera.AspectRatio, camera.NearPlane,
                30);
            tempCamera.GetFrustrum().GetCorners(corners);
            BoundingBox cameraBox = MathFunctions.GetBoundingBox(corners);
            cameraBox = cameraBox.Expand(1.0f);
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            effect.Parameters["xTexture"].SetValue(tilemap);
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
            effect.CurrentTechnique = effect.Techniques["Textured"];
            effect.Parameters["SelfIllumination"].SetValue(0);
        }

        /// <summary>
        /// Renders light maps for all the chunks.
        /// </summary>
        /// <param name="renderCamera">The render camera.</param>
        /// <param name="gameTime">The game time.</param>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="effect">The effect.</param>
        /// <param name="worldMatrix">The world matrix.</param>
        public void RenderLightmaps(Camera renderCamera, DwarfTime gameTime, GraphicsDevice graphicsDevice,
            Effect effect, Matrix worldMatrix)
        {
            RasterizerState state = RasterizerState.CullNone;
            RasterizerState origState = graphicsDevice.RasterizerState;

            effect.CurrentTechnique = effect.Techniques["Lightmap"];
            effect.Parameters["xIllumination"].SetValue(ChunkData.IllumMap);
            effect.Parameters["xTexture"].SetValue(ChunkData.Tilemap);
            effect.Parameters["xSunGradient"].SetValue(ChunkData.SunMap);
            effect.Parameters["xAmbientGradient"].SetValue(ChunkData.AmbientMap);
            effect.Parameters["xTorchGradient"].SetValue(ChunkData.TorchMap);
            effect.Parameters["xTint"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            effect.Parameters["SelfIllumination"].SetValue(1);
            effect.Parameters["xEnableShadows"].SetValue(GameSettings.Default.UseDynamicShadows ? 1 : 0);
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
            effect.Parameters["SelfIllumination"].SetValue(0);
            effect.CurrentTechnique = effect.Techniques["Textured"];
            graphicsDevice.RasterizerState = origState;
        }

        /// <summary>
        /// Renders all the visible chunks.
        /// </summary>
        /// <param name="renderCamera">The render camera.</param>
        /// <param name="gameTime">The game time.</param>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="effect">The effect.</param>
        /// <param name="worldMatrix">The world matrix.</param>
        public void Render(Camera renderCamera, DwarfTime gameTime, GraphicsDevice graphicsDevice, Effect effect,
            Matrix worldMatrix)
        {
            if (GameSettings.Default.UseLightmaps)
            {
                effect.CurrentTechnique = effect.Techniques["Textured_From_Lightmap"];
                effect.Parameters["xEnableShadows"].SetValue(0);
            }
            else
            {
                effect.CurrentTechnique = effect.Techniques["Textured"];
                effect.Parameters["xEnableShadows"].SetValue(GameSettings.Default.UseDynamicShadows ? 1 : 0);
            }
            effect.Parameters["xIllumination"].SetValue(ChunkData.IllumMap);
            effect.Parameters["xTexture"].SetValue(ChunkData.Tilemap);
            effect.Parameters["xSunGradient"].SetValue(ChunkData.SunMap);
            effect.Parameters["xAmbientGradient"].SetValue(ChunkData.AmbientMap);
            effect.Parameters["xTorchGradient"].SetValue(ChunkData.TorchMap);
            effect.Parameters["xTint"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            effect.Parameters["SelfIllumination"].SetValue(1);
            effect.Parameters["xWorld"].SetValue(Matrix.Identity);

            List<VoxelChunk> renderListCopy = RenderList.ToArray().ToList();

            foreach (VoxelChunk chunk in renderListCopy)
            {
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    if (GameSettings.Default.UseLightmaps)
                    {
                        effect.Parameters["xLightmap"].SetValue(chunk.Primitive.Lightmap);
                        effect.Parameters["pixelSize"].SetValue(new Vector2(1.0f/chunk.Primitive.Lightmap.Width,
                            1.0f/chunk.Primitive.Lightmap.Height));
                    }
                    pass.Apply();
                    chunk.Render(Graphics);
                }
            }
            effect.Parameters["SelfIllumination"].SetValue(0);
            effect.CurrentTechnique = effect.Techniques["Textured"];
        }

        /// <summary>
        /// Generate ore veins/clusters for all chunks. (TODO: Move this to chunk generator!)
        /// </summary>
        public void GenerateOres()
        {
            foreach (VoxelType type in VoxelLibrary.GetTypes())
            {
                if (type.SpawnClusters || type.SpawnVeins)
                {
                    var numEvents = (int) MathFunctions.Rand(75*(1.0f - type.Rarity), 100*(1.0f - type.Rarity));
                    for (int i = 0; i < numEvents; i++)
                    {
                        var clusterBounds = new BoundingBox
                        {
                            Max = new Vector3(Bounds.Max.X, type.MaxSpawnHeight, Bounds.Max.Z),
                            Min = new Vector3(Bounds.Min.X, type.MinSpawnHeight, Bounds.Min.Z)
                        };

                        if (type.SpawnClusters)
                        {
                            var cluster = new OreCluster
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
                            var vein = new OreVein
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
         
        /// <summary>
        /// Generates a set of chunks to start the game with.
        /// </summary>
        /// <param name="origin">The origin of the world.</param>
        /// <param name="message">The loading message.</param>
        public void GenerateInitialChunks(Point3 origin, ref string message)
        {
            float origBuildRadius = GenerateDistance;
            GenerateDistance = origBuildRadius*2.0f;

            int i = 0;
            int iters = WorldSize.X*WorldSize.Y*WorldSize.Z;
            for (int dx = origin.X - WorldSize.X/2 + 1; dx < origin.X + WorldSize.X/2; dx++)
            {
                for (int dy = origin.Y - WorldSize.Y/2; dy <= origin.Y + WorldSize.Y/2; dy++)
                {
                    for (int dz = origin.Z - WorldSize.Z/2 + 1; dz < origin.Z + WorldSize.Z/2; dz++)
                    {
                        message = "Generating : " + (i + 1) + "/" + iters;
                        i++;

                        var box = new Point3(dx, dy, dz);

                        if (!ChunkData.ChunkMap.ContainsKey(box))
                        {
                            var worldPos = new Vector3(box.X*ChunkData.ChunkSizeX, box.Y*ChunkData.ChunkSizeY,
                                box.Z*ChunkData.ChunkSizeZ);
                            VoxelChunk chunk = ChunkGen.GenerateChunk(worldPos, (int) ChunkData.ChunkSizeX,
                                (int) ChunkData.ChunkSizeY, (int) ChunkData.ChunkSizeZ, Components, Content, Graphics);
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
            RecalculateBounds();
            message = "Generating Ores...";

            GenerateOres();

            message = "Fog of war...";
            ChunkData.Reveal(GeneratedChunks.First().MakeVoxel(0, (int) ChunkData.ChunkSizeY - 1, 0),
                new HashSet<VoxelChunk>());

            UpdateRebuildList();
            GenerateDistance = origBuildRadius;

            while (GeneratedChunks.Count > 0)
            {
                VoxelChunk gen = null;
                if (!GeneratedChunks.TryDequeue(out gen))
                {
                    break;
                }
            }

            ChunkData.ChunkManager.CreateGraphics(ref message, ChunkData);
        }

        /// <summary>
        /// Recalculates the world's bounding box.
        /// </summary>
        private void RecalculateBounds()
        {
            List<BoundingBox> boxes = ChunkData.ChunkMap.Select(chunkPair => chunkPair.Value.GetBoundingBox()).ToList();
            Bounds = MathFunctions.GetBoundingBox(boxes);
        }


        /// <summary>
        /// Gets the chunks intersecting the given bounding box.
        /// </summary>
        /// <param name="box">The box.</param>
        /// <param name="chunks">The chunks which intersect.</param>
        public void GetChunksIntersecting(BoundingBox box, HashSet<VoxelChunk> chunks)
        {
            chunks.Clear();
            Point3 minChunk = ChunkData.GetChunkID(box.Min);
            Point3 maxChunk = ChunkData.GetChunkID(box.Max);
            var key = new Point3(0, 0, 0);
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

        /// <summary>
        /// Gets the chunks intersecting the given bounding frustum.
        /// </summary>
        /// <param name="frustum">The frustum.</param>
        /// <param name="chunks">The chunks which intersect.</param>
        public void GetChunksIntersecting(BoundingFrustum frustum, HashSet<VoxelChunk> chunks)
        {
            chunks.Clear();
            BoundingBox frustumBox = MathFunctions.GetBoundingBox(frustum.GetCorners());
            GetChunksIntersecting(frustumBox, chunks);

            chunks.RemoveWhere(chunk => frustum.Contains(chunk.GetBoundingBox()) == ContainmentType.Disjoint);
        
        }

        /// <summary>
        /// Updates the chunks.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="camera">The camera.</param>
        /// <param name="g">The g.</param>
        public void Update(DwarfTime gameTime, Camera camera, GraphicsDevice g)
        {
            UpdateRenderList(camera);

            if (waterUpdateTimer.Update(gameTime))
            {
                WaterUpdateEvent.Set();
            }

            UpdateRebuildList();

            generateChunksTimer.Update(gameTime);
            if (generateChunksTimer.HasTriggered)
            {
                if (ToGenerate.Count > 0)
                {
                    NeedsGenerationEvent.Set();
                    ChunkData.RecomputeNeighbors();
                }

                foreach (VoxelChunk chunk in GeneratedChunks)
                {
                    if (!ChunkData.ChunkMap.ContainsKey(chunk.ID))
                    {
                        ChunkData.AddChunk(chunk);
                        ChunkGen.GenerateVegetation(chunk, Components, Content, Graphics);
                        ChunkGen.GenerateFauna(chunk, Components, Content, Graphics, PlayState.ComponentManager.Factions);
                        List<VoxelChunk> adjacents = ChunkData.GetAdjacentChunks(chunk);
                        foreach (VoxelChunk c in adjacents)
                        {
                            c.ShouldRecalculateLighting = true;
                            c.ShouldRebuild = true;
                        }
                        RecalculateBounds();
                    }
                }

                while (GeneratedChunks.Count > 0)
                {
                    VoxelChunk gen = null;
                    if (!GeneratedChunks.TryDequeue(out gen))
                    {
                        break;
                    }
                }
            }


            visibilityChunksTimer.Update(gameTime);
            if (visibilityChunksTimer.HasTriggered)
            {
                visibleSet.Clear();
                GetChunksIntersecting(camera.GetFrustrum(), visibleSet);
                //RemoveDistantBlocks(camera);
            }


            foreach (VoxelChunk chunk in ChunkData.ChunkMap.Values)
            {
                chunk.Update(gameTime);
            }

            Water.Splash(gameTime);
            Water.HandleTransfers(gameTime);

            var affectedChunks = new HashSet<VoxelChunk>();

            foreach (Voxel voxel in KilledVoxels)
            {
                affectedChunks.Add(voxel.Chunk);
                voxel.Chunk.NotifyDestroyed(new Point3(voxel.GridPosition));
                if (!voxel.IsInterior)
                {
                    foreach (var n in voxel.Chunk.Neighbors)
                    {
                        affectedChunks.Add(n.Value);
                    }
                }
            }

            if (GameSettings.Default.FogofWar)
            {
                ChunkData.Reveal(KilledVoxels, affectedChunks);
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

        /// <summary>
        /// Gets the list of voxels intersecting the given bounding box.
        /// </summary>
        /// <param name="box">The bounding box.</param>
        /// <returns>A list of voxels intersecting the given bounding box.</returns>
        public List<Voxel> GetVoxelsIntersecting(BoundingBox box)
        {
            var intersects = new HashSet<VoxelChunk>();
            GetChunksIntersecting(box, intersects);

            var toReturn = new List<Voxel>();

            foreach (VoxelChunk chunk in intersects)
            {
                toReturn.AddRange(chunk.GetVoxelsIntersecting(box));
            }

            return toReturn;
        }

        /// <summary>
        /// Creates the vertex buffers for the initial chunks.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="chunkData">The chunk data.</param>
        public void CreateGraphics(ref string message, ChunkData chunkData)
        {
            message = "Creating Graphics";
            var toRebuild = new List<VoxelChunk>();

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

                toRebuild.Add(chunk);
            }

            message = "Creating Graphics : Updating Ramps";
            foreach (VoxelChunk chunk in toRebuild.Where(chunk => GameSettings.Default.CalculateRamps))
            {
                chunk.UpdateRamps();
            }

            message = "Creating Graphics : Calculating lighting ";
            int j = 0;
            foreach (VoxelChunk chunk in toRebuild)
            {
                j++;
                message = "Creating Graphics : Calculating lighting " + j + "/" + toRebuild.Count;
                if (chunk.ShouldRecalculateLighting)
                {
                    chunk.CalculateGlobalLight();
                    chunk.ShouldRecalculateLighting = false;
                }
            }

            j = 0;
            foreach (VoxelChunk chunk in toRebuild)
            {
                j++;
                message = "Creating Graphics : Calculating vertex light " + j + "/" + toRebuild.Count;
                chunk.CalculateVertexLighting();
            }

            message = "Creating Graphics: Building Vertices";
            j = 0;
            foreach (VoxelChunk chunk in toRebuild)
            {
                j++;
                message = "Creating Graphics : Building Vertices " + j + "/" + toRebuild.Count;

                if (!chunk.ShouldRebuild)
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

        /// <summary>
        /// Updates the world's bounding box TODO(mklingen) doesn't this already exist!?
        /// </summary>
        public void UpdateBounds()
        {
            List<BoundingBox> boundingBoxes =
                chunkData.ChunkMap.Select(chunkPair => chunkPair.Value.GetBoundingBox()).ToList();
            Bounds = MathFunctions.GetBoundingBox(boundingBoxes);
        }

        /// <summary>
        /// Destroys this instance.
        /// </summary>
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
        /// Gets all the voxels within a radius of the given voxel which match a criteria.
        /// </summary>
        /// <param name="seed">The seed voxel.</param>
        /// <param name="radiusSquared">The radius squared of the search.</param>
        /// <param name="searchEmpty">if set to <c>true</c> search only empty voxels, otherwise search filled ones..</param>
        /// <returns>A list of voxels that can be reached from the seed matching the criteria.</returns>
        public List<Voxel> BreadthFirstSearch(Voxel seed, float radiusSquared, bool searchEmpty = true)
        {
            var queue = new Queue<Voxel>();
            queue.Enqueue(seed);
            var outList = new List<Voxel>();
            Func<Voxel, bool> searchQuery = (Voxel v) => v.IsEmpty;
            if (!searchEmpty)
            {
                searchQuery = v => !v.IsEmpty;
            }


            while (queue.Count > 0)
            {
                Voxel curr = queue.Dequeue();
                if (curr != null && searchQuery(curr) && !outList.Contains(curr) &&
                    (curr.Position - seed.Position).LengthSquared() < radiusSquared)
                {
                    outList.Add(curr);
                    var neighbors = new List<Voxel>(6);
                    curr.Chunk.GetNeighborsManhattan((int) curr.GridPosition.X, (int) curr.GridPosition.Y,
                        (int) curr.GridPosition.Z, neighbors);

                    foreach (Voxel voxel in neighbors)
                    {
                        if (voxel != null && !outList.Contains(voxel) && searchQuery(voxel))
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