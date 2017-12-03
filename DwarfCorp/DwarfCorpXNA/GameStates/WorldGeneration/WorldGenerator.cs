using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Security.Policy;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json.Converters;

namespace DwarfCorp.GameStates
{
    public class WorldGenerator
    {
        public enum GenerationState
        {
            NotStarted,
            Generating,
            Finished
        }

        public GenerationState CurrentState { get; private set; }

        public WorldGenerationSettings Settings { get; set; }
        public VertexBuffer LandMesh { get; set; }
        public IndexBuffer LandIndex { get; set; }
        public Color[] worldData;
        public string LoadingMessage = "";
        private Thread genThread;
        public float Progress = 0.0f;

        public Action UpdatePreview;

        public int Seed
        {
            get { return MathFunctions.Seed; }
            set { MathFunctions.Seed = value; }
        }


        public List<Faction> NativeCivilizations = new List<Faction>();

        public WorldGenerator(WorldGenerationSettings Settings)
        {
            CurrentState = GenerationState.NotStarted;
            Seed = Settings.Seed;
            this.Settings = Settings;
            MathFunctions.Random = new ThreadSafeRandom(Seed);
            Overworld.Volcanoes = new List<Vector2>();
            LandMesh = null;
            LandIndex = null;
            NativeCivilizations = Settings.Natives;
        }

        public static int[] SetUpTerrainIndices(int width, int height)
        {
            int[] indices = new int[(width - 1) * (height - 1) * 6];
            int counter = 0;
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int lowerLeft = x + y * width;
                    int lowerRight = (x + 1) + y * width;
                    int topLeft = x + (y + 1) * width;
                    int topRight = (x + 1) + (y + 1) * width;

                    indices[counter++] = topLeft;
                    indices[counter++] = lowerRight;
                    indices[counter++] = lowerLeft;

                    indices[counter++] = topLeft;
                    indices[counter++] = topRight;
                    indices[counter++] = lowerRight;
                }
            }

