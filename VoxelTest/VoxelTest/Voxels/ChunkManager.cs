using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    public class ChunkManager
    {


        public ConcurrentDictionary<Point3, VoxelChunk> ChunkMap { get; set; }
        
        public ConcurrentQueue<VoxelChunk> RenderList { get; set; }
        public ConcurrentQueue<VoxelChunk> RebuildList { get; set; }
        public ConcurrentQueue<VoxelChunk> RebuildLiquidsList { get; set; }

        public Texture2D Tilemap { get; set; }
        public Texture2D IllumMap { get; set; }
        public ChunkGenerator ChunkGen { get; set; }
        public List<BoundingBox> PotentialChunks { get; set; }
        public ConcurrentQueue<VoxelChunk> GeneratedChunks { get; set; }
        public List<BoundingBox> ToGenerate { get; set; }
        public int MaxChunks { get { return (int)GameSettings.Default.MaxChunks; } set { GameSettings.Default.MaxChunks = (ulong)value; } }

        private Thread GeneratorThread { get; set; }
        private Mutex GeneratorLock { get; set; }
        static AutoResetEvent NeedsGenerationEvent = new AutoResetEvent(false);


        private Thread RebuildThread { get; set; }
        private Thread RebuildLiquidThread { get; set; }
        private Mutex RebuildLock { get; set; }
        private Mutex LiquidLock { get; set; }
        static AutoResetEvent NeedsRebuildEvent = new AutoResetEvent(false);
        static AutoResetEvent NeedsLiquidEvent = new AutoResetEvent(false);

        private Thread WaterThread { get; set; }
        private AutoResetEvent WaterUpdateEvent = new AutoResetEvent(true);


        private Timer GenerateChunksTimer = new Timer(0.5f, false);
        private Timer RebuildChunksTimer = new Timer(0.5f, false);
        private Timer VisibilityChunksTimer = new Timer(0.5f, false);
        private Timer WaterUpdateTimer = new Timer(1.0f, false);

        public float DrawDistance { get { return GameSettings.Default.ChunkDrawDistance; } set { GameSettings.Default.ChunkDrawDistance = value; m_drawDistSq = value * value; } }
        protected float m_drawDistSq = 0;
        public float DrawDistanceSquared { get { return m_drawDistSq; } set { m_drawDistSq = value; GameSettings.Default.ChunkDrawDistance = (float)Math.Sqrt(value); } }
        public float RemoveDistance { get { return GameSettings.Default.ChunkUnloadDistance; } set { GameSettings.Default.ChunkDrawDistance = value; } }
        public float GenerateDistance { get { return GameSettings.Default.ChunkGenerateDistance; } set { GameSettings.Default.ChunkDrawDistance = value; } }
        public GraphicsDevice Graphics { get; set; }
        public float MaxViewingLevel { get; set;}

        public bool PauseThreads { get; set; }

        public enum SliceMode
        {
            X, Y, Z
        }

        public SliceMode Slice { get; set; }

        private Camera m_camera = null;

        public ComponentManager Components { get; set; }
        public ContentManager Content { get; set; }

        public Octree ChunkOctree { get; set; }

        private HashSet<VoxelChunk> m_intersecting = new HashSet<VoxelChunk>();
        private HashSet<VoxelChunk> m_visibleSet = new HashSet<VoxelChunk>();

        public WaterManager Water { get; set; }

        public Texture2D SunMap { get; set; }
        public Texture2D AmbientMap { get; set; }
        public Texture2D TorchMap { get; set; }

        public  uint ChunkSizeX { get; set; }
        public  uint ChunkSizeY { get; set; }
        public  uint ChunkSizeZ { get; set; }
        public float InvCSX { get; set; }
        public float InvCSY { get; set; }
        public float InvCSZ { get; set;}

        public List<DynamicLight> DynamicLights { get; set; }

        public ChunkManager(ContentManager Content, uint chunkSizeX, uint chunkSizeY, uint chunkSizeZ, Camera camera, GraphicsDevice graphics, Texture2D tilemap, Texture2D illumMap, Texture2D sunMap, Texture2D ambientMap, Texture2D torchMap, ChunkGenerator chunkGen)
        {
            m_drawDistSq = DrawDistance * DrawDistance;
            this.Content = Content;
            ChunkSizeX = chunkSizeX;
            ChunkSizeY = chunkSizeY;
            ChunkSizeZ = chunkSizeZ;

            InvCSX = 1.0f / chunkSizeX;
            InvCSY = 1.0f / chunkSizeY;
            InvCSZ = 1.0f / chunkSizeZ;

            Tilemap = tilemap;
            IllumMap = illumMap;
            LiquidLock = new Mutex();
            ChunkMap = new ConcurrentDictionary<Point3, VoxelChunk>();
            RenderList = new ConcurrentQueue<VoxelChunk>();
            RebuildList = new ConcurrentQueue<VoxelChunk>();
            RebuildLiquidsList = new ConcurrentQueue<VoxelChunk>();
            ChunkGen = chunkGen;
            PotentialChunks = new List<BoundingBox>();

            GeneratedChunks = new ConcurrentQueue<VoxelChunk>();
            GeneratorLock = new Mutex();
            GeneratorThread = new Thread(this.GenerateThread);

            RebuildLock = new Mutex();
            RebuildThread = new Thread(this.RebuildVoxelsThread);
            RebuildLiquidThread = new Thread(this.RebuildLiquidsThread);

            WaterThread = new Thread(this.UpdateWaterThread);

            ToGenerate = new List<BoundingBox>();
            Graphics = graphics;

            chunkGen.Manager = this;

            MaxViewingLevel = chunkSizeY;

            GenerateChunksTimer = new Timer(GameSettings.Default.ChunkGenerateTime, false);
            RebuildChunksTimer = new Timer(GameSettings.Default.ChunkRebuildTime, false);
            VisibilityChunksTimer = new Timer(GameSettings.Default.VisibilityUpdateTime, false);
            GenerateChunksTimer.HasTriggered = true;
            RebuildChunksTimer.HasTriggered = true;
            VisibilityChunksTimer.HasTriggered = true;
            m_camera = camera;

            Water = new WaterManager(this);

            ChunkOctree = new Octree(new BoundingBox(new Vector3(-2000, -2000, -2000), new Vector3(2000, 2000, 2000)), 10, 4, 1);

            SunMap = sunMap;
            AmbientMap = ambientMap;
            TorchMap = torchMap;

            DynamicLights = new List<DynamicLight>();

            Slice = SliceMode.Y;
            PauseThreads = false;
            //ChunkOctree.DebugDraw = true;
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
            EventWaitHandle[] waitHandles = new EventWaitHandle[] { WaterUpdateEvent, Program.shutdownEvent };

            bool shouldExit = false;
            while (!shouldExit && !GeometricPrimitive.ExitGame)
            {
                EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                if (wh == Program.shutdownEvent)
                {
                    shouldExit = true;
                    break;
                }

                if (!PauseThreads)
                {
                    Water.UpdateWater();
                }
            }

        }

        public void RebuildLiquidsThread()
        {

            EventWaitHandle[] waitHandles = new EventWaitHandle[] { NeedsLiquidEvent, Program.shutdownEvent };

            bool shouldExit = false;
            while (!shouldExit && !GeometricPrimitive.ExitGame)
            {
                EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                if (wh == Program.shutdownEvent)
                {
                    shouldExit = true;
                    break;
                }

                while (!PauseThreads && RebuildLiquidsList.Count > 0 )
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
                    catch(Exception e)
                    {
                        Console.Error.WriteLine(e.Message);
                        shouldExit = true;
                        break;
                    }
                }

               
            }
        }


        public void RebuildVoxelsThread()
        {
            EventWaitHandle[] waitHandles = new EventWaitHandle[] { NeedsRebuildEvent, Program.shutdownEvent};
            bool shouldExit = false;
    
            while (!shouldExit && !GeometricPrimitive.ExitGame)
            {
                EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                if (wh == Program.shutdownEvent)
                {
                    break;
                }
                {
                    if (PauseThreads)
                    {
                        continue;
                    }

                    if (RebuildList.Count > 0)
                    {
                        UpdateDynamicLights();
                    }

                    Dictionary<Point3, VoxelChunk> ToRebuild = new Dictionary<Point3, VoxelChunk>();
                    bool calculateRamps = GameSettings.Default.CalculateRamps;
                    while (RebuildList.Count > 0)
                    {
                        VoxelChunk chunk = null;

                        //RebuildLock.WaitOne();
                        if (!RebuildList.TryDequeue(out chunk))
                        {
                            //RebuildLock.ReleaseMutex();
                            break;
                        }
                        //RebuildLock.ReleaseMutex();

                        if (chunk == null)
                        {
                            continue;
                        }

                        ToRebuild[chunk.ID] = chunk;

                        if (PauseThreads)
                        {
                            break;
                        }

                    }

                    if (calculateRamps)
                    {
                        foreach (KeyValuePair<Point3, VoxelChunk> chunkPair in ToRebuild)
                        {
                            VoxelChunk chunk = chunkPair.Value;
                            chunk.UpdateRamps();
                        }
                    }


                    /*
                    foreach (KeyValuePair<Point3, VoxelChunk> chunkPair in ToRebuild)
                    {
                        VoxelChunk chunk = chunkPair.Value;
                        chunk.UpdateMaxViewingLevel();
                    }

                    // First pass for quick graphics
                    foreach (KeyValuePair<Point3, VoxelChunk> chunkPair in ToRebuild)
                    {
                        VoxelChunk chunk = chunkPair.Value;
                        if (chunk.RebuildPending && chunk.ShouldRebuild)
                        {
                            chunk.Rebuild(Graphics);
                            chunk.ShouldRebuild = true;
                        }
                    }


                    foreach (KeyValuePair<Point3, VoxelChunk> chunkPair in ToRebuild)
                    {
                        VoxelChunk chunk = chunkPair.Value;
                        if (chunk.ShouldRecalculateLighting)
                        {
                            chunk.CalculateLighting();
                            chunk.ShouldRecalculateLighting = false;
                        }

                    }
                     */

                    foreach (KeyValuePair<Point3, VoxelChunk> chunkPair in ToRebuild)
                    {
                        VoxelChunk chunk = chunkPair.Value;
                        if (chunk.ShouldRecalculateLighting)
                        {
                            chunk.CalculateGlobalLight();
                        }

                    }

                    foreach (KeyValuePair<Point3, VoxelChunk> chunkPair in ToRebuild)
                    {
                        VoxelChunk chunk = chunkPair.Value;
                        if (chunk.RebuildPending && chunk.ShouldRebuild)
                        {
                            chunk.UpdateMaxViewingLevel();

                            if (chunk.ShouldRecalculateLighting)
                            {
                                //SimpleDrawing.DrawBox(chunk.GetBoundingBox(), Color.White, 0.1f);
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

        public int CompareChunkDistance(VoxelChunk A, VoxelChunk B)
        {
            if (A == B || !A.IsVisible && !B.IsVisible)
            {
                return 0;
            }
            else
            {
                if (!A.IsVisible)
                {
                    return 1;
                }
                else if (!B.IsVisible)
                {
                    return -1;
                }

                float dA = (A.Origin - m_camera.Position + new Vector3(A.SizeX / 2.0f, A.SizeY / 2.0f, A.SizeZ / 2.0f)).LengthSquared();
                float dB = (B.Origin - m_camera.Position + new Vector3(B.SizeX / 2.0f, B.SizeY / 2.0f, B.SizeZ / 2.0f)).LengthSquared();

                if (dA < dB)
                {
                    return -1;
                }
                else return 1;
            }
        }



        public void SetMaxViewingLevel(float level, SliceMode slice)
        {
            Slice = slice;
            MaxViewingLevel = Math.Max(Math.Min(level, ChunkSizeY), 1);

            foreach (KeyValuePair<Point3, VoxelChunk> chunks in ChunkMap)
            {
                VoxelChunk c = chunks.Value;

                if (c.NeedsViewingLevelChange())
                {
                    c.ShouldRecalculateLighting = false;
                    c.ShouldRebuild = true;
                }
            }
        }

        public Ray GetMouseRay(MouseState mouse, Camera camera, Viewport viewPort)
        {
            float x = mouse.X;
            float y = mouse.Y;
            Vector3 pos1 = viewPort.Unproject(new Vector3(x, y, 0), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(x, y, 1), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);
            return new Ray(pos1, dir);
        }
        
        public Voxel GetFirstVisibleBlockHitByMouse(MouseState mouse, Camera camera, Viewport viewPort)
        {
            Voxel vox =  GetFirstVisibleBlockHitByScreenCoord(mouse.X, mouse.Y, camera, viewPort, 50.0f);

            if (vox == null)
            {
                return null;
            }

            return vox;
        }

        public Voxel GetFirstVisibleBlockHitByScreenCoord(int x, int y, Camera camera, Viewport viewPort, float dist)
        {
            Vector3 pos1 = viewPort.Unproject(new Vector3(x, y, 0), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(x, y, 1), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);
            Voxel vox = GetFirstVisibleBlockHitByRay(pos1, pos1 + dir * dist, false);

            if (vox == null)
            {
                return null;
            }

            return vox;
        }

        public Voxel GetFirstVisibleBlockUnder(Vector3 rayStart, bool nullCheck)
        {
            VoxelChunk startChunk = GetVoxelChunkAtWorldLocation(rayStart);

            if (startChunk == null)
            {
                return null;
            }

            Point3 point = new Point3(startChunk.WorldToGrid(rayStart));

            for (int y = point.Y; y > 0; y--)
            {
                Voxel vox = startChunk.VoxelGrid[point.X][y][point.Z];
                if (vox != null && (vox.IsVisible || nullCheck))
                {
                    return vox;
                }
            }

            return null;
        }

        public Voxel GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd)
        {
            VoxelChunk startChunk = GetVoxelChunkAtWorldLocation(rayStart);

            return GetFirstVisibleBlockHitByRay(rayStart, rayEnd, null, startChunk, false);
        }

        public Voxel GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd, bool draw)
        {
            VoxelChunk startChunk = GetVoxelChunkAtWorldLocation(rayStart);

            return GetFirstVisibleBlockHitByRay(rayStart, rayEnd, null, startChunk, draw);
        }

        public Voxel GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd, Voxel ignore, VoxelChunk startChunk, bool draw)
        {
            Vector3 delta = rayEnd - rayStart;
            float length = delta.Length();
            delta.Normalize();

            
            Vector3 pos = Vector3.Zero;

            List<Voxel> atPos = new List<Voxel>(); 
            for (float dn = 0.0f; dn < length; dn += 0.2f)
            {
                pos = rayStart + delta * dn;

                atPos.Clear();
                GetNonNullVoxelsAtWorldLocationCheckFirst(startChunk, pos, atPos, 0);

                if (draw && atPos.Count > 0)
                {
                    SimpleDrawing.DrawBox(new BoundingBox(pos, pos + new Vector3(0.01f, 0.01f, 0.01f)), Color.White, 0.01f);
                }
                if (atPos.Count > 0)
                {
                    float closestDist = 100000;
                    Voxel closestVox = null;
                    foreach (Voxel v in atPos)
                    {

                        if (v != null && v.IsVisible && v != ignore)
                        {
                            float d = (v.Position + new Vector3(0.5f, 0.5f, 0.5f) - rayStart).LengthSquared() ;
                            if(d < closestDist)
                            {
                                closestDist = d;
                                closestVox = v;
                            }
                        }
                    }

                    if(closestVox != null)
                    {
                        return closestVox;
                    }
                }
            }

            return null;
        }

        public void AddChunk(VoxelChunk chunk)
        {
            if (ChunkMap.Count < MaxChunks && !ChunkMap.ContainsKey(chunk.ID))
            {
                ChunkMap[chunk.ID] = chunk;
                ChunkOctree.AddObjectRecursive(chunk);
            }
            else if (ChunkMap.ContainsKey(chunk.ID))
            {
                RemoveChunk(ChunkMap[chunk.ID]);
                ChunkMap[chunk.ID] = chunk;
                ChunkOctree.AddObjectRecursive(chunk);
            }


        }

        public void RemoveChunk(VoxelChunk chunk)
        {
            VoxelChunk removed = null;
            while (!ChunkMap.TryRemove(chunk.ID, out removed))
            {
                ChunkOctree.Root.RemoveObject(chunk);
            }

            HashSet<LocatableComponent> locatables = new HashSet<LocatableComponent>();

            LocatableComponent.m_octree.Root.GetComponentsIntersecting(chunk.GetBoundingBox(), locatables);

            foreach (LocatableComponent component in locatables)
            {
                component.IsDead = true;
            }

            chunk.Destroy(Graphics);


        }

        public void RemoveDistantBlocks(Camera camera)
        {
            List<VoxelChunk> toRemove = new List<VoxelChunk>();
            foreach (KeyValuePair<Point3, VoxelChunk> chunkpair in ChunkMap)
            {
                VoxelChunk chunk = chunkpair.Value;
                BoundingBox box = chunk.GetBoundingBox();

                if((camera.Position - box.Min).Length() > RemoveDistance)
                {
                    toRemove.Add(chunk);
                    PotentialChunks.Add(box);
                }
            }

            foreach(VoxelChunk chunk in toRemove)
            {
                RemoveChunk(chunk);
            }
        }

        public void UpdateRenderList(Camera camera)
        {
            while(RenderList.Count > 0)
            {
                VoxelChunk result = null;
                if (!RenderList.TryDequeue(out result))
                {
                    break;
                }
            }



            foreach (VoxelChunk chunk in m_visibleSet)
            {
                BoundingBox box = chunk.GetBoundingBox();

                if ((camera.Position - (box.Min + box.Max) * 0.5f).Length() < DrawDistance)
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

        public List<VoxelChunk> rebuildTest = new List<VoxelChunk>();

        public void UpdateRebuildList()
        {
            List<VoxelChunk> toRebuild = new List<VoxelChunk>();
            List<VoxelChunk> toRebuildLiquids = new List<VoxelChunk>();
            rebuildTest = toRebuild;
            foreach (KeyValuePair<Point3, VoxelChunk> chunks in ChunkMap)
            {
                VoxelChunk chunk = chunks.Value;
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


            if (toRebuild.Count > 0)
            {
                toRebuild.Sort(CompareChunkDistance);
                //RebuildLock.WaitOne();
                foreach (VoxelChunk chunk in toRebuild)
                {
                    RebuildList.Enqueue(chunk);
                }
                //RebuildLock.ReleaseMutex();
            }

            if (toRebuildLiquids.Count > 0)
            {
                toRebuildLiquids.Sort(CompareChunkDistance);

                //LiquidLock.WaitOne();
                foreach (VoxelChunk chunk in toRebuildLiquids)
                {
                    if (!RebuildLiquidsList.Contains(chunk))
                    {
                        RebuildLiquidsList.Enqueue(chunk);
                    }
                }
                //LiquidLock.ReleaseMutex();
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

        public List<VoxelChunk> GetAdjacentChunks(VoxelChunk chunk)
        {
            List<VoxelChunk> toReturn = new List<VoxelChunk>();
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dz = -1; dz < 2; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }
                    else
                    {
                        Point3 key = new Point3(chunk.ID.X + dx, 0, chunk.ID.Z + dz);

                        if (ChunkMap.ContainsKey(key))
                        {
                            toReturn.Add(ChunkMap[key]);
                        }
                    }
                }
            }

            return toReturn;

        }

        public bool BoundingBoxIntersectsWorld(BoundingBox box)
        {
            HashSet<VoxelChunk> chunksIntersecting = new HashSet<VoxelChunk>();
            ChunkOctree.Root.GetComponentsIntersecting<VoxelChunk>(box, chunksIntersecting);

            return chunksIntersecting.Count > 0;
        }

        public void GenerateThread()
        {
            EventWaitHandle[] waitHandles = new EventWaitHandle[] { NeedsGenerationEvent, Program.shutdownEvent };
            while (true)
            {
                EventWaitHandle wh = Datastructures.WaitFor(waitHandles);

                //GeneratorLock.WaitOne();



                if (!PauseThreads && ToGenerate != null && ToGenerate.Count > 0)
                {
                    BoundingBox box = ToGenerate[0];
                    BoundingBox smaller = new BoundingBox(box.Min + new Vector3(0.05f, 0.05f, 0.05f), box.Max - new Vector3(0.05f, 0.05f, 0.05f));


                    bool intersectsWorld = BoundingBoxIntersectsWorld(smaller);

                    if (!intersectsWorld)
                    {
                        VoxelChunk chunk = ChunkGen.GenerateChunk(box.Min , (int)(box.Max.X - box.Min.X), (int)(box.Max.Y - box.Min.Y), (int)(box.Max.Z - box.Min.Z), Components, Content, Graphics);
                        chunk.ShouldRebuild = true;
                        chunk.ShouldRecalculateLighting = true;
                        GeneratedChunks.Enqueue(chunk);
                        
                    }

                    ToGenerate.Remove(box);
                }


                //GeneratorLock.ReleaseMutex();
                if (wh == Program.shutdownEvent)
                {
                    break;
                }
            }
        }

        public void BuildAllGrass()
        {
            foreach (KeyValuePair<Point3, VoxelChunk> pair in ChunkMap)
            {
                pair.Value.BuildGrassMotes();
            }
        }

        public void GenerateNewChunks(Camera camera, bool frustrumCull)
        {

            if (ChunkMap.Count > MaxChunks)
            {
                return;
            }

            if (ToGenerate.Count > 0)
            {
                NeedsGenerationEvent.Set();
            }


            List<BoundingBox> toRemove = new List<BoundingBox>();
            List<BoundingBox> toAdd = new List<BoundingBox>();


            int originalCount = PotentialChunks.Count;
            for (int i = 0; i < originalCount; i++)
            {
                BoundingBox box = PotentialChunks[i];
                if (!frustrumCull || (camera.IsInView(box) && ((camera.Position - box.Min).Length() < GenerateDistance)))
                {
                    toRemove.Add(box);

                    ToGenerate.Add(box);
                    for (int dx = -1; dx < 2; dx++)
                    {
                        for (int dy = -1; dy < 2; dy++)
                        {

                            if (dx == 0 && dy == 0)
                            {
                                continue;
                            }

                            Vector3 extents = box.Max - box.Min;
                            float xWidth = (box.Max.X - box.Min.X) * dx;
                            float zWidth = (box.Max.Z - box.Min.Z) * dy;

                            BoundingBox newPotential = new BoundingBox(box.Min + new Vector3(xWidth, 0, zWidth), box.Max + new Vector3(xWidth, 0, zWidth));
                            BoundingBox smaller = new BoundingBox(newPotential.Min + new Vector3(2f, 2f, 2f), newPotential.Max - new Vector3(2f, 2f, 2f));

                            bool intersects = false;

                            foreach (BoundingBox potential in PotentialChunks)
                            {
                                if (potential.Intersects(smaller))
                                {
                                    intersects = true;
                                    break;
                                }
                            }

                            foreach (BoundingBox potential in toAdd)
                            {
                                if (potential.Intersects(smaller))
                                {
                                    intersects = true;
                                    break;
                                }
                            }

                            if (!intersects && !BoundingBoxIntersectsWorld(smaller))
                            {
                                toAdd.Add(newPotential);
                            }

                        }

                    }

                    foreach (BoundingBox add in toAdd)
                    {
                        PotentialChunks.Add(add);
                    }

                    toAdd.Clear();
                }


            }

            foreach (BoundingBox box in toRemove)
            {
                PotentialChunks.Remove(box);
            }



        }

        public void RecomputeNeighbors()
        {


            foreach (KeyValuePair<Point3, VoxelChunk> chunks in ChunkMap)
            {
                VoxelChunk chunk = chunks.Value;
                chunk.Neighbors.Clear();
            }

            foreach (KeyValuePair<Point3, VoxelChunk> chunks in ChunkMap)
            {
                VoxelChunk chunk = chunks.Value;
                List<VoxelChunk> adjacents = GetAdjacentChunks(chunk);
                foreach (VoxelChunk c in adjacents)
                {
                    if (!c.Neighbors.ContainsKey(chunk.ID) && chunk != c)
                    {
                        c.Neighbors[chunk.ID] = (chunk);
                    }
                    chunk.Neighbors[c.ID] = c;
                }
            }
            

        }

        public void Render(Camera camera, GameTime gameTime, GraphicsDevice graphicsDevice, Effect effect, Matrix worldMatrix)
        {
            effect.Parameters["xIllumination"].SetValue(IllumMap);
            effect.Parameters["xTexture"].SetValue(Tilemap);
            effect.Parameters["xSunGradient"].SetValue(SunMap);
            effect.Parameters["xAmbientGradient"].SetValue(AmbientMap);
            effect.Parameters["xTorchGradient"].SetValue(TorchMap);
            effect.Parameters["xTint"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            effect.Parameters["SelfIllumination"].SetValue(true);
            foreach (VoxelChunk chunk in RenderList)
            {
                chunk.Render(Tilemap, IllumMap, SunMap, AmbientMap, TorchMap, graphicsDevice, effect, worldMatrix);
            }

            ChunkOctree.Root.Draw();
        }

        public void GenerateInitialChunks(OrbitCamera camera, ref string message)
        {
            float origBuildRadius = GenerateDistance;
            GenerateDistance = origBuildRadius * 2.0f;
            int iters = 4;
            for (int i = 0; i < iters; i++)
            {
                message = "Generating : " + (i + 1) + "/" + iters;
                camera.Radius = 0.01f;
                camera.Phi = -1.57f / 4.0f;
                camera.Theta = (3.14159f / (iters * 0.5f)) * (i + 1);
                camera.UpdateProjectionMatrix();
                camera.UpdateViewMatrix();
                camera.UpdateBasisVectors();
             
                GenerateNewChunks(camera, false);

                while(ToGenerate != null && ToGenerate.Count > 0)
                {
                    BoundingBox box = ToGenerate[0];
                    BoundingBox smaller = new BoundingBox(box.Min + new Vector3(0.05f, 0.05f, 0.05f), box.Max - new Vector3(0.05f, 0.05f, 0.05f));


                    bool intersectsWorld = BoundingBoxIntersectsWorld(smaller);

                    if (!intersectsWorld)
                    {
                        VoxelChunk chunk = ChunkGen.GenerateChunk(box.Min, (int)(box.Max.X - box.Min.X), (int)(box.Max.Y - box.Min.Y) , (int)(box.Max.Z - box.Min.Z), Components, Content, Graphics);
                        chunk.ShouldRebuild = true;
                        chunk.ShouldRecalculateLighting = true;
                        chunk.IsVisible = true;
                        chunk.ResetSunlight(0);
                        GeneratedChunks.Enqueue(chunk);
                        foreach (VoxelChunk chunk2 in GeneratedChunks)
                        {
                            AddChunk(chunk2);
                        }
                    }

                    ToGenerate.RemoveAt(0);
                }



                UpdateRebuildList();

            }


            while (GeneratedChunks.Count > 0)
            {
                VoxelChunk gen = null;
                if (!GeneratedChunks.TryDequeue(out gen))
                {
                    break;
                }
            } 

            message = "Creating Graphics";
            List<VoxelChunk> ToRebuild = new List<VoxelChunk>();
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

                ToRebuild.Add(chunk);
            }

            message = "Creating Graphics : Updating Max Viewing Level";
            foreach (VoxelChunk chunk in ToRebuild)
            {
                chunk.UpdateMaxViewingLevel();
            }

            message = "Creating Graphics : Updating Ramps";
            foreach (VoxelChunk chunk in ToRebuild)
            {
                if (GameSettings.Default.CalculateRamps)
                {
                    chunk.UpdateRamps();
                }
            }

            message = "Creating Graphics : Calculating lighting ";
            int j = 0;
            foreach (VoxelChunk chunk in ToRebuild)
            {
                j++;
                message = "Creating Graphics : Calculating lighting " + j + "/" + ToRebuild.Count; 
                if (chunk.ShouldRecalculateLighting)
                {
                    chunk.CalculateGlobalLight();
                    chunk.ShouldRecalculateLighting = false;
                }

            }

            j = 0;
            foreach (VoxelChunk chunk in ToRebuild)
            {
                j++;
                message = "Creating Graphics : Calculating vertex light " + j + "/" + ToRebuild.Count;
                chunk.CalculateVertexLighting();
            }

            message = "Creating Graphics: Building Vertices";
            j = 0;
            foreach (VoxelChunk chunk in ToRebuild)
            {
                j++;
                message = "Creating Graphics : Building Vertices " + j + "/" + ToRebuild.Count;

                if (chunk.ShouldRebuild)
                {
                    chunk.Rebuild(Graphics);
                    chunk.ShouldRebuild = false;
                    chunk.RebuildPending = false;
                    chunk.RebuildLiquidPending = false;
                }
            }

            RecomputeNeighbors();

            GenerateDistance = origBuildRadius;


            message = "Cleaning Up.";
            

        }

        public void Update(GameTime gameTime, Camera camera, GraphicsDevice g)
        {
            UpdateRenderList(camera);

            if (WaterUpdateTimer.HasTriggered)
            {
                WaterUpdateEvent.Set();
            }
            else
            {
                WaterUpdateTimer.Update(gameTime);
            }

            UpdateRebuildList();

            if (GenerateChunksTimer.HasTriggered)
            {
                if (ToGenerate.Count > 0)
                {
                    NeedsGenerationEvent.Set();
                    RecomputeNeighbors();
                }

                GenerateNewChunks(camera, true);

 

                foreach (VoxelChunk chunk in GeneratedChunks)
                {
                    AddChunk(chunk);
                    List<VoxelChunk> adjacents = GetAdjacentChunks(chunk);
                    foreach (VoxelChunk c in adjacents)
                    {
                        c.ShouldRecalculateLighting = true;
                        c.ShouldRebuild = true;
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
            else
            {
                GenerateChunksTimer.Update(gameTime);
            }
             

            
            if (VisibilityChunksTimer.HasTriggered)
            {
                m_visibleSet.Clear();
                ChunkOctree.Root.GetComponentsIntersecting<VoxelChunk>(camera.GetFrustrum(), m_visibleSet);
                RemoveDistantBlocks(camera);
            }
            else
            {
                VisibilityChunksTimer.Update(gameTime);
            }


            foreach (VoxelChunk chunk in ChunkMap.Values)
            {
                chunk.Update(gameTime);
            }

            Water.Splash(gameTime);
            Water.HandleTransfers(gameTime);
           
        }

        public  Vector3 RoundToChunkCoords(Vector3 location)
        {
            int x = (int)(location.X * InvCSX);
            int y = (int)(location.Y * InvCSY);
            int z = (int)(location.Z * InvCSZ);


            return new Vector3(x, y, z);
        }

        public VoxelChunk GetVoxelChunkAtWorldLocation(Vector3 worldLocation)
        {
            Point3 id = GetChunkID(worldLocation);

            if (ChunkMap.ContainsKey(id))
            {
                return ChunkMap[id];
            }


                return null;
        }

        public List<VoxelRef> GetVoxelReferencesAtWorldLocation(Vector3 worldLocation)
        {
            List<VoxelRef> toReturn = new List<VoxelRef>();
            GetVoxelReferencesAtWorldLocation(null, worldLocation, toReturn);
            return toReturn;
        }

        public void GetVoxelReferencesAtWorldLocation(VoxelChunk checkFirst, Vector3 worldLocation, List<VoxelRef> toReturn)
        {
              if (checkFirst != null )
                {
                    if (checkFirst.IsWorldLocationValid(worldLocation))
                    {
                        Voxel v = checkFirst.GetVoxelAtWorldLocation(worldLocation);

                        if (v != null)
                        {
                            toReturn.Add(v.GetReference());
                        }
                        else
                        {
                            Vector3 grid = checkFirst.WorldToGrid(worldLocation);
                            VoxelRef newReference = new VoxelRef();
                            newReference.ChunkID = checkFirst.ID;
                            newReference.GridPosition = new Vector3((int)grid.X, (int)grid.Y, (int)grid.Z);
                            newReference.WorldPosition = newReference.GridPosition + checkFirst.Origin;
                            newReference.TypeName = "empty";
                            newReference.isValid = true;
                            toReturn.Add(newReference);
                        }
                    }
                    else
                    {
                         GetVoxelReferencesAtWorldLocation(null, worldLocation, toReturn);
                        
                        /*
                        foreach (VoxelChunk n in checkFirst.Neighbors.Values)
                        {
                            if (n.IsWorldLocationValid(worldLocation))
                            {
                                Voxel voxel = n.GetVoxelAtWorldLocation(worldLocation);

                                if (voxel != null)
                                {
                                    toReturn.Add(voxel.GetReference());
                                }
                                else
                                {
                                    VoxelRef newReference = new VoxelRef();
                                    newReference.ChunkID = n.ID;
                                    Vector3 grid = n.WorldToGrid(worldLocation);
                                    newReference.GridPosition = new Vector3((int)grid.X, (int)grid.Y, (int)grid.Z);
                                    newReference.WorldPosition = newReference.GridPosition + n.Origin;
                                    newReference.TypeName = "empty";
                                    newReference.isValid = true;
                                    toReturn.Add(newReference);
                                }
                                break;
                            }

                        }
                        */
                    }
                         
                }
                else
                {
                    VoxelChunk v = GetVoxelChunkAtWorldLocation(worldLocation);

                    if (v != null)
                    {
                        Voxel got = v.GetVoxelAtWorldLocation(worldLocation);

                        if (got != null)
                        {
                            toReturn.Add(got.GetReference());
                        }
                        else
                        {
                            Vector3 grid = v.WorldToGrid(worldLocation);
                            VoxelRef newReference = new VoxelRef();
                            newReference.ChunkID = v.ID;
                            newReference.GridPosition = new Vector3((int)grid.X, (int)grid.Y, (int)grid.Z);
                            newReference.WorldPosition = newReference.GridPosition + v.Origin;
                            newReference.TypeName = "empty";
                            newReference.isValid = true;
                            toReturn.Add(newReference);
                        }
                        
                    }

                  /*
                    m_intersecting.Clear();
                    m_intersecting.Add(ChunkOctree.Root.GetComponentIntersecting<VoxelChunk>(worldLocation));

                    foreach (VoxelChunk v in m_intersecting)
                    {
                        if (v != null)
                        {
                            if (v.IsWorldLocationValid(worldLocation))
                            {
                                Voxel got = v.GetVoxelAtWorldLocation(worldLocation);
                            
                                if (got != null)
                                {
                                    toReturn.Add(got.GetReference());
                                }
                                else
                                {
                                    Vector3 grid = v.WorldToGrid(worldLocation);
                                    VoxelRef newReference = new VoxelRef();
                                    newReference.ChunkID = v.ID;
                                    newReference.GridPosition = new Vector3((int)grid.X, (int)grid.Y, (int)grid.Z);
                                    newReference.WorldPosition = newReference.GridPosition + v.Origin;
                                    newReference.TypeName = "empty";
                                    newReference.isValid = true;
                                    toReturn.Add(newReference);
                                }
                            }

                        }
                    }
                   */
                }

                return;
            
        }

        public Point3 GetChunkID(Vector3 origin)
        {
            origin = RoundToChunkCoords(origin);
            return new Point3((int)origin.X, (int)origin.Y, (int)origin.Z);
        }

        public List<Voxel> GetNonNullVoxelsAtWorldLocation(Vector3 worldLocation, int depth)
        {
            List<Voxel> toReturn = new List<Voxel>();
            GetNonNullVoxelsAtWorldLocationCheckFirst(null, worldLocation, toReturn, depth);
            return toReturn;
        }

        public float GetFilledVoxelGridHeightAt(float x, float y, float z)
        {
            VoxelChunk chunk = GetVoxelChunkAtWorldLocation(new Vector3(x, y, z));

            if (chunk != null)
            {
                return chunk.GetFilledVoxelGridHeightAt((int)(x - chunk.Origin.X), (int)(y - chunk.Origin.Y), (int)(z - chunk.Origin.Z));
            }

            else return -1;
        }

        public void GetNonNullVoxelsAtWorldLocationCheckFirst(VoxelChunk checkFirst,  Vector3 worldLocation, List<Voxel> toReturn, int depth)
        {
            if (depth > 1)
            {
                return;
            }

            if (checkFirst != null )
            {
                if (checkFirst.IsWorldLocationValid(worldLocation))
                {
                    Voxel v = checkFirst.GetVoxelAtWorldLocation(worldLocation);

                    if (v != null)
                    {
                        toReturn.Add(v); 
                    }
                    else
                    {
                        toReturn.AddRange(GetNonNullVoxelsAtWorldLocation(worldLocation, depth + 1));
                    }
                }
                
                else
                {
                    toReturn.AddRange(GetNonNullVoxelsAtWorldLocation(worldLocation, depth + 1));
                }
                 
            }
            else
            {
                
                VoxelChunk v = GetVoxelChunkAtWorldLocation(worldLocation);
                
                if (v != null)
                {
                    if (v.IsWorldLocationValid(worldLocation))
                    {
                        Voxel got = v.GetVoxelAtWorldLocation(worldLocation);

                        if (got != null)
                        {
                            toReturn.Add(got);
                        }
                    }

                }
            }

            return;
        }

        public List<LiquidPrimitive> GetAllLiquidPrimitives()
        {
            List<LiquidPrimitive> toReturn = new List<LiquidPrimitive>();

            foreach (KeyValuePair<Point3, VoxelChunk> chunks in ChunkMap)
            {
                VoxelChunk chunk = chunks.Value;
                toReturn.AddRange(chunk.Liquids.Values);
            }

            return toReturn;
        }

        public bool DoesWaterCellExist(Vector3 worldLocation)
        {
            VoxelChunk chunkAtLocation = GetVoxelChunkAtWorldLocation(worldLocation);

            if (chunkAtLocation == null)
            {
                return false;
            }
            else
            {
                return chunkAtLocation.IsWorldLocationValid(worldLocation);
            }
        }

        public WaterCell GetWaterCellAtLocation(Vector3 worldLocation)
        {
            VoxelChunk chunkAtLocation = GetVoxelChunkAtWorldLocation(worldLocation);

            if (chunkAtLocation != null)
            {
                Vector3 gridPos = chunkAtLocation.WorldToGrid(worldLocation);
                return chunkAtLocation.Water[(int)gridPos.X][ (int)gridPos.Y][ (int)gridPos.Z];
            }

            return null;
        }

        public List<VoxelRef> GetVoxelsIntersecting(BoundingBox box)
        {
            HashSet<VoxelChunk> intersects = new HashSet<VoxelChunk>();
            ChunkOctree.Root.GetComponentsIntersecting<VoxelChunk>(box, intersects);

            List<VoxelRef> toReturn = new List<VoxelRef>();

            foreach (VoxelChunk chunk in intersects)
            {
                toReturn.AddRange(chunk.GetVoxelsIntersecting(box));
            }

            return toReturn;
        }

        public void UpdateDynamicLights()
        {
            Dictionary<Point3, VoxelChunk> needsUpdate = new Dictionary<Point3,VoxelChunk>();

            for (int i = 0; i < DynamicLights.Count; i++)
            {
                DynamicLight light = DynamicLights[i];
                VoxelChunk chunk = ChunkMap[light.Voxel.ChunkID];
                needsUpdate[light.Voxel.ChunkID] = chunk;

                foreach (VoxelChunk neighbor in chunk.Neighbors.Values)
                {
                    needsUpdate[neighbor.ID] = neighbor;
                }
            }

            foreach (VoxelChunk chunk in needsUpdate.Values)
            {
                chunk.ResetDynamicLight(0);
            }

            foreach (VoxelChunk chunk in needsUpdate.Values)
            {
                chunk.CalculateDynamicLights();
            }


        }

        public bool SaveAllChunks(string directory, bool compress)
        {
            foreach (KeyValuePair<Point3, VoxelChunk> pair in ChunkMap)
            {
                ChunkFile chunkFile = new ChunkFile(pair.Value);
                
                string fileName = directory + System.IO.Path.DirectorySeparatorChar + pair.Key.X + "_" + pair.Key.Y + "_" + pair.Key.Z;

                if(compress)
                {
                    fileName += ".zch";
                }
                else
                {
                    fileName += ".jch";
                }

                if (!chunkFile.WriteFile(fileName, compress))
                {
                    return false;
                }
            }

            return true;
        }
        
    }
}
