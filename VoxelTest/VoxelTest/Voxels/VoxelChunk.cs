using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    /// <summary>
    /// A 3D grid of voxels, water, and light.
    /// </summary>
    public class VoxelChunk : IBoundedObject
    {
        public struct VertexColorInfo
        {
            public int SunColor;
            public int AmbientColor;
            public int DynamicColor;
        }

        public Dictionary<string, List<InstanceData>> Motes { get; set; }
        public VoxelListPrimitive Primitive { get; set; }
        public VoxelListPrimitive NewPrimitive = null;
        public Dictionary<LiquidType, LiquidPrimitive> Liquids { get; set; }
        public bool NewPrimitiveReceived = false;
        public bool NewLiquidReceived = false;
        public Voxel[][][] VoxelGrid { get; set; }
        public WaterCell[][][] Water { get; set; }
        public byte[][][] SunColors { get; set; }
        public byte[][][] DynamicColors { get; set; }
        public ConcurrentDictionary<VoxelRef, byte> Springs { get; set; }

        public int SizeX
        {
            get { return m_sizeX; }
        }

        public int SizeY
        {
            get { return m_sizeY; }
        }

        public int SizeZ
        {
            get { return m_sizeZ; }
        }

        public bool IsVisible { get; set; }
        public bool ShouldRebuild { get; set; }
        public bool IsRebuilding { get; set; }
        public Vector3 Origin { get; set; }
        private Vector3 HalfLength { get; set; }
        public bool RenderWireframe { get; set; }
        public ChunkManager Manager { get; set; }
        public bool IsActive { get; set; }
        public bool FirstWaterIter { get; set; }

        public Mutex PrimitiveMutex { get; set; }
        public bool ShouldRecalculateLighting { get; set; }
        public bool ShouldRebuildWater { get; set; }

        public static byte m_fogOfWar = 1;
        public ConcurrentDictionary<Point3, VoxelChunk> Neighbors { get; set; }
        public List<DynamicLight> DynamicLights { get; set; }


        private static bool m_staticsInitialized = false;
        private static Vector3[] m_vertexDeltas = new Vector3[8];
        private static Vector3[] m_faceDeltas = new Vector3[6];

        private static readonly Dictionary<VoxelVertex, List<Vector3>> m_vertexSuccessors = new Dictionary<VoxelVertex, List<Vector3>>();
        private static readonly Dictionary<VoxelVertex, List<Vector3>> m_vertexSuccessorsDiag = new Dictionary<VoxelVertex, List<Vector3>>();
        private static readonly Dictionary<BoxFace, VoxelVertex[]> m_faceVertices = new Dictionary<BoxFace, VoxelVertex[]>();
        private static readonly List<Vector3> m_manhattanSuccessors = new List<Vector3>();
        public static ColorGradient m_sunGradient = new ColorGradient(new Color(70, 70, 70), new Color(255, 254, 224), 255);
        public static ColorGradient m_ambientGradient = new ColorGradient(new Color(50, 50, 50), new Color(255, 255, 255), 255);
        public static ColorGradient m_caveGradient = new ColorGradient(new Color(8, 12, 17), new Color(41, 54, 76), 255);
        public static ColorGradient m_torchGradient = null;
        public bool LightingCalculated { get; set; }
        private bool firstRebuild = true;
        private int m_sizeX = -1;
        private int m_sizeY = -1;
        private int m_sizeZ = -1;
        private int m_tileSize = -1;


        public bool RebuildPending { get; set; }
        public bool RebuildLiquidPending { get; set; }
        public Point3 ID { get; set; }

        public bool ReconstructRamps { get; set; }

        public uint GetID()
        {
            return (uint) ID.GetHashCode();
        }

        #region statics

        private void InitializeStatics()
        {
            if(m_staticsInitialized)
            {
                return;
            }

            m_vertexDeltas[(int) VoxelVertex.BackBottomLeft] = new Vector3(0, 0, 0);
            m_vertexDeltas[(int) VoxelVertex.BackTopLeft] = new Vector3(0, 1.0f, 0);
            m_vertexDeltas[(int) VoxelVertex.BackBottomRight] = new Vector3(1.0f, 0, 0);
            m_vertexDeltas[(int) VoxelVertex.BackTopRight] = new Vector3(1.0f, 1.0f, 0);

            m_vertexDeltas[(int) VoxelVertex.FrontBottomLeft] = new Vector3(0, 0, 1.0f);
            m_vertexDeltas[(int) VoxelVertex.FrontTopLeft] = new Vector3(0, 1.0f, 1.0f);
            m_vertexDeltas[(int) VoxelVertex.FrontBottomRight] = new Vector3(1.0f, 0, 1.0f);
            m_vertexDeltas[(int) VoxelVertex.FrontTopRight] = new Vector3(1.0f, 1.0f, 1.0f);


            m_manhattanSuccessors.Add(new Vector3(1.0f, 0, 0));
            m_manhattanSuccessors.Add(new Vector3(-1.0f, 0, 0));
            m_manhattanSuccessors.Add(new Vector3(0, -1.0f, 0));
            m_manhattanSuccessors.Add(new Vector3(0, 1.0f, 0));
            m_manhattanSuccessors.Add(new Vector3(0, 0, -1.0f));
            m_manhattanSuccessors.Add(new Vector3(0, 0, 1.0f));

            m_faceDeltas[(int) BoxFace.Top] = new Vector3(0.5f, 0.0f, 0.5f);
            m_faceDeltas[(int) BoxFace.Bottom] = new Vector3(0.5f, 1.0f, 0.5f);
            m_faceDeltas[(int) BoxFace.Left] = new Vector3(1.0f, 0.5f, 0.5f);
            m_faceDeltas[(int) BoxFace.Right] = new Vector3(0.0f, 0.5f, 0.5f);
            m_faceDeltas[(int) BoxFace.Front] = new Vector3(0.5f, 0.5f, 0.0f);
            m_faceDeltas[(int) BoxFace.Back] = new Vector3(0.5f, 0.5f, 1.0f);


            m_faceVertices[BoxFace.Top] = new[]
            {
                VoxelVertex.BackTopLeft,
                VoxelVertex.BackTopRight,
                VoxelVertex.FrontTopLeft,
                VoxelVertex.FrontTopRight
            };
            m_faceVertices[BoxFace.Bottom] = new[]
            {
                VoxelVertex.BackBottomLeft,
                VoxelVertex.BackBottomRight,
                VoxelVertex.FrontBottomLeft,
                VoxelVertex.FrontBottomRight
            };
            m_faceVertices[BoxFace.Left] = new[]
            {
                VoxelVertex.BackTopLeft,
                VoxelVertex.BackBottomLeft,
                VoxelVertex.FrontTopLeft,
                VoxelVertex.FrontBottomLeft
            };
            m_faceVertices[BoxFace.Right] = new[]
            {
                VoxelVertex.BackTopRight,
                VoxelVertex.BackBottomRight,
                VoxelVertex.FrontTopRight,
                VoxelVertex.FrontBottomRight
            };
            m_faceVertices[BoxFace.Front] = new[]
            {
                VoxelVertex.FrontBottomLeft,
                VoxelVertex.FrontBottomRight,
                VoxelVertex.FrontTopLeft,
                VoxelVertex.FrontTopRight
            };
            m_faceVertices[BoxFace.Back] = new[]
            {
                VoxelVertex.BackTopLeft,
                VoxelVertex.BackTopRight,
                VoxelVertex.BackBottomLeft,
                VoxelVertex.BackBottomRight
            };


            ColorStop first = new ColorStop
            {
                m_color = Color.Black,
                m_position = 0.0f
            };
            ColorStop second = new ColorStop
            {
                m_color = new Color(100, 50, 0),
                m_position = 0.2f
            };
            ColorStop third = new ColorStop
            {
                m_color = new Color(240, 200, 50),
                m_position = 0.5f
            };
            ColorStop fourth = new ColorStop
            {
                m_color = new Color(255, 240, 180),
                m_position = 1.0f
            };

            List<ColorStop> torchStops = new List<ColorStop>
            {
                first,
                second,
                third,
                fourth
            };

            m_torchGradient = new ColorGradient(torchStops);

            for(int i = 0; i < 8; i++)
            {
                VoxelVertex vertex = (VoxelVertex) (i);
                List<Vector3> successors = new List<Vector3>();
                List<Vector3> diagSuccessors = new List<Vector3>();
                int xlim = 0;
                int ylim = 0;
                int zlim = 0;
                int nXLim = 0;
                int nYLim = 0;
                int nZLim = 0;

                switch(vertex)
                {
                    case VoxelVertex.BackBottomLeft:
                        nXLim = -1;
                        xlim = 1;

                        nYLim = -1;
                        ylim = 1;

                        nZLim = -1;
                        zlim = 1;
                        break;
                    case VoxelVertex.BackBottomRight:
                        nXLim = 0;
                        xlim = 2;

                        nYLim = -1;
                        ylim = 1;

                        nZLim = -1;
                        zlim = 1;
                        break;
                    case VoxelVertex.BackTopLeft:
                        nXLim = -1;
                        xlim = 1;
                        nYLim = 0;
                        ylim = 2;
                        nZLim = -1;
                        zlim = 1;
                        break;
                    case VoxelVertex.BackTopRight:
                        nXLim = 0;
                        xlim = 2;
                        nYLim = 0;
                        ylim = 2;
                        nZLim = -1;
                        zlim = 1;
                        break;

                    case VoxelVertex.FrontBottomLeft:
                        nXLim = -1;
                        xlim = 1;
                        nYLim = -1;
                        ylim = 1;
                        nZLim = 0;
                        zlim = 2;
                        break;
                    case VoxelVertex.FrontBottomRight:
                        nXLim = 0;
                        xlim = 2;
                        nYLim = -1;
                        ylim = 1;
                        nZLim = 0;
                        zlim = 2;
                        break;
                    case VoxelVertex.FrontTopLeft:
                        nXLim = -1;
                        xlim = 1;
                        nYLim = 0;
                        ylim = 2;
                        nZLim = 0;
                        zlim = 2;
                        break;
                    case VoxelVertex.FrontTopRight:
                        nXLim = 0;
                        xlim = 2;
                        nYLim = 0;
                        ylim = 2;
                        nZLim = 0;
                        zlim = 2;
                        break;
                }

                for(int dx = nXLim; dx < xlim; dx++)
                {
                    for(int dy = nYLim; dy < ylim; dy++)
                    {
                        for(int dz = nZLim; dz < zlim; dz++)
                        {
                            if(dx == 0 && dy == 0 && dz == 0)
                            {
                                continue;
                            }
                            else
                            {
                                successors.Add(new Vector3(dx, dy, dz));

                                if((dx != 0 && dz != 0 && dy == 0))
                                {
                                    diagSuccessors.Add(new Vector3(dx, dy, dz));
                                }
                            }
                        }
                    }
                }

                m_vertexSuccessors[vertex] = successors;
                m_vertexSuccessorsDiag[vertex] = diagSuccessors;
            }

            m_staticsInitialized = true;
        }

        #endregion

        public VoxelChunk(ChunkManager manager, Vector3 origin, int tileSize, Point3 id, int sizeX, int sizeY, int sizeZ)
        {
            FirstWaterIter = true;
            m_sizeX = sizeX;
            m_sizeY = sizeY;
            m_sizeZ = sizeZ;
            ID = id;
            Origin = origin;
            VoxelGrid = ChunkGenerator.Allocate(m_sizeX, m_sizeY, m_sizeZ);
            Water = WaterAllocate(m_sizeX, m_sizeY, m_sizeZ);
            IsVisible = true;
            ShouldRebuild = true;
            m_tileSize = tileSize;
            HalfLength = new Vector3((float) m_tileSize / 2.0f, (float) m_tileSize / 2.0f, (float) m_tileSize / 2.0f);
            Primitive = new VoxelListPrimitive();
            RenderWireframe = false;
            Manager = manager;
            IsActive = true;

            InitializeStatics();
            PrimitiveMutex = new Mutex();
            ShouldRecalculateLighting = true;
            Neighbors = new ConcurrentDictionary<Point3, VoxelChunk>();
            DynamicLights = new List<DynamicLight>();
            Liquids = new Dictionary<LiquidType, LiquidPrimitive>();
            Liquids[LiquidType.Water] = new LiquidPrimitive(LiquidType.Water);
            Liquids[LiquidType.Lava] = new LiquidPrimitive(LiquidType.Lava);
            ShouldRebuildWater = true;
            Springs = new ConcurrentDictionary<VoxelRef, byte>();
            InitializeWater();
            IsRebuilding = false;
            LightingCalculated = false;
            RebuildPending = false;
            RebuildLiquidPending = false;
            ReconstructRamps = true;
            SunColors = ChunkGenerator.Allocate<byte>(m_sizeX, m_sizeY, m_sizeZ);
            DynamicColors = ChunkGenerator.Allocate<byte>(m_sizeX, m_sizeY, m_sizeZ);
        }


        public static WaterCell[][][] WaterAllocate(int sx, int sy, int sz)
        {
            WaterCell[][][] w = new WaterCell[sx][][];

            for(int x = 0; x < sx; x++)
            {
                w[x] = new WaterCell[sy][];

                for(int y = 0; y < sy; y++)
                {
                    w[x][y] = new WaterCell[sz];

                    for(int z = 0; z < sx; z++)
                    {
                        w[x][y][z] = new WaterCell();
                    }
                }
            }

            return w;
        }


        public VoxelChunk(Vector3 origin, ChunkManager manager, Voxel[][][] voxelGrid, Point3 id, int tileSize)
        {
            FirstWaterIter = true;
            Motes = new Dictionary<string, List<InstanceData>>();
            VoxelGrid = voxelGrid;
            m_sizeX = VoxelGrid.Length;
            m_sizeY = VoxelGrid[0].Length;
            m_sizeZ = VoxelGrid[0][0].Length;
            ID = id;
            IsVisible = true;
            m_tileSize = tileSize;
            HalfLength = new Vector3((float) m_tileSize / 2.0f, (float) m_tileSize / 2.0f, (float) m_tileSize / 2.0f);
            Origin = origin;
            Primitive = new VoxelListPrimitive();
            RenderWireframe = false;
            Manager = manager;
            IsActive = true;

            Neighbors = new ConcurrentDictionary<Point3, VoxelChunk>();
            DynamicLights = new List<DynamicLight>();
            Liquids = new Dictionary<LiquidType, LiquidPrimitive>();
            Liquids[LiquidType.Water] = new LiquidPrimitive(LiquidType.Water);
            Liquids[LiquidType.Lava] = new LiquidPrimitive(LiquidType.Lava);

            for(int x = 0; x < voxelGrid.Length; x++)
            {
                for(int y = 0; y < voxelGrid[x].Length; y++)
                {
                    for(int z = 0; z < voxelGrid[x][y].Length; z++)
                    {
                        Voxel v = voxelGrid[x][y][z];
                        if(v != null)
                        {
                            v.Chunk = this;
                            v.GridPosition = v.Position - Origin;
                        }
                    }
                }
            }

            Water = WaterAllocate(m_sizeX, m_sizeY, m_sizeZ);
            SunColors = ChunkGenerator.Allocate<byte>(m_sizeX, m_sizeY, m_sizeZ);
            DynamicColors = ChunkGenerator.Allocate<byte>(m_sizeX, m_sizeY, m_sizeZ);
            InitializeStatics();
            PrimitiveMutex = new Mutex();
            ShouldRecalculateLighting = true;
            ShouldRebuildWater = true;
            Springs = new ConcurrentDictionary<VoxelRef, byte>();
            IsRebuilding = false;
            InitializeWater();
            LightingCalculated = false;
        }

        public void InitializeWater()
        {
            for(int x = 0; x < m_sizeX; x++)
            {
                for(int y = 0; y < m_sizeY; y++)
                {
                    for(int z = 0; z < m_sizeZ; z++)
                    {
                        Water[x][y][z] = new WaterCell();
                    }
                }
            }
        }

        public static VoxelVertex GetNearestDelta(Vector3 position)
        {
            float bestDist = 10000000;
            VoxelVertex bestKey = VoxelVertex.BackTopRight;
            for(int i = 0; i < 8; i++)
            {
                float dist = (position - m_vertexDeltas[i]).LengthSquared();
                if(dist < bestDist)
                {
                    bestDist = dist;
                    bestKey = (VoxelVertex) (i);
                }
            }


            return bestKey;
        }

        public static BoxFace GetNearestFace(Vector3 position)
        {
            float bestDist = 10000000;
            BoxFace bestKey = BoxFace.Top;
            for(int i = 0; i < 6; i++)
            {
                float dist = (position - m_faceDeltas[i]).LengthSquared();
                if(dist < bestDist)
                {
                    bestDist = dist;
                    bestKey = (BoxFace) (i);
                }
            }


            return bestKey;
        }

        private BoundingBox m_boundingBox;
        private bool m_boundingBoxCreated = false;

        public BoundingBox GetBoundingBox()
        {
            if(!m_boundingBoxCreated)
            {
                Vector3 max = new Vector3(m_sizeX, m_sizeY, m_sizeZ) + Origin;
                m_boundingBox = new BoundingBox(Origin, max);
                m_boundingBoxCreated = true;
            }

            return m_boundingBox;
        }

        private BoundingSphere m_boundingSphere;
        private bool m_boundingSphereCreated = false;

        public BoundingSphere GetBoundingSphere()
        {
            if(!m_boundingSphereCreated)
            {
                float m = Math.Max(Math.Max(m_sizeX, m_sizeY), m_sizeZ) * 0.5f;
                m_boundingSphere = new BoundingSphere(Origin + new Vector3(m_sizeX, m_sizeY, m_sizeZ) * 0.5f, (float) Math.Sqrt(3 * m * m));
                m_boundingSphereCreated = true;
            }

            return m_boundingSphere;
        }

        public void Update(GameTime t)
        {
            PrimitiveMutex.WaitOne();
            if(NewPrimitiveReceived)
            {
                Primitive = NewPrimitive;
                NewPrimitive = null;
                NewPrimitiveReceived = false;
            }
            PrimitiveMutex.ReleaseMutex();
        }

        public void Render(Texture2D tilemap, Texture2D illumMap, Texture2D sunMap, Texture2D ambientMap, Texture2D torchMap, GraphicsDevice device, Effect effect, Matrix worldMatrix)
        {
            effect.Parameters["xEnableLighting"].SetValue(GameSettings.Default.CursorLightEnabled);

            effect.Parameters["xLightColor"].SetValue(new Vector4(0, 0, 1, 0));
            effect.Parameters["xLightPos"].SetValue(PlayState.CursorLightPos);


            if(GameSettings.Default.SelfIlluminationEnabled)
            {
                effect.Parameters["SelfIllumination"].SetValue(true);
            }

            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                if(!RenderWireframe)
                {
                    Primitive.Render(device);
                }
                else
                {
                    Primitive.RenderWireframe(device);
                }
            }

            effect.Parameters["SelfIllumination"].SetValue(false);

            //Color color = Color.White;


            //Drawer2D.DrawText("" + ID.X + "," + ID.Y + "," + ID.Z + ":" + ID.GetHashCode(), Origin, Color.White, Color.Black);
        }

        public void RebuildLiquids(GraphicsDevice g)
        {
            foreach(KeyValuePair<LiquidType, LiquidPrimitive> primitive in Liquids)
            {
                primitive.Value.InitializeFromChunk(this, g);
            }
            ShouldRebuildWater = false;
        }

        private byte getMax(byte[] values)
        {
            byte max = 0;

            foreach(byte b in values)
            {
                if(b > max)
                {
                    max = b;
                }
            }

            return max;
        }

        public static Perlin MoteNoise = new Perlin(0);
        public static Perlin MoteScaleNoise = new Perlin(250);

        private float Clamp(float v, float a)
        {
            if(v > a)
            {
                return a;
            }

            if(v < -a)
            {
                return -a;
            }

            return v;
        }

        private Vector3 ClampVector(Vector3 v, float a)
        {
            v.X = Clamp(v.X, a);
            v.Y = Clamp(v.Y, a);
            v.Z = Clamp(v.Z, a);
            return v;
        }

        public void BuildGrassMotes(Overworld.Biome biome)
        {
            BiomeData biomeData = BiomeLibrary.Biomes[biome];

            string GrassType = biomeData.GrassVoxel;

            for(int i = 0; i < biomeData.Motes.Count; i++)
            {
                List<Vector3> GrassPositions = new List<Vector3>();
                List<Color> GrassColors = new List<Color>();
                List<float> GrassScales = new List<float>();
                DetailMoteData moteData = biomeData.Motes[i];

                for(int x = 0; x < SizeX; x++)
                {
                    for(int y = 1; y < Math.Min(Manager.ChunkData.MaxViewingLevel + 1, SizeY - 1); y++)
                    {
                        for(int z = 0; z < SizeZ; z++)
                        {
                            Voxel v = VoxelGrid[x][y][z];


                            if(v != null && VoxelGrid[x][y + 1][z] == null && v.Type.Name == GrassType && v.IsVisible && Water[x][y + 1][z].WaterLevel == 0)
                            {
                                float vOffset = 0.0f;

                                if(v.RampType != RampType.None)
                                {
                                    vOffset = -0.5f;
                                }

                                float value = MoteNoise.Noise(v.Position.X * moteData.RegionScale, v.Position.Y * moteData.RegionScale, v.Position.Z * moteData.RegionScale);
                                float s = MoteScaleNoise.Noise(v.Position.X * moteData.RegionScale, v.Position.Y * moteData.RegionScale, v.Position.Z * moteData.RegionScale) * moteData.MoteScale;

                                if(Math.Abs(value) > moteData.SpawnThreshold)
                                {
                                    Vector3 smallNoise = ClampVector(VertexNoise.GetRandomNoiseVector(v.Position * moteData.RegionScale * 20.0f) * 20.0f, 0.4f);
                                    smallNoise.Y = 0.0f;
                                    GrassPositions.Add(v.Position + new Vector3(0.5f, 1.0f + s * 0.5f + vOffset, 0.5f) + smallNoise);
                                    GrassColors.Add(new Color((int) SunColors[x][y][z], 128, (int) DynamicColors[x][y][z]));
                                    GrassScales.Add(s);
                                }
                            }
                        }
                    }
                }

                if(Motes.Count < i + 1)
                {
                    Motes[moteData.Name] = new List<InstanceData>();
                }

                Motes[moteData.Name] = EntityFactory.GenerateGrassMotes(GrassPositions,
                    GrassColors, GrassScales, Manager.Components, Manager.Content, Manager.Graphics, Motes[moteData.Name], moteData.Asset, moteData.Name);
            }
        }

        public void UpdateRamps()
        {
            if(ReconstructRamps || firstRebuild)
            {
                VoxelListPrimitive.UpdateRamps(this);
                VoxelListPrimitive.UpdateCornerRamps(this);
                ReconstructRamps = false;
            }
        }

        public void BuildGrassMotes()
        {
            Vector2 v = new Vector2(Origin.X, Origin.Z) / PlayState.WorldScale;

            Overworld.Biome biome = Overworld.Map[(int) v.X, (int) v.Y].Biome;
            /*Overworld.GetBiome(Overworld.LinearInterpolate(v, Overworld.Map, Overworld.ScalarFieldType.Temperature),
                                   Overworld.LinearInterpolate(v, Overworld.Map, Overworld.ScalarFieldType.Rainfall),
                                   Overworld.LinearInterpolate(v, Overworld.Map, Overworld.ScalarFieldType.Height));*/
            BuildGrassMotes(biome);
        }

        public void Rebuild(GraphicsDevice g)
        {
            //Drawer3D.DrawBox(GetBoundingBox(), Color.White, 0.1f);

            if(g == null || g.IsDisposed)
            {
                return;
            }
            IsRebuilding = true;

            BuildPrimitive(g);
            BuildGrassMotes();
            if(firstRebuild)
            {
                RebuildLiquids(g);
                firstRebuild = false;
            }

            IsRebuilding = false;

            if(ShouldRecalculateLighting)
            {
                NotifyChangedComponents();
            }
            ShouldRebuild = false;
        }

        public void BuildPrimitive(GraphicsDevice g)
        {
            //Primitive.InitializeFromChunk(this, g);
            VoxelListPrimitive primitive = new VoxelListPrimitive();
            primitive.InitializeFromChunk(this, g);
        }

        public void NotifyChangedComponents()
        {
            HashSet<IBoundedObject> componentsInside = new HashSet<IBoundedObject>();
            Manager.Components.CollisionManager.GetObjectsIntersecting(GetBoundingBox(), componentsInside, CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

            Message changedMessage = new Message(Message.MessageType.OnChunkModified, "Chunk Modified");

            foreach(IBoundedObject c in componentsInside)
            {
                if(c is GameComponent)
                {
                    ((GameComponent) c).ReceiveMessageRecursive(changedMessage);
                }
            }
        }

        public void Destroy(GraphicsDevice device)
        {
            if(Primitive != null)
            {
                Primitive.ResetBuffer(device);
            }

            if(Motes != null)
            {
                foreach(KeyValuePair<string, List<InstanceData>> mote in Motes)
                {
                    foreach(InstanceData mote2 in mote.Value)
                    {
                        EntityFactory.instanceManager.RemoveInstance(mote.Key, mote2);
                    }
                }
            }
        }

        #region transformations

        public Vector3 WorldToGrid(Vector3 worldLocation)
        {
            Vector3 grid = (worldLocation - Origin);
            return grid;
        }


        public Voxel GetVoxelAtWorldLocation(Vector3 worldLocation)
        {
            Vector3 grid = WorldToGrid(worldLocation);

            bool valid = IsCellValid((int) grid.X, (int) grid.Y, (int) grid.Z);

            if(valid)
            {
                return VoxelGrid[(int) grid.X][(int) grid.Y][(int) grid.Z];
            }
            else
            {
                return null;
            }
        }

        public bool IsGridPositionValid(Vector3 grid)
        {
            return IsCellValid((int) grid.X, (int) grid.Y, (int) grid.Z);
        }

        public bool IsWorldLocationValid(Vector3 worldLocation)
        {
            Vector3 grid = WorldToGrid(worldLocation);

            return IsCellValid((int) grid.X, (int) grid.Y, (int) grid.Z);
        }

        public bool IsCellValid(int x, int y, int z)
        {
            return x >= 0 && y >= 0 && z >= 0 && x < SizeX && y < SizeY && z < SizeZ;
        }

        #endregion transformations

        #region lighting

        public DynamicLight AddLight(Vector3 worldLocation, byte range, byte intensity)
        {
            if(IsWorldLocationValid(worldLocation))
            {
                ShouldRecalculateLighting = true;
                ShouldRebuild = true;
                VoxelRef voxels = Manager.ChunkData.GetVoxelReferenceAtWorldLocation(this, worldLocation);
                DynamicLight light = new DynamicLight(range, intensity, voxels, Manager);
                DynamicLights.Add(light);
                Manager.DynamicLights.Add(light);
                foreach(VoxelChunk chunk in Neighbors.Values)
                {
                    if(chunk != this)
                    {
                        chunk.ShouldRebuild = true;
                        chunk.ShouldRecalculateLighting = true;
                    }
                }

                return light;
            }

            return null;
        }

        public void SetAllToRecalculate()
        {
            for(int x = 0; x < SizeX; x++)
            {
                for(int y = 0; y < SizeY; y++)
                {
                    for(int z = 0; z < SizeZ; z++)
                    {
                        Voxel v = VoxelGrid[x][y][z];
                        if(v != null)
                        {
                            v.RecalculateLighting = true;
                        }
                    }
                }
            }
        }

        public void CalculateDynamicLights()
        {
            for(int i = 0; i < DynamicLights.Count; i++)
            {
                DynamicLight light = DynamicLights[i];
                List<VoxelRef> visitedNodes = new List<VoxelRef>();
                CalculateDynamicLight(light, 0, light.Intensity, light.Voxel, visitedNodes);
            }
        }

        public byte GetIntensity(DynamicLight light, byte lightIntensity, VoxelRef voxel)
        {
            Vector3 vertexPos = voxel.WorldPosition;
            Vector3 diff = vertexPos - (light.Voxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f));
            float dist = diff.LengthSquared() * 2;

            return (byte) (int) ((Math.Min(1.0f / (dist + 0.0001f), 1.0f)) * (float) light.Intensity);
        }

        public void CalculateDynamicLight(DynamicLight light, byte depth, byte intensity, VoxelRef seed, List<VoxelRef> visitedNodes)
        {
            Queue<VoxelRef> q = new Queue<VoxelRef>();
            Queue<int> depths = new Queue<int>();
            Queue<Vector3> deltas = new Queue<Vector3>();
            HashSet<VoxelRef> visitedSet = new HashSet<VoxelRef>();

            q.Enqueue(light.Voxel);
            visitedSet.Add(light.Voxel);
            depths.Enqueue(0);
            deltas.Enqueue(new Vector3(0.01f, 0, 0));
            int iters = 0;

            while(q.Count > 0)
            {
                VoxelRef t = q.Dequeue();
                int d = depths.Dequeue();
                Vector3 delta = deltas.Dequeue();
                iters++;

                if(d > light.Range)
                {
                    continue;
                }

                Voxel seedVoxel = t.GetVoxel(false);
                VoxelChunk seedChunk = null;
                if(seedVoxel != null)
                {
                    seedChunk = seedVoxel.Chunk;
                }
                else
                {
                    seedChunk = t.ChunkID.Equals(ID) ? this : Manager.ChunkData.ChunkMap[t.ChunkID];
                }

                if(seedVoxel != null && iters > 1)
                {
                    Vector3 grid = seedVoxel.GridPosition;
                    seedVoxel.Chunk.DynamicColors[(int) grid.X][(int) grid.Y][(int) grid.Z] = (byte) (int) Math.Min((float) seedVoxel.Chunk.DynamicColors[(int) grid.X][(int) grid.Y][(int) grid.Z] + GetIntensity(light, intensity, t), 255.0f);
                }

                if(d != 0 && t.TypeName != "empty" && t.TypeName != "water")
                {
                    continue;
                }

                Vector3 relativeGrid = t.WorldPosition - seedChunk.Origin;
                List<VoxelRef> neighbors = seedChunk.GetNeighborsManhattan((int) relativeGrid.X, (int) relativeGrid.Y, (int) relativeGrid.Z);


                foreach(VoxelRef n in neighbors.Where(n => !visitedSet.Contains(n)))
                {
                    q.Enqueue(n);
                    depths.Enqueue(d + 1);
                    visitedSet.Add(n);
                    deltas.Enqueue(n.WorldPosition - t.WorldPosition + HalfLength);
                }
            }
        }

        public static void CalculateVertexLight(Voxel vox, VoxelVertex face,
            ChunkManager chunks, List<VoxelRef> neighbors, ref VertexColorInfo color)
        {
            float numHit = 1;
            float numChecked = 1;


            neighbors.Clear();

            color.SunColor += vox.Chunk.SunColors[(int) vox.GridPosition.X][(int) vox.GridPosition.Y][(int) vox.GridPosition.Z];
            color.DynamicColor += vox.Chunk.DynamicColors[(int) vox.GridPosition.X][(int) vox.GridPosition.Y][(int) vox.GridPosition.Z];
            vox.Chunk.GetNeighborsVertex(face, vox.GetReference(), neighbors, true);

            foreach(VoxelRef v in neighbors)
            {
                if(!chunks.ChunkData.ChunkMap.ContainsKey(v.ChunkID))
                {
                    continue;
                }

                VoxelChunk c = chunks.ChunkData.ChunkMap[v.ChunkID];
                color.SunColor += c.SunColors[(int) v.GridPosition.X][(int) v.GridPosition.Y][(int) v.GridPosition.Z];
                if(VoxelLibrary.IsSolid(v))
                {
                    numHit++;
                    numChecked++;
                    color.DynamicColor += c.DynamicColors[(int) v.GridPosition.X][(int) v.GridPosition.Y][(int) v.GridPosition.Z];
                }
                else
                {
                    numChecked++;
                }
            }


            float proportionHit = numHit / numChecked;
            color.AmbientColor = (int) Math.Min((1.0f - proportionHit) * 255.0f, 255);
            color.SunColor = (int) Math.Min((float) color.SunColor / (float) numChecked, 255);
            color.DynamicColor = (int) Math.Min((float) color.DynamicColor / (float) numHit, 255);
        }

        public void ResetDynamicLight(byte sunColor)
        {
            for(int x = 0; x < SizeX; x++)
            {
                for(int z = 0; z < SizeZ; z++)
                {
                    for(int y = 0; y < SizeY; y++)
                    {
                        if(VoxelGrid[x][y][z] != null)
                        {
                            DynamicColors[x][y][z] = sunColor;
                        }
                    }
                }
            }
        }

        public void ResetSunlightIgnoreEdges(byte sunColor)
        {
            for(int x = 1; x < SizeX - 1; x++)
            {
                for(int z = 1; z < SizeZ - 1; z++)
                {
                    for(int y = 0; y < SizeY; y++)
                    {
                        if(VoxelGrid[x][y][z] != null)
                        {
                            SunColors[x][y][z] = sunColor;
                        }
                    }
                }
            }
        }

        public void ResetSunlight(byte sunColor)
        {
            for(int x = 0; x < SizeX; x++)
            {
                for(int z = 0; z < SizeZ; z++)
                {
                    for(int y = 0; y < SizeY; y++)
                    {
                        if(VoxelGrid[x][y][z] != null)
                        {
                            SunColors[x][y][z] = sunColor;
                        }
                    }
                }
            }
        }

        public float GetTotalWaterHeight(VoxelRef voxRef)
        {
            float tot = 0;
            int x = (int) voxRef.GridPosition.X;
            int z = (int) voxRef.GridPosition.Z;
            for(int y = (int) voxRef.GridPosition.Y; y < SizeY; y++)
            {
                tot += Water[x][y][z].WaterLevel;

                if(Water[x][y][z].WaterLevel == 0)
                {
                    return tot;
                }
            }

            return tot;
        }

        public float GetTotalWaterHeightCells(VoxelRef voxRef)
        {
            float tot = 0;
            int x = (int) voxRef.GridPosition.X;
            int z = (int) voxRef.GridPosition.Z;
            for(int y = (int) voxRef.GridPosition.Y; y < SizeY; y++)
            {
                tot += (Water[x][y][z].WaterLevel) / 255.0f;

                if(Water[x][y][z].WaterLevel == 0 && y > (int) voxRef.GridPosition.Y)
                {
                    return tot;
                }
            }

            return tot;
        }

        public void UpdateSunlight(byte sunColor)
        {
            LightingCalculated = false;
            
            if(!Manager.ChunkData.ChunkMap.ContainsKey(ID))
            {
                return;
            }

            VoxelRef r = new VoxelRef();
            ResetSunlight(0);

            for(int x = 0; x < SizeX; x++)
            {
                for(int z = 0; z < SizeZ; z++)
                {
                    bool rayHit = false;
                    bool recalculateFound = false;
                    for(int y = SizeY - 1; y >= 0; y--)
                    {
                        if(rayHit)
                        {
                            break;
                        }

                        if(VoxelGrid[x][y][z] == null)
                        {
                            recalculateFound = true;
                            r.ChunkID = ID;
                            r.GridPosition = new Vector3(x, y, z);
                            r.WorldPosition = new Vector3(x, y, z) + Origin;
                            r.TypeName = "empty";

                            if(!HasNoNeighbors(r))
                            {
                                SunColors[x][y][z] = sunColor;
                            }
                            continue;
                        }

                        VoxelRef reference = VoxelGrid[x][y][z].GetReference();
                        recalculateFound = recalculateFound || VoxelGrid[x][y][z].RecalculateLighting;
                        if(!recalculateFound)
                        {
                            continue;
                        }

                        if(y >= SizeY - 1)
                        {
                            continue;
                        }

                        SunColors[x][y][z] = sunColor;
                        rayHit = true;
                    }
                }
            }
        }

        public void GetSharedVertices(VoxelRef v, VoxelVertex vertex, List<KeyValuePair<Voxel, List<VoxelVertex>>> vertices)
        {
            vertices.Clear();

            List<Voxel> neighbors = new List<Voxel>();
            GetNeighborsVertex(vertex, v, neighbors, false);

            Vector3 myDelta = m_vertexDeltas[(int) vertex];
            foreach(Voxel neighbor in neighbors)
            {
                if(neighbor == null)
                {
                    continue;
                }

                List<VoxelVertex> vertsNeighbor = new List<VoxelVertex>();
                Vector3 otherDelta = v.WorldPosition - neighbor.Position + myDelta;
                vertsNeighbor.Add(GetNearestDelta(otherDelta));


                vertices.Add(new KeyValuePair<Voxel, List<VoxelVertex>>(neighbor, vertsNeighbor));
            }
        }

        public void CalculateVertexLighting()
        {
            List<VoxelRef> neighbors = new List<VoxelRef>();
            VertexColorInfo colorInfo = new VertexColorInfo();
            bool ambientOcclusion = GameSettings.Default.AmbientOcclusion;
            for(int x = 0; x < SizeX; x++)
            {
                for(int y = 0; y < Math.Min(Manager.ChunkData.MaxViewingLevel + 1, SizeY); y++)
                {
                    for(int z = 0; z < SizeZ; z++)
                    {
                        Voxel voxel = VoxelGrid[x][y][z];
                        if(voxel == null)
                        {
                            continue;
                        }

                        if(VoxelLibrary.IsSolid(voxel) && (voxel.IsVisible || voxel.RecalculateLighting))
                        {
                            VoxelRef voxelRef = voxel.GetReference();
                            if(IsCompletelySurrounded(voxelRef, true))
                            {
                                SunColors[x][y][z] = 0;
                                for(int i = 0; i < 8; i++)
                                {
                                    voxel.VertexColors[i].G = m_fogOfWar;
                                }
                                voxel.RecalculateLighting = false;
                                continue;
                            }

                            if(ambientOcclusion)
                            {
                                for(int i = 0; i < 8; i++)
                                {
                                    CalculateVertexLight(voxel, (VoxelVertex) (i), Manager, neighbors, ref colorInfo);
                                    voxel.VertexColors[i] = new Color(colorInfo.SunColor, colorInfo.AmbientColor, colorInfo.DynamicColor);
                                }
                                voxel.RecalculateLighting = false;
                            }
                            else
                            {
                                byte sunColor = SunColors[(int) voxel.GridPosition.X][(int) voxel.GridPosition.Y][(int) voxel.GridPosition.Z];
                                byte dynColor = DynamicColors[(int) voxel.GridPosition.X][(int) voxel.GridPosition.Y][(int) voxel.GridPosition.Z];
                                for(int i = 0; i < 8; i++)
                                {
                                    voxel.VertexColors[i] = new Color(sunColor, 128, dynColor);
                                }
                                voxel.RecalculateLighting = false;
                            }
                        }
                        else if(voxel.IsVisible && voxel.RecalculateLighting)
                        {
                            SunColors[x][y][z] = 0;
                            voxel.RecalculateLighting = false;
                            for(int i = 0; i < 8; i++)
                            {
                                voxel.VertexColors[i] = new Color(0, m_fogOfWar, 0);
                            }
                        }
                    }
                }
            }

            LightingCalculated = true;
        }

        public void CalculateGlobalLight()
        {
            if(GameSettings.Default.CalculateSunlight)
            {
                UpdateSunlight(255);
            }
            else
            {
                ResetSunlight(255);
            }
        }

        #endregion

        #region visibility

        public int GetFilledVoxelGridHeightAt(int x, int y, int z)
        {
            int invalid = -1;

            if(!IsCellValid(x, y, z))
            {
                return invalid;
            }
            else
            {
                for(int h = y; h > 0; h--)
                {
                    if(VoxelLibrary.IsSolid(VoxelGrid[x][h][z]))
                    {
                        return h + 1;
                    }
                }
            }

            return invalid;
        }

        public int GetFilledHeightOrWaterAt(int x, int y, int z)
        {
            int invalid = -1;

            if(!IsCellValid(x, y, z))
            {
                return invalid;
            }
            else
            {
                for(int h = y; h > 0; h--)
                {
                    if(VoxelLibrary.IsSolid(VoxelGrid[x][h][z]) || Water[x][h][z].WaterLevel > 1)
                    {
                        return h + 1;
                    }
                }
            }

            return invalid;
        }

        public bool NeedsViewingLevelChange()
        {
            float level = Manager.ChunkData.MaxViewingLevel;

            int mx = SizeX;
            int my = SizeY;
            int mz = SizeZ;

            for(int x = 0; x < mx; x++)
            {
                for(int y = 0; y < my; y++)
                {
                    for(int z = 0; z < mz; z++)
                    {
                        float test = 0.0f;

                        switch(Manager.ChunkData.Slice)
                        {
                            case ChunkManager.SliceMode.X:
                                test = x + Origin.X;
                                break;
                            case ChunkManager.SliceMode.Y:
                                test = y + Origin.Y;
                                break;
                            case ChunkManager.SliceMode.Z:
                                test = z + Origin.Z;
                                break;
                        }

                        if(test > level && VoxelGrid[x][y][z] != null && VoxelGrid[x][y][z].IsVisible)
                        {
                            return true;
                        }
                        else if(VoxelGrid[x][y][z] != null && !VoxelGrid[x][y][z].IsVisible)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void UpdateMaxViewingLevel()
        {
            float level = Manager.ChunkData.MaxViewingLevel;

            int mx = SizeX;
            int my = SizeY;
            int mz = SizeZ;

            for(int x = 0; x < mx; x++)
            {
                for(int y = 0; y < my; y++)
                {
                    for(int z = 0; z < mz; z++)
                    {
                        float test = 0.0f;

                        switch(Manager.ChunkData.Slice)
                        {
                            case ChunkManager.SliceMode.X:
                                test = x + Origin.X;
                                break;
                            case ChunkManager.SliceMode.Y:
                                test = y + Origin.Y;
                                break;
                            case ChunkManager.SliceMode.Z:
                                test = z + Origin.Z;
                                break;
                        }

                        if(test > level && VoxelGrid[x][y][z] != null)
                        {
                            VoxelGrid[x][y][z].IsVisible = false;
                        }
                        else if(VoxelGrid[x][y][z] != null)
                        {
                            VoxelGrid[x][y][z].IsVisible = true;
                        }
                    }
                }
            }

            ShouldRebuildWater = true;
            ShouldRebuild = true;
        }


        public void MakeAllVoxelsVisible()
        {
            for(int x = 0; x < SizeX; x++)
            {
                for(int y = 0; y < SizeY; y++)
                {
                    for(int z = 0; z < SizeZ; z++)
                    {
                        Voxel v = VoxelGrid[x][y][z];
                        if(v.Type != VoxelLibrary.emptyType)
                        {
                            continue;
                        }
                        else
                        {
                            v.IsVisible = true;
                        }
                    }
                }
            }
        }

        #endregion

        #region neighbors

        //-------------------------
        public void GetNeighborsVertex(VoxelVertex vertex, VoxelRef v, List<VoxelRef> toReturn, bool empties)
        {
            Vector3 grid = v.GridPosition;
            GetNeighborsVertex(vertex, (int) grid.X, (int) grid.Y, (int) grid.Z, toReturn, empties);
        }

        public void GetNeighborsVertex(VoxelVertex vertex, VoxelRef v, List<Voxel> toReturn, bool empties)
        {
            Vector3 grid = v.GridPosition;
            GetNeighborsVertex(vertex, (int) grid.X, (int) grid.Y, (int) grid.Z, toReturn, empties);
        }


        private bool IsInterior(int x, int y, int z)
        {
            return (x != 0 && y != 0 && z != 0 && x != SizeX - 1 && y != SizeY - 1 && z != SizeZ - 1);
        }


        public void GetNeighborsSuccessors(List<Vector3> succ, int x, int y, int z, List<Voxel> toReturn, bool considerEmpties)
        {
            toReturn.Clear();

            Vector3 successor = Vector3.Zero;
            bool isInterior = IsInterior(x, y, z);
            int count = succ.Count;
            for(int i = 0; i < count; i++)
            {
                successor = succ[i];
                int nx = (int) successor.X + x;
                int ny = (int) successor.Y + y;
                int nz = (int) successor.Z + z;

                if(isInterior || IsCellValid(nx, ny, nz))
                {
                    Voxel v = VoxelGrid[nx][ny][nz];
                    if(v != null)
                    {
                        toReturn.Add(v);
                    }
                    else if(considerEmpties)
                    {
                        toReturn.Add(null);
                    }
                }
                else
                {
                    Point3 chunkID = ID;
                    if(nx >= SizeZ)
                    {
                        chunkID.X += 1;
                        nx = 0;
                    }
                    else if(nx < 0)
                    {
                        chunkID.X -= 1;
                        nx = SizeX - 1;
                    }

                    if(ny >= SizeY)
                    {
                        chunkID.Y += 1;
                        ny = 0;
                    }
                    else if(ny < 0)
                    {
                        chunkID.Y -= 1;
                        ny = SizeY - 1;
                    }

                    if(nz >= SizeZ)
                    {
                        chunkID.Z += 1;
                        nz = 0;
                    }
                    else if(nz < 0)
                    {
                        chunkID.Z -= 1;
                        nz = SizeZ - 1;
                    }


                    if(!Manager.ChunkData.ChunkMap.ContainsKey(chunkID))
                    {
                        continue;
                    }

                    VoxelChunk chunk = Manager.ChunkData.ChunkMap[chunkID];
                    Voxel n = chunk.VoxelGrid[nx][ny][nz];

                    if(n != null)
                    {
                        toReturn.Add(n);
                    }
                    else if(considerEmpties)
                    {
                        toReturn.Add(null);
                    }
                }
            }
        }

        public void GetNeighborsSuccessors(List<Vector3> succ, int x, int y, int z, List<VoxelRef> toReturn, bool considerEmpties)
        {
            toReturn.Clear();

            bool isInterior = IsInterior(x, y, z);
            int count = succ.Count;
            for(int i = 0; i < count; i++)
            {
                Vector3 successor = succ[i];
                int nx = (int) successor.X + x;
                int ny = (int) successor.Y + y;
                int nz = (int) successor.Z + z;

                if(isInterior || IsCellValid(nx, ny, nz))
                {
                    Voxel v = VoxelGrid[nx][ny][nz];
                    if(v != null)
                    {
                        toReturn.Add(v.GetReference());
                    }
                    else if(considerEmpties)
                    {
                        VoxelRef newRef = new VoxelRef
                        {
                            TypeName = "empty",
                            ChunkID = ID,
                            GridPosition = new Vector3(nx, ny, nz),
                            IsValid = true
                        };
                        newRef.WorldPosition = newRef.GridPosition + Origin;
                        toReturn.Add(newRef);
                    }
                }
                else
                {
                    Point3 chunkID = ID;
                    if(nx >= SizeZ)
                    {
                        chunkID.X += 1;
                        nx = 0;
                    }
                    else if(nx < 0)
                    {
                        chunkID.X -= 1;
                        nx = SizeX - 1;
                    }

                    if(ny >= SizeY)
                    {
                        chunkID.Y += 1;
                        ny = 0;
                    }
                    else if(ny < 0)
                    {
                        chunkID.Y -= 1;
                        ny = SizeY - 1;
                    }

                    if(nz >= SizeZ)
                    {
                        chunkID.Z += 1;
                        nz = 0;
                    }
                    else if(nz < 0)
                    {
                        chunkID.Z -= 1;
                        nz = SizeZ - 1;
                    }


                    if(!Manager.ChunkData.ChunkMap.ContainsKey(chunkID))
                    {
                        continue;
                    }

                    VoxelChunk chunk = Manager.ChunkData.ChunkMap[chunkID];
                    Voxel n = chunk.VoxelGrid[nx][ny][nz];

                    if(n != null)
                    {
                        toReturn.Add(n.GetReference());
                    }
                    else if(considerEmpties)
                    {
                        VoxelRef newRef = new VoxelRef
                        {
                            TypeName = "empty",
                            ChunkID = chunk.ID,
                            GridPosition = new Vector3(nx, ny, nz),
                            IsValid = true
                        };
                        newRef.WorldPosition = newRef.GridPosition + chunk.Origin;

                        toReturn.Add(newRef);
                    }
                }
            }
        }

        public void GetNeighborsVertex(VoxelVertex vertex, int x, int y, int z, List<VoxelRef> toReturn, bool considerEmpties)
        {
            GetNeighborsSuccessors(m_vertexSuccessors[vertex], x, y, z, toReturn, considerEmpties);
        }

        public void GetNeighborsVertex(VoxelVertex vertex, int x, int y, int z, List<Voxel> toReturn, bool considerEmpties)
        {
            GetNeighborsSuccessors(m_vertexSuccessors[vertex], x, y, z, toReturn, considerEmpties);
        }

        public void GetNeighborsVertexDiag(VoxelVertex vertex, int x, int y, int z, List<VoxelRef> toReturn, bool considerEmpties)
        {
            GetNeighborsSuccessors(m_vertexSuccessorsDiag[vertex], x, y, z, toReturn, considerEmpties);
        }

        public List<VoxelRef> GetNeighborsEuclidean(Voxel v)
        {
            Vector3 gridCoord = v.GridPosition;
            return GetNeighborsEuclidean((int) gridCoord.X, (int) gridCoord.Y, (int) gridCoord.Z);
        }

        public List<VoxelRef> GetNeighborsEuclidean(int x, int y, int z)
        {
            List<VoxelRef> toReturn = new List<VoxelRef>();
            bool isInterior = (x > 0 && y > 0 && z > 0 && x < SizeX - 1 && y < SizeY - 1 && z < SizeZ - 1);
            for(int dx = -1; dx < 2; dx++)
            {
                for(int dy = -1; dy < 2; dy++)
                {
                    for(int dz = -1; dz < 2; dz++)
                    {
                        if(dx == 0 && dy == 0 && dz == 0)
                        {
                            continue;
                        }

                        int nx = (int) dx + x;
                        int ny = (int) dy + y;
                        int nz = (int) dz + z;

                        if(isInterior || IsCellValid(nx, ny, nz))
                        {
                            if(VoxelGrid[nx][ny][nz] != null)
                            {
                                toReturn.Add(VoxelGrid[nx][ny][nz].GetReference());
                            }
                            else
                            {
                                VoxelRef newRef = new VoxelRef
                                {
                                    TypeName = "empty",
                                    ChunkID = this.ID,
                                    GridPosition = new Vector3(nx, ny, nz)
                                };

                                newRef.WorldPosition = newRef.GridPosition + this.Origin;
                                newRef.IsValid = true;

                                toReturn.Add(newRef);
                            }
                        }
                        else
                        {
                            VoxelRef otherVox =  Manager.ChunkData.GetVoxelReferenceAtWorldLocation(this, new Vector3(nx, ny, nz) + Origin);

                            if(otherVox != null)
                                toReturn.Add(otherVox);
                        }
                    }
                }
            }
            return toReturn;
        }

        public List<VoxelRef> GetNeighborsManhattan(int x, int y, int z)
        {
            List<VoxelRef> toReturn = new List<VoxelRef>();
            GetNeighborsSuccessors(m_manhattanSuccessors, x, y, z, toReturn, true);
            return toReturn;
        }

        public void NotifyTotalRebuild(bool neighbors)
        {
            ShouldRebuild = true;
            SetAllToRecalculate();
            ShouldRecalculateLighting = true;
            ShouldRebuildWater = true;
            ReconstructRamps = true;

            if(neighbors)
            {
                foreach(VoxelChunk chunk in Neighbors.Values)
                {
                    chunk.ShouldRebuild = true;
                    chunk.ShouldRecalculateLighting = true;
                    chunk.SetAllToRecalculate();
                    chunk.ShouldRebuildWater = true;
                }
            }
        }

        public List<VoxelRef> GetMovableNeighbors(int x, int y, int z)
        {
            List<VoxelRef> toReturn = new List<VoxelRef>();
            VoxelRef[,,] neighborHood = new VoxelRef[3, 3, 3];
            for(int dx = -1; dx < 2; dx++)
            {
                for(int dy = -1; dy < 2; dy++)
                {
                    for(int dz = -1; dz < 2; dz++)
                    {
                        int nx = dx + x;
                        int ny = dy + y;
                        int nz = dz + z;



                        VoxelRef otherVox = Manager.ChunkData.GetVoxelReferenceAtWorldLocation(this, new Vector3(nx, ny, nz) + Origin);
                        neighborHood[dx + 1, dy + 1, dz + 1] = otherVox;
                    }
                }
            }

            bool inWater = (neighborHood[1, 1, 1].GetWaterLevel(this.Manager) > 5);
            bool standingOnGround = (neighborHood[1, 0, 1].TypeName != "empty");
            bool topCovered = (neighborHood[1, 2, 1] == null || neighborHood[1, 2, 1].TypeName != "empty");
            bool hasNeighbors = false;

            for(int dx = 0; dx < 3; dx++)
            {
                for(int dz = 0; dz < 3; dz++)
                {
                    if(dx == 1 && dz == 1)
                    {
                        continue;
                    }

                    hasNeighbors = hasNeighbors || (neighborHood[dx, 1, dz] != null && neighborHood[dx, 1, dz].TypeName != "empty");
                }
            }

            List<Vector3> successors = new List<Vector3>();

            if(standingOnGround || inWater)
            {
                successors.Add(new Vector3(0, 1, 1));
                successors.Add(new Vector3(2, 1, 1));
                successors.Add(new Vector3(1, 1, 0));
                successors.Add(new Vector3(1, 1, 2));

                if(!hasNeighbors)
                {
                    successors.Add(new Vector3(2, 1, 2));
                    successors.Add(new Vector3(2, 1, 0));
                    successors.Add(new Vector3(0, 1, 2));
                    successors.Add(new Vector3(0, 1, 0));
                }

                if(!topCovered)
                {
                    successors.Add(new Vector3(0, 2, 1));
                    successors.Add(new Vector3(1, 2, 0));
                    successors.Add(new Vector3(2, 2, 1));
                    successors.Add(new Vector3(1, 2, 2));
                }
            }

            successors.Add(new Vector3(1, 0, 1));


            foreach(Vector3 v in successors)
            {
                VoxelRef n = neighborHood[(int) v.X, (int) v.Y, (int) v.Z];
                if(n != null && (n.TypeName == "empty" || n.GetWaterLevel(Manager) > 0))
                {
                    toReturn.Add(neighborHood[(int) v.X, (int) v.Y, (int) v.Z]);
                }
            }


            return toReturn;
        }

        public List<VoxelRef> GetMovableNeighbors(VoxelRef v)
        {
            Vector3 gridCoord = v.GridPosition;

            return GetMovableNeighbors((int) gridCoord.X, (int) gridCoord.Y, (int) gridCoord.Z);
        }


        public List<VoxelRef> GetReverseMovableNeighbors(VoxelRef v)
        {
            List<VoxelRef> toReturn = new List<VoxelRef>();
            Vector3 gridCoord = v.GridPosition;

            for(int dx = -1; dx < 2; dx++)
            {
                for(int dy = -1; dy < 2; dy++)
                {
                    for(int dz = -1; dz < 2; dz++)
                    {
                        if(dx == 0 && dy == 0 && dz == 0)
                        {
                            continue;
                        }

                        if(!GetMovableNeighbors((int) gridCoord.X + dx, (int) gridCoord.Y + dy, (int) gridCoord.Z + dz).Contains(v))
                        {
                            continue;
                        }

                        VoxelRef neighbor = Manager.ChunkData.GetVoxelReferenceAtWorldLocation(v.WorldPosition + new Vector3(dx, dy, dz));

                        if(neighbor != null)
                        {
                            toReturn.Add(neighbor);
                        }
                    }
                }
            }

            return toReturn;
        }


        public bool HasNoNeighbors(VoxelRef v)
        {
            Vector3 pos = v.WorldPosition;
            Vector3 gridPos = v.GridPosition;

            if(!Manager.ChunkData.ChunkMap.ContainsKey(v.ChunkID))
            {
                return false;
            }

            VoxelChunk chunk = Manager.ChunkData.ChunkMap[v.ChunkID];
            Point3 gridPoint = new Point3(gridPos);

            bool interior = Voxel.IsInteriorPoint(gridPoint, chunk);


            if(interior)
            {
                for(int i = 0; i < 6; i++)
                {
                    Vector3 neighbor = m_manhattanSuccessors[i];
                    Voxel n = VoxelGrid[gridPoint.X + (int) neighbor.X][gridPoint.Y + (int) neighbor.Y][gridPoint.Z + (int) neighbor.Z];

                    if(n != null)
                    {
                        return false;
                    }
                }
            }
            else
            {

                for(int i = 0; i < 6; i++)
                {
                    Vector3 neighbor = m_manhattanSuccessors[i];

                    if(!IsGridPositionValid(neighbor + gridPos))
                    {
                        Voxel atPos = Manager.ChunkData.GetNonNullVoxelAtWorldLocation( pos + neighbor);
                        if(atPos != null)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        Voxel n = VoxelGrid[(int) gridPos.X + (int) neighbor.X][(int) gridPos.Y + (int) neighbor.Y][(int) gridPos.Z + (int) neighbor.Z];

                        if(n != null)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }


        public bool IsCompletelySurrounded(VoxelRef v, bool ramps)
        {
            if(!Manager.ChunkData.ChunkMap.ContainsKey(v.ChunkID))
            {
                return false;
            }

            Vector3 pos = v.WorldPosition;
            VoxelChunk chunk = Manager.ChunkData.ChunkMap[v.ChunkID];
            Point3 gridPoint = new Point3(v.GridPosition);
            bool interior = Voxel.IsInteriorPoint(gridPoint, chunk);


            if(interior)
            {
                for(int i = 0; i < 6; i++)
                {
                    Vector3 neighbor = m_manhattanSuccessors[i];
                    Voxel n = chunk.VoxelGrid[gridPoint.X + (int) neighbor.X][gridPoint.Y + (int) neighbor.Y][gridPoint.Z + (int) neighbor.Z];

                    if(n == null || (ramps && n.RampType != RampType.None))
                    {
                        return false;
                    }
                }
            }
            else
            {

                for(int i = 0; i < 6; i++)
                {
                    Vector3 neighbor = m_manhattanSuccessors[i];
                    Voxel atPos = Manager.ChunkData.GetNonNullVoxelAtWorldLocationCheckFirst(chunk, pos + neighbor);

                    if(atPos == null|| (ramps && atPos.RampType != RampType.None))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool IsCompletelySurrounded(VoxelRef v)
        {
            return IsCompletelySurrounded(v, false);
        }

        public List<VoxelRef> GetNeighborsManhattan(VoxelRef v)
        {
            Vector3 gridCoord = v.GridPosition;

            return GetNeighborsManhattan((int) gridCoord.X, (int) gridCoord.Y, (int) gridCoord.Z);
        }


        public void ResetWaterBuffer()
        {
            for(int x = 0; x < SizeX; x++)
            {
                for(int y = 0; y < SizeY; y++)
                {
                    for(int z = 0; z < SizeZ; z++)
                    {
                        Water[x][y][z].HasChanged = false;
                        Water[x][y][z].IsFalling = false;
                        Water[x][y][z].FluidFlow = Vector3.Zero;
                    }
                }
            }
        }

        public Vector3 GridToWorld(Vector3 gridCoord)
        {
            return gridCoord + Origin;
        }

        //-------------------------

        public List<VoxelRef> GetVoxelsIntersecting(BoundingBox box)
        {
            if(!GetBoundingBox().Intersects(box) && GetBoundingBox().Contains(box) != ContainmentType.Disjoint)
            {
                return new List<VoxelRef>();
            }
            else
            {
                BoundingBox myBox = GetBoundingBox();
                List<VoxelRef> toReturn = new List<VoxelRef>();
                for(float x = Math.Max(box.Min.X, myBox.Min.X); x < Math.Min(box.Max.X, myBox.Max.X); x++)
                {
                    for(float y = Math.Max(box.Min.Y, myBox.Min.Y); y < Math.Min(box.Max.Y, myBox.Max.Y); y++)
                    {
                        for(float z = Math.Max(box.Min.Z, myBox.Min.Z); z < Math.Min(box.Max.Z, myBox.Max.Z); z++)
                        {
                            Vector3 grid = new Vector3(x, y, z) - Origin;
                            Voxel vox = VoxelGrid[(int) grid.X][(int) grid.Y][(int) grid.Z];
                            if(null != vox)
                            {
                                toReturn.Add(vox.GetReference());
                            }
                            else
                            {
                                VoxelRef empt = new VoxelRef();
                                empt.ChunkID = ID;
                                empt.TypeName = "empty";
                                empt.WorldPosition = new Vector3(x, y, z);
                                empt.GridPosition = grid;
                                toReturn.Add(empt);
                            }
                        }
                    }
                }

                return toReturn;
            }
        }

        #endregion neighbors
    }

}