            return indices;
        }

        public void CreateMesh(GraphicsDevice Device)
        {
           int resolution = 4;
           int width = Overworld.Map.GetLength(0);
           int height = Overworld.Map.GetLength(1);
           int numVerts = (width * height) / resolution;
           LandMesh = new VertexBuffer(Device, VertexPositionNormalTexture.VertexDeclaration, numVerts, BufferUsage.None);
           VertexPositionNormalTexture[] verts = new VertexPositionNormalTexture[numVerts];

            int i = 0;
            for (int x = 0; x < width; x += resolution)
            {
                for (int y = 0; y < height; y += resolution)
                {
                    float landHeight = Overworld.Map[x, y].Height;
                    verts[i].Position = new Vector3((float)x / width, landHeight * 0.05f, (float)y / height);
                    verts[i].TextureCoordinate = new Vector2(((float)x) / width, ((float)y) / height);
                    Vector3 normal = new Vector3(Overworld.Map[MathFunctions.Clamp(x + 1, 0, width - 1), y].Height - height,  1.0f, Overworld.Map[x, MathFunctions.Clamp(y + 1, 0, height - 1)].Height - height);
                    normal.Normalize();
                    verts[i].Normal = normal;
                    i++;
                }
            }
            LandMesh.SetData(verts);
            int[] indices = SetUpTerrainIndices(width / resolution, height / resolution);
            LandIndex = new IndexBuffer(Device, typeof(int), indices.Length, BufferUsage.None);
            LandIndex.SetData(indices);
        }

        public void WaitForFinish()
        {
            if (genThread != null && genThread.IsAlive)
            {
                genThread.Join();
            }
        }

        public void Abort()
        {
            if (genThread != null && genThread.IsAlive)
                genThread.Abort();
        }

        public void AutoSelectSpawnRegion()
        {
            LoadingMessage = "Selecting spawn.";
            Settings.WorldOrigin = Settings.WorldGenerationOrigin;
            bool inWater = true;
            do
            {
                Rectangle rect = GetSpawnRectangle();
                var center = rect.Center;
                inWater = Overworld.Map[center.X, center.Y].Height < Settings.SeaLevel;
                if (inWater)
                {
                    Settings.WorldGenerationOrigin = 
                        new Vector2(MathFunctions.Rand(rect.Width + 1, Settings.Width - rect.Width), MathFunctions.Rand(rect.Height + 1, Settings.Height));
                    Settings.WorldOrigin = Settings.WorldGenerationOrigin;
                }
            } while (inWater);
        }

        public List<Faction> GetFactionsInSpawn()
        {
            Rectangle spawnRect = GetSpawnRectangle();
            List<Faction> toReturn = new List<Faction>();
            for (int x = spawnRect.X; x < spawnRect.X + spawnRect.Width; x++)
            {
                for (int y = spawnRect.Y; y < spawnRect.Y + spawnRect.Height; y++)
                {
                    byte factionIdx = Overworld.Map[x, y].Faction;

                    if (factionIdx > 0)
                    {
                        Faction faction = NativeCivilizations[factionIdx - 1];

                        if (!toReturn.Contains(faction))
                        {
                            toReturn.Add(faction);
                        }
                        
                    }
                }
            }
            return toReturn;
        }
                
        public void Generate()
        {
            LandMesh = null;
            LandIndex = null;
            if (CurrentState == GenerationState.NotStarted)
            {
                Settings.WorldGenerationOrigin = new Vector2(Settings.Width / 2.0f, Settings.Height / 2.0f);
                genThread = new Thread(unused =>
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                    System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                    GenerateWorld(Seed, (int) Settings.Width, (int) Settings.Height);
                })
                {
                    Name = "GenerateWorld"
                };
                genThread.Start();
            }
        }
        
        public Dictionary<string, Color> GenerateFactionColors()
        {
            Dictionary<string, Color> toReturn = new Dictionary<string, Color>();
            toReturn["Unclaimed"] = Color.Gray;
            foreach (Faction faction in NativeCivilizations)
            {
                toReturn[faction.Name + " (" + faction.Race.Name + ")"] = faction.PrimaryColor;
            }
            return toReturn;
        }
        
        
        public void GenerateVolcanoes(int width, int height)
        {
            int volcanoSamples = 4;
            float volcanoSize = 11;
            for(int i = 0; i < (int) Settings.NumVolcanoes; i++)
            {
                Vector2 randomPos = new Vector2((float) (MathFunctions.Random.NextDouble() * width), (float) (MathFunctions.Random.NextDouble() * height));
                float maxFaults = Overworld.Map[(int) randomPos.X, (int) randomPos.Y].Height;
                for(int j = 0; j < volcanoSamples; j++)
                {
                    Vector2 randomPos2 = new Vector2((float) (MathFunctions.Random.NextDouble() * width), (float) (MathFunctions.Random.NextDouble() * height));
                    float faults = Overworld.Map[(int) randomPos2.X, (int) randomPos2.Y].Height;

                    if(faults > maxFaults)
                    {
                        randomPos = randomPos2;
                        maxFaults = faults;
                    }
                }

                Overworld.Volcanoes.Add(randomPos);


                for(int dx = -(int) volcanoSize; dx <= (int) volcanoSize; dx++)
                {
                    for(int dy = -(int) volcanoSize; dy <= (int) volcanoSize; dy++)
                    {
                        int x = (int) MathFunctions.Clamp(randomPos.X + dx, 0, width - 1);
                        int y = (int) MathFunctions.Clamp(randomPos.Y + dy, 0, height - 1);

                        float dist = (float) Math.Sqrt(dx * dx + dy * dy);
                        float fDist = (float) Math.Sqrt((dx / 3.0f) * (dx / 3.0f) + (dy / 3.0f) * (dy / 3.0f));

                        //Overworld.Map[x, y].Erosion = MathFunctions.Clamp(dist, 0.0f, 0.5f);
                        float f = (float) (Math.Pow(Math.Sin(fDist), 3.0f) + 1.0f) * 0.2f;
                        Overworld.Map[x, y].Height += f;

                        if(dist <= 2)
                        {
                            Overworld.Map[x, y].Water = Overworld.WaterType.Volcano;
                        }

                        if(dist < volcanoSize)
                            Overworld.Map[x, y].Biome = BiomeLibrary.GetBiome("Waste").Biome;
                    }
                }
            }
        }

        public void GenerateWorld(int seed, int width, int height)
        {
#if CREATE_CRASH_LOGS
           try
#endif
            {
                Seed = seed;
                MathFunctions.Random = new ThreadSafeRandom(Seed);
                Overworld.heightNoise.Seed = seed;
                CurrentState = GenerationState.Generating;

                if (Overworld.Name == null)
                {
                    Overworld.Name = WorldGenerationSettings.GetRandomWorldName();
                }

                LoadingMessage = "Init..";
                Overworld.heightNoise.Seed = Seed;
                worldData = new Color[width * height];
                Overworld.Map = new Overworld.MapData[width, height];

                Progress = 0.01f;

                LoadingMessage = "Height Map ...";
                float[,] heightMapLookup = null;
                heightMapLookup = Overworld.GenerateHeightMapLookup(width, height);
                Overworld.GenerateHeightMapFromLookup(heightMapLookup, width, height, 1.0f, false);

                Progress = 0.05f;

                int numRains = (int)Settings.NumRains;
                int rainLength = 250;
                int numRainSamples = 3;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Overworld.Map[x, y].Erosion = 1.0f;
                        Overworld.Map[x, y].Weathering = 0;
                        Overworld.Map[x, y].Faults = 1.0f;
                    }
                }

                LoadingMessage = "Climate";
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Overworld.Map[x, y].Temperature = ((float)(y) / (float)(height)) * Settings.TemperatureScale;
                        //Overworld.Map[x, y].Rainfall = Math.Max(Math.Min(Overworld.noise(x, y, 1000.0f, 0.01f) + Overworld.noise(x, y, 100.0f, 0.1f) * 0.05f, 1.0f), 0.0f) * RainfallScale;
                    }
                }

                //Overworld.Distort(width, height, 60.0f, 0.005f, Overworld.ScalarFieldType.Rainfall);
                Overworld.Distort(width, height, 30.0f, 0.005f, Overworld.ScalarFieldType.Temperature);
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Overworld.Map[x, y].Temperature = Math.Max(Math.Min(Overworld.Map[x, y].Temperature, 1.0f), 0.0f);
                    }
                }
        
                int numVoronoiPoints = (int)Settings.NumFaults;

                if (UpdatePreview != null) UpdatePreview();

                Progress = 0.1f;
                LoadingMessage = "Faults ...";

                #region voronoi

                Voronoi(width, height, numVoronoiPoints);

                #endregion

                Overworld.GenerateHeightMapFromLookup(heightMapLookup, width, height, 1.0f, true);

                Progress = 0.2f;

                Overworld.GenerateHeightMapFromLookup(heightMapLookup, width, height, 1.0f, true);

                Progress = 0.25f;
                if (UpdatePreview != null) UpdatePreview();
                LoadingMessage = "Erosion...";

                #region erosion

                float[,] buffer = new float[width, height];
                Erode(width, height, Settings.SeaLevel, Overworld.Map, numRains, rainLength, numRainSamples, buffer);
                Overworld.GenerateHeightMapFromLookup(heightMapLookup, width, height, 1.0f, true);

                #endregion

                Progress = 0.9f;


                LoadingMessage = "Blur.";
                Overworld.Blur(Overworld.Map, width, height, Overworld.ScalarFieldType.Erosion);

                LoadingMessage = "Generate height.";
                Overworld.GenerateHeightMapFromLookup(heightMapLookup, width, height, 1.0f, true);


                LoadingMessage = "Rain";
                CalculateRain(width, height);

                LoadingMessage = "Biome";
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        Overworld.Map[x, y].Biome = Overworld.GetBiome(Overworld.Map[x, y].Temperature, Overworld.Map[x, y].Rainfall, Overworld.Map[x, y].Height).Biome;

                LoadingMessage = "Volcanoes";

                GenerateVolcanoes(width, height);

                if (UpdatePreview != null) UpdatePreview();


                LoadingMessage = "Factions";
                FactionLibrary library = new FactionLibrary();
                library.Initialize(null, new CompanyInformation());

                if (Settings.Natives == null || Settings.Natives.Count == 0)
                {
                    NativeCivilizations = new List<Faction>();
                    for (int i = 0; i < Settings.NumCivilizations; i++)
                    {
                        NativeCivilizations.Add(library.GenerateFaction(null, i, Settings.NumCivilizations));
                    }
                }
                else
                {
                    NativeCivilizations = Settings.Natives;

                    if (Settings.NumCivilizations > Settings.Natives.Count)
                    {
                        int count = Settings.Natives.Count;
                        for (int i = count; i < Settings.NumCivilizations; i++)
                        {
                            NativeCivilizations.Add(library.GenerateFaction(null, i, Settings.NumCivilizations));
                        }
                    }
                }
                SeedCivs(Overworld.Map, Settings.NumCivilizations, NativeCivilizations);
                GrowCivs(Overworld.Map, 200, NativeCivilizations);


                for (int x = 0; x < width; x++)
                {
                    Overworld.Map[x, 0] = Overworld.Map[x, 1];
                    Overworld.Map[x, height - 1] = Overworld.Map[x, height - 2];
                }

                for (int y = 0; y < height; y++)
                {
                    Overworld.Map[0, y] = Overworld.Map[1, y];
                    Overworld.Map[width - 1, y] = Overworld.Map[width - 2, y];
                }

                LoadingMessage = "Selecting Spawn Point";
                AutoSelectSpawnRegion();

                CurrentState = GenerationState.Finished;
                LoadingMessage = "";
                Progress = 1.0f;
            }
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
                throw;
            }
#endif
        }

        public void CalculateRain(int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                float currentMoisture = Settings.RainfallScale * 10;
                for (int x = 0; x < width; x++)
                {
                    float h = Overworld.Map[x, y].Height;
                    bool isWater = h < Settings.SeaLevel;

                    if (isWater)
                    {
                        currentMoisture += MathFunctions.Rand(0.1f, 0.3f);
                        currentMoisture = Math.Min(currentMoisture, Settings.RainfallScale * 20);
                        Overworld.Map[x, y].Rainfall = 0.5f;
                    }
                    else
                    {
                        float rainAmount = currentMoisture * 0.017f * h + currentMoisture * 0.0006f;
                        currentMoisture -= rainAmount;
                        float evapAmount = MathFunctions.Rand(0.01f, 0.02f);
                        currentMoisture += evapAmount;
                        Overworld.Map[x, y].Rainfall = rainAmount * Settings.RainfallScale * Settings.Width * 0.015f;
                    }
                }
            }

            Overworld.Distort(width, height, 5.0f, 0.03f, Overworld.ScalarFieldType.Rainfall);

        }

        internal void LoadDummy(Color[] color, GraphicsDevice Device)
        {
            CurrentState = GenerationState.Finished;
            Progress = 1.0f;
            worldData = color;
            CreateMesh(Device);
        }

        private void Voronoi(int width, int height, int numVoronoiPoints)
        {
            List<List<Vector2>> vPoints = new List<List<Vector2>>();
            List<float> rands = new List<float>();

            /*
            List<Vector2> edge = new List<Vector2>
            {
                new Vector2(0, 0),
                new Vector2(width, 0),
                new Vector2(width, height),
                new Vector2(0, height),
                new Vector2(0, 0)
            };

            List<Vector2> randEdge = new List<Vector2>();
            for (int i = 1; i < edge.Count; i++)
            {
                if (MathFunctions.RandEvent(0.5f))
                {
                    randEdge.Add(edge[i]);
                    randEdge.Add(edge[i - 1]);
                }
            }

            vPoints.Add(randEdge);
             */
            for(int i = 0; i < numVoronoiPoints; i++)
            {
                Vector2 v = GetEdgePoint(width, height);

                for(int j = 0; j < 4; j++)
                {
                    List<Vector2> line = new List<Vector2>();
                    rands.Add(1.0f);

                    line.Add(v);
                    v += new Vector2(MathFunctions.Rand() - 0.5f, MathFunctions.Rand() - 0.5f) * Settings.Width * 0.5f;
                    line.Add(v);
                    vPoints.Add(line);
                }
            }


            List<VoronoiNode> nodes = new List<VoronoiNode>();
            foreach (List<Vector2> pts in vPoints)
            {
                for(int j = 0; j < pts.Count - 1; j++)
                {
                    VoronoiNode node = new VoronoiNode
                    {
                        pointA = pts[j], 
                        pointB = pts[j + 1]
                    };
                    nodes.Add(node);
                }
            }

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    Overworld.Map[x, y].Faults = GetVoronoiValue(nodes, x, y);
                }
            }

            ScaleMap(Overworld.Map, width, height, Overworld.ScalarFieldType.Faults);
            Overworld.Distort(width, height, 20, 0.01f, Overworld.ScalarFieldType.Faults);
        }

        private void Erode(int width, int height, float seaLevel, Overworld.MapData[,] heightMap, int numRains, int rainLength, int numRainSamples, float[,] buffer)
        {
            float remaining = 1.0f - Progress - 0.2f;
            float orig = Progress;
            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    buffer[x, y] = heightMap[x, y].Height;
                }
            }

            for(int i = 0; i < numRains; i++)
            {
                Progress = orig + remaining * ((float) i / (float) numRains);
                Vector2 currentPos = new Vector2(0, 0);
                Vector2 bestPos = currentPos;
                float bestHeight = 0.0f;
                for(int k = 0; k < numRainSamples; k++)
                {
                    int randX = MathFunctions.Random.Next(1, width - 1);
                    int randY = MathFunctions.Random.Next(1, height - 1);

                    currentPos = new Vector2(randX, randY);
                    float h = Overworld.GetHeight(buffer, currentPos);

                    if(h > bestHeight)
                    {
                        bestHeight = h;
                        bestPos = currentPos;
                    }
                }

                currentPos = bestPos;

                const float erosionRate = 0.9f;
                Vector2 velocity = Vector2.Zero;
                for(int j = 0; j < rainLength; j++)
                {
                    Vector2 g = Overworld.GetMinNeighbor(buffer, currentPos);

                    float h = Overworld.GetHeight(buffer, currentPos);

                    if(h < seaLevel|| g.LengthSquared() < 1e-12)
                    {
                        break;
                    }

                    Overworld.MinBlend(Overworld.Map, currentPos, erosionRate * Overworld.GetValue(Overworld.Map, currentPos, Overworld.ScalarFieldType.Erosion), Overworld.ScalarFieldType.Erosion);

                    velocity = 0.1f * g + 0.7f * velocity + 0.2f * MathFunctions.RandVector2Circle();
                    currentPos += velocity;
                }
            }
        }

        private void Weather(int width, int height, float T, Vector2[] neighbs, float[,] buffer)
        {
            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    buffer[x, y] = Overworld.Map[x, y].Height * Overworld.Map[x, y].Faults;
                }
            }

            int weatheringIters = 10;

            for(int iter = 0; iter < weatheringIters; iter++)
            {
                for(int x = 0; x < width; x++)
                {
                    for(int y = 0; y < height; y++)
                    {
                        Vector2 p = new Vector2(x, y);
                        Vector2 maxDiffNeigh = Vector2.Zero;
                        float maxDiff = 0;
                        float totalDiff = 0;
                        float h = Overworld.GetHeight(buffer, p);
                        float lowestNeighbor = 0.0f;
                        for(int i = 0; i < 4; i++)
                        {
                            float nh = Overworld.GetHeight(buffer, p + neighbs[i]);
                            float diff = h - nh;
                            totalDiff += diff;
                            if(diff > maxDiff)
                            {
                                maxDiffNeigh = neighbs[i];
                                maxDiff = diff;
                                lowestNeighbor = nh;
                            }
                        }

                        if(maxDiff > T)
                        {
                            Overworld.AddValue(Overworld.Map, p + maxDiffNeigh, Overworld.ScalarFieldType.Weathering, (float)(maxDiff * 0.4f));
                            Overworld.AddValue(Overworld.Map, p, Overworld.ScalarFieldType.Weathering, (float)(-maxDiff * 0.4f));
                        }
                    }
                }

                for(int x = 0; x < width; x++)
                {
                    for(int y = 0; y < height; y++)
                    {
                        Vector2 p = new Vector2(x, y);
                        float w = Overworld.GetValue(Overworld.Map, p, Overworld.ScalarFieldType.Weathering);
                        Overworld.AddHeight(buffer, p, w);
                        Overworld.Map[x, y].Weathering = 0.0f;
                    }
                }
            }

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    Overworld.Map[x, y].Weathering = buffer[x, y] - Overworld.Map[x, y].Height * Overworld.Map[x, y].Faults;
                }
            }
        }

        private static Vector2 GetEdgePoint(int width, int height)
        {
            return new Vector2(MathFunctions.Random.Next(0, width), MathFunctions.Random.Next(0, height));
        }

        private static void ScaleMap(Overworld.MapData[,] map, int width, int height, Overworld.ScalarFieldType fieldType)
        {
            float min = 99999;
            float max = -99999;
            float average = 0;

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    float v = map[x, y].GetValue(fieldType);
                    average += v;
                    if(v < min)
                    {
                        min = v;
                    }

                    if(v > max)
                    {
                        max = v;
                    }
                }
            }
            average /= (width*height);
            average = ((average - min)/(max - min));
            bool tooLow = average < 0.5f;
            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    float v = map[x, y].GetValue(fieldType);
                    float newVal = ((v - min)/(max - min)) + 0.001f;
                    if (tooLow)
                        newVal = 1.0f - newVal;
                    map[x, y].SetValue(fieldType, newVal);
                }
            }
        }

        private class VoronoiNode
        {
            public Vector2 pointA;
            public Vector2 pointB;
            public float dist;
        }

        private float GetVoronoiValue(List<VoronoiNode> points, int x, int y)
        {
            Vector2 xVec = new Vector2(x, y);

            float minDist = float.MaxValue;
            VoronoiNode maxNode = null;
            for(int i = 0; i < points.Count; i++)
            {
                VoronoiNode vor = points[i];
                vor.dist = MathFunctions.PointLineDistance2D(vor.pointA, vor.pointB, xVec);

                if(vor.dist < minDist)
                {
                    minDist = vor.dist;
                    maxNode = vor;
                }
            }

            if(maxNode == null)
            {
                return 1.0f;
            }

            return (float) (1e-2*(maxNode.dist / Settings.Width));
        }


        public Point? GetRandomLandPoint(Overworld.MapData[,] map)
        {
            const int maxIters = 1000;
            int i = 0;
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            while (i < maxIters)
            {
                int x = MathFunctions.Random.Next(0, width);
                int y = MathFunctions.Random.Next(0, height);

                if (map[x, y].Height > Settings.SeaLevel)
                {
                    return new Point(x, y);
                }

                i++;
            }

            return null;
        }

        public void SeedCivs(Overworld.MapData[,] map, int numCivs, List<Faction> civs )
        {
            for (int i = 0; i < numCivs; i++)
            {
                Point? randomPoint = GetRandomLandPoint(map);

                if (randomPoint == null) continue;
                else
                {
                    map[randomPoint.Value.X, randomPoint.Value.Y].Faction = (byte)(i + 1);
                }
            }
        }

        public void GrowCivs(Overworld.MapData[,] map, int iters, List<Faction> civs)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            byte[] neighbors = new byte[] {0, 0, 0, 0};
            float[] neighborheights = new float[] { 0, 0, 0, 0};
            Point[] deltas = new Point[] { new Point(1, 0), new Point(0, 1), new Point(-1, 0), new Point(1, -1) };
            for (int i = 0; i < iters; i++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        bool isUnclaimed = map[x, y].Faction == 0;
                        bool isWater = map[x, y].Height < Settings.SeaLevel;
                        if (!isUnclaimed && !isWater)
                        {
                            neighbors[0] = map[x + 1, y].Faction;
                            neighbors[1] = map[x, y + 1].Faction;
                            neighbors[2] = map[x - 1, y].Faction;
                            neighbors[3] = map[x, y - 1].Faction;
                            neighborheights[0] = map[x + 1, y].Height;
                            neighborheights[1] = map[x, y + 1].Height;
                            neighborheights[2] = map[x - 1, y].Height;
                            neighborheights[3] = map[x, y - 1].Height;

                            int minNeighbor = -1;
                            float minHeight = float.MaxValue;

                            for (int k = 0; k < 4; k++)
                            {
                                if (neighbors[k] == 0 && neighborheights[k] < minHeight && neighborheights[k] > Settings.SeaLevel)
                                {
                                    minHeight = neighborheights[k];
                                    minNeighbor = k;
                                }
                            }

                            if (minNeighbor >= 0 && MathFunctions.RandEvent(0.25f / (neighborheights[minNeighbor] + 1e-2f)))
                            {
                                var faction = map[x, y].Faction;
                                map[x + deltas[minNeighbor].X, y + deltas[minNeighbor].Y].Faction = faction;
                                var biome = map[x + deltas[minNeighbor].X, y + deltas[minNeighbor].Y].Biome;
                                var biomeName = BiomeLibrary.Biomes[biome].Name;
                                var myFaction = civs[faction - 1];
                                if (myFaction.Race.Biomes.ContainsKey(biomeName))
                                    map[x + deltas[minNeighbor].X, y + deltas[minNeighbor].Y].Biome =
                                        BiomeLibrary.GetBiome(myFaction.Race.Biomes[biomeName]).Biome;
                            }
                        }
                    }
                }
            }

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    byte f = map[x, y].Faction;
                    if (f> 0)
                    {
                        civs[f - 1].Center = new Point(x + civs[f - 1].Center.X, y + civs[f - 1].Center.Y);
                        civs[f - 1].TerritorySize++;
                    }
                }
            }

            foreach (Faction f in civs)
            {
                if(f.TerritorySize > 0)
                    f.Center = new Point(f.Center.X / f.TerritorySize, f.Center.Y / f.TerritorySize);
            }
        }

        // Spawn rectangle in world map pixel units
        public Rectangle GetSpawnRectangle()
        {
            int w = (int)(Settings.ColonySize.X * VoxelConstants.ChunkSizeX / Settings.WorldScale);
            int h = (int)(Settings.ColonySize.Z * VoxelConstants.ChunkSizeZ / Settings.WorldScale);
            return new Rectangle(
                (int)Settings.WorldGenerationOrigin.X - w / 2, (int)Settings.WorldGenerationOrigin.Y - h / 2, w, h);
        }

        // Get origin in world map pixel units
        public Vector2 GetOrigin(Point clickPoint, Vector3 worldSize)
        {
            return new Vector2(
                System.Math.Max(System.Math.Min(clickPoint.X, Settings.Width  - worldSize.X / 2), worldSize.X / 2),
                System.Math.Max(System.Math.Min(clickPoint.Y, Settings.Height - worldSize.Z / 2), worldSize.Z / 2)
            );
        }
    }
    
}
