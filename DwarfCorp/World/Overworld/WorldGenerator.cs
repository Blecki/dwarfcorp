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
using System.Text;
using System.Linq;

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

        public OverworldGenerationSettings Settings { get; set; }
        public VertexBuffer LandMesh { get; set; }
        public IndexBuffer LandIndex { get; set; }
        public string LoadingMessage = "";
        private Thread genThread;
        public float Progress = 0.0f;
        public Overworld Overworld;
        public Action UpdatePreview;

        public WorldGenerator(OverworldGenerationSettings Settings, bool ClearOverworld)
        {
            CurrentState = GenerationState.NotStarted;
            this.Settings = Settings;

            MathFunctions.Random = new ThreadSafeRandom(Settings.Seed);

            if (ClearOverworld)
            {
                Overworld = new Overworld(Settings.Width, Settings.Height);
                Settings.Overworld = Overworld;
            }
            else
                Overworld = Settings.Overworld;

            Overworld.Volcanoes = new List<Vector2>();
            LandMesh = null;
            LandIndex = null;
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
            if (Overworld.Map == null)
            {
                return;
            }
           int resolution = 1;
           int width = Overworld.Map.GetLength(0);
           int height = Overworld.Map.GetLength(1);
           int numVerts = ((width + 1) * (height + 1)) / resolution;
           LandMesh = new VertexBuffer(Device, VertexPositionNormalTexture.VertexDeclaration, numVerts, BufferUsage.None);
           VertexPositionNormalTexture[] verts = new VertexPositionNormalTexture[numVerts];

            int i = 0;
            for (int x = 0; x <= width; x += resolution)
            {
                for (int y = 0; y <= height; y += resolution)
                {
                    float landHeight = Overworld.Map[(x < width) ? x : x - 1, (y < height) ? y : y - 1].Height;
                    verts[i].Position = new Vector3((float)x / width, landHeight * 0.05f, (float)y / height);
                    verts[i].TextureCoordinate = new Vector2(((float)x) / width, ((float)y) / height);
                    Vector3 normal = new Vector3(Overworld.Map[MathFunctions.Clamp(x + 1, 0, width - 1), MathFunctions.Clamp(y, 0, height - 1)].Height - height,  1.0f, Overworld.Map[MathFunctions.Clamp(x, 0, width - 1), MathFunctions.Clamp(y + 1, 0, height - 1)].Height - height);
                    normal.Normalize();
                    verts[i].Normal = normal;
                    i++;
                }
            }
            LandMesh.SetData(verts);
            int[] indices = SetUpTerrainIndices((width + 1) / resolution, (height + 1) / resolution);
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

        public IEnumerable<KeyValuePair<string, Color>> GetSpawnStats()
        {
            var factions = GetFactionsInSpawn();
            var biomes = new HashSet<byte>();
            Rectangle spawnRect = GetSpawnRectangle();
            for (int x = Math.Max(spawnRect.X, 0); x < Math.Min(spawnRect.X + spawnRect.Width, Overworld.Map.GetLength(0) - 1); x++)
            {
                for (int y = Math.Max(spawnRect.Y, 0); y < Math.Min(spawnRect.Y + spawnRect.Height, Overworld.Map.GetLength(1) - 1); y++)
                {
                    biomes.Add(Overworld.Map[x, y].Biome);
                }
            }


            if (factions.Count == 0)
            {
                yield return new KeyValuePair<string, Color>("Unclaimed land.", Color.White);
            }
            else
            {
                yield return new KeyValuePair<string, Color>("Claimed by:", Color.White);
            }
            
            foreach (var faction in factions)
            {
                int goodwill = (int)(faction.GoodWill * 100);
                string dsc = "Neutral";
                Color color = Color.White;
                if (goodwill < -80)
                {
                    color = Color.Red;
                    dsc = "Enemies";
                }
                else if (goodwill > 80)
                {
                    color = Color.Green;
                    dsc = "Friendly";
                }
                yield return new KeyValuePair<string, Color>("    " + faction.Name + " (" + faction.Race + ") --" + dsc, color);
            }
            yield return new KeyValuePair<string, Color>("Biomes: ", Color.White);
            foreach (var biome in biomes)
            {
                var _biome = BiomeLibrary.GetBiome(biome);
                if (_biome == null)
                    continue;

                var biomeColor = _biome.MapColor;
                biomeColor.R = (byte)Math.Min(255, biomeColor.R + 80);
                biomeColor.G = (byte)Math.Min(255, biomeColor.G + 80);
                biomeColor.B = (byte)Math.Min(255, biomeColor.B + 80);
                yield return new KeyValuePair<string, Color>("    " + _biome.Name, biomeColor);
            }
        }

        public List<OverworldFaction> GetFactionsInSpawn()
        {
            Rectangle spawnRect = GetSpawnRectangle();
            var toReturn = new List<OverworldFaction>();

            try
            {
                for (int x = spawnRect.X; x < spawnRect.X + spawnRect.Width; x++)
                {
                    for (int y = spawnRect.Y; y < spawnRect.Y + spawnRect.Height; y++)
                    {
                        byte factionIdx = Overworld.Map[x, y].Faction;

                        if (factionIdx > 0 && factionIdx <= Settings.Natives.Count)
                        {
                            var faction = Settings.Natives[factionIdx - 1];

                            if (!toReturn.Contains(faction))
                                toReturn.Add(faction);

                        }
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // Not sure how this is possible - it almost has to be that the spawnrect is somehow outside the overworld. 
                // So, we'll just give up. Worst case is the land defaults to 'unclaimed'.
            }

            return toReturn;
        }
                
        public void Generate()
        {
            LandMesh = null;
            LandIndex = null;
            if (CurrentState == GenerationState.NotStarted)
            {
                genThread = new Thread(unused =>
                {
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                    GenerateWorld();
                })
                {
                    Name = "GenerateWorld",
                    IsBackground = true
                };
                genThread.Start();
            }
        }
        
        public Dictionary<string, Color> GenerateFactionColors()
        {
            var toReturn = new Dictionary<string, Color>();
            toReturn["Unclaimed"] = Color.Gray;
            foreach (var faction in Settings.Natives.Where(n => n.InteractiveFaction))
            {
                int goodwill = (int)(100 * faction.GoodWill);
                string goodwillStr = goodwill > 0 ? "+" + goodwill.ToString() : goodwill.ToString();
                toReturn[faction.Name + " (" + faction.Race + ") " + goodwillStr] = faction.PrimaryColor;
            }
            return toReturn;
        }
        
        
        public void GenerateVolcanoes(int width, int height)
        {
            int volcanoSamples = 4;
            float volcanoSize = 11;

            for(int i = 0; i < (int) Settings.NumVolcanoes; i++) // Todo: Need to move the random used for world generation into settings.
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
                            Overworld.Map[x, y].Height = 0.1f;
                        }

                        if(dist < volcanoSize)
                            Overworld.Map[x, y].Biome = BiomeLibrary.GetBiome("Waste").Biome;
                    }
                }
            }
        }

        public void GenerateWorld()
        {
#if !DEBUG
           try
#endif
            {
                if (Overworld.Name == null)
                {
                    Overworld.Name = OverworldGenerationSettings.GetRandomWorldName();
                }

                MathFunctions.Random = new ThreadSafeRandom(Settings.Seed);
                CurrentState = GenerationState.Generating;
                
                LoadingMessage = "Init..";
                Overworld.heightNoise.Seed = Settings.Seed;
                Overworld.Map = new OverworldCell[Settings.Width, Settings.Height];

                Progress = 0.01f;

                LoadingMessage = "Height Map ...";
                float[,] heightMapLookup = null;
                heightMapLookup = Overworld.GenerateHeightMapLookup(Settings.Width, Settings.Height);
                Overworld.GenerateHeightMapFromLookup(Overworld.Map, heightMapLookup, Settings.Width, Settings.Height, 1.0f, false);

                Progress = 0.05f;

                int numRains = (int)Settings.NumRains;
                int rainLength = 250;
                int numRainSamples = 3;

                for (int x = 0; x < Settings.Width; x++)
                {
                    for (int y = 0; y < Settings.Height; y++)
                    {
                        Overworld.Map[x, y].Erosion = 1.0f;
                        Overworld.Map[x, y].Weathering = 0;
                        Overworld.Map[x, y].Faults = 1.0f;
                    }
                }

                LoadingMessage = "Climate";
                for (int x = 0; x < Settings.Width; x++)
                {
                    for (int y = 0; y < Settings.Height; y++)
                    {
                        Overworld.Map[x, y].Temperature = ((float)(y) / (float)(Settings.Height)) * Settings.TemperatureScale;
                        //Overworld.Map[x, y].Rainfall = Math.Max(Math.Min(Overworld.noise(x, y, 1000.0f, 0.01f) + Overworld.noise(x, y, 100.0f, 0.1f) * 0.05f, 1.0f), 0.0f) * RainfallScale;
                    }
                }

                //Overworld.Distort(width, height, 60.0f, 0.005f, Overworld.ScalarFieldType.Rainfall);
                OverworldImageOperations.Distort(Overworld.Map, Settings.Width, Settings.Height, 30.0f, 0.005f, OverworldField.Temperature);
                for (int x = 0; x < Settings.Width; x++)
                {
                    for (int y = 0; y < Settings.Height; y++)
                    {
                        Overworld.Map[x, y].Temperature = Math.Max(Math.Min(Overworld.Map[x, y].Temperature, 1.0f), 0.0f);
                    }
                }
        
                int numVoronoiPoints = (int)Settings.NumFaults;

                if (UpdatePreview != null) UpdatePreview();

                Progress = 0.1f;
                LoadingMessage = "Faults ...";

                #region voronoi

                Voronoi(Settings.Width, Settings.Height, numVoronoiPoints);

                #endregion

                Overworld.GenerateHeightMapFromLookup(Overworld.Map, heightMapLookup, Settings.Width, Settings.Height, 1.0f, true);

                Progress = 0.2f;

                Overworld.GenerateHeightMapFromLookup(Overworld.Map, heightMapLookup, Settings.Width, Settings.Height, 1.0f, true);

                Progress = 0.25f;
                if (UpdatePreview != null) UpdatePreview();
                LoadingMessage = "Erosion...";

                #region erosion

                float[,] buffer = new float[Settings.Width, Settings.Height];
                Erode(Settings.Width, Settings.Height, Settings.SeaLevel, Overworld.Map, numRains, rainLength, numRainSamples, buffer);
                Overworld.GenerateHeightMapFromLookup(Overworld.Map, heightMapLookup, Settings.Width, Settings.Height, 1.0f, true);

                #endregion

                Progress = 0.9f;


                LoadingMessage = "Blur.";
                OverworldImageOperations.Blur(Overworld.Map, Settings.Width, Settings.Height, OverworldField.Erosion);

                LoadingMessage = "Generate height.";
                Overworld.GenerateHeightMapFromLookup(Overworld.Map, heightMapLookup, Settings.Width, Settings.Height, 1.0f, true);


                LoadingMessage = "Rain";
                CalculateRain(Settings.Width, Settings.Height);

                LoadingMessage = "Biome";
                for (int x = 0; x < Settings.Width; x++)
                    for (int y = 0; y < Settings.Height; y++)
                        Overworld.Map[x, y].Biome = BiomeLibrary.GetBiomeForConditions(Overworld.Map[x, y].Temperature, Overworld.Map[x, y].Rainfall, Overworld.Map[x, y].Height).Biome;

                LoadingMessage = "Volcanoes";

                GenerateVolcanoes(Settings.Width, Settings.Height);

                if (UpdatePreview != null) UpdatePreview();


                LoadingMessage = "Factions";
                FactionSet library = new FactionSet();
                library.Initialize(null, new CompanyInformation());

                Settings.Natives = new List<OverworldFaction>();
                foreach (var fact in library.Factions)
                    Settings.Natives.Add(fact.Value.ParentFaction); // Todo: Don't create a whole faction just to grab the overworldfactions from them.
                for (int i = 0; i < Settings.NumCivilizations; i++)
                    Settings.Natives.Add(library.GenerateOverworldFaction(Settings, i, Settings.NumCivilizations));

                SeedCivs(Overworld.Map, Settings.Natives.Count, Settings.Natives);
                GrowCivs(Overworld.Map, 200, Settings.Natives);



                for (int x = 0; x < Settings.Width; x++)
                {
                    Overworld.Map[x, 0] = Overworld.Map[x, 1];
                    Overworld.Map[x, Settings.Height - 1] = Overworld.Map[x, Settings.Height - 2];
                }

                for (int y = 0; y < Settings.Height; y++)
                {
                    Overworld.Map[0, y] = Overworld.Map[1, y];
                    Overworld.Map[Settings.Width - 1, y] = Overworld.Map[Settings.Width - 2, y];
                }

                CurrentState = GenerationState.Finished;
                LoadingMessage = "";
                Progress = 1.0f;
            }
#if !DEBUG
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

            OverworldImageOperations.Distort(Overworld.Map, width, height, 5.0f, 0.03f, OverworldField.Rainfall);

        }

        internal void LoadDummy(Color[] color, GraphicsDevice Device)
        {
            CurrentState = GenerationState.Finished;
            Progress = 1.0f;
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

            ScaleMap(Overworld.Map, width, height, OverworldField.Faults);
            OverworldImageOperations.Distort(Overworld.Map, width, height, 20, 0.01f, OverworldField.Faults);
        }

        private void Erode(int width, int height, float seaLevel, OverworldCell[,] heightMap, int numRains, int rainLength, int numRainSamples, float[,] buffer)
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
                    float h = OverworldImageOperations.GetHeight(buffer, currentPos);

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
                    Vector2 g = OverworldImageOperations.GetMinNeighbor(buffer, currentPos);

                    float h = OverworldImageOperations.GetHeight(buffer, currentPos);

                    if(h < seaLevel|| g.LengthSquared() < 1e-12)
                    {
                        break;
                    }

                    OverworldImageOperations.MinBlend(Overworld.Map, currentPos, erosionRate * OverworldImageOperations.GetValue(Overworld.Map, currentPos, OverworldField.Erosion), OverworldField.Erosion);

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
                        float h = OverworldImageOperations.GetHeight(buffer, p);
                        float lowestNeighbor = 0.0f;
                        for(int i = 0; i < 4; i++)
                        {
                            float nh = OverworldImageOperations.GetHeight(buffer, p + neighbs[i]);
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
                            OverworldImageOperations.AddValue(Overworld.Map, p + maxDiffNeigh, OverworldField.Weathering, (float)(maxDiff * 0.4f));
                            OverworldImageOperations.AddValue(Overworld.Map, p, OverworldField.Weathering, (float)(-maxDiff * 0.4f));
                        }
                    }
                }

                for(int x = 0; x < width; x++)
                {
                    for(int y = 0; y < height; y++)
                    {
                        Vector2 p = new Vector2(x, y);
                        float w = OverworldImageOperations.GetValue(Overworld.Map, p, OverworldField.Weathering);
                        OverworldImageOperations.AddHeight(buffer, p, w);
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

        private static void ScaleMap(OverworldCell[,] map, int width, int height, OverworldField fieldType)
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


        public Point? GetRandomLandPoint(OverworldCell[,] map)
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

        public void SeedCivs(OverworldCell[,] map, int numCivs, List<OverworldFaction> civs )
        {
            for (int i = 0; i < numCivs; i++)
            {
                if (civs[i].InteractiveFaction && !civs[i].IsMotherland)
                {
                    Point? randomPoint = GetRandomLandPoint(map);

                    if (randomPoint == null) continue;
                    else
                    {
                        map[randomPoint.Value.X, randomPoint.Value.Y].Faction = (byte)(i + 1);
                    }
                }
            }
        }

        public void GrowCivs(OverworldCell[,] map, int iters, List<OverworldFaction> civs)
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
                                var biomeName = BiomeLibrary.GetBiome(biome).Name;
                                var myFaction = civs[faction - 1];
                                var race = Library.GetRace(myFaction.Race);
                                if (race.Biomes.ContainsKey(biomeName))
                                    map[x + deltas[minNeighbor].X, y + deltas[minNeighbor].Y].Biome =
                                        BiomeLibrary.GetBiome(race.Biomes[biomeName]).Biome;
                            }
                        }
                    }
                }
            }            
        }

        // Spawn rectangle in world map pixel units
        public Rectangle GetSpawnRectangle() // Todo: Kill
        {
            return Settings.InstanceSettings.Cell.Bounds;
        }
    }
}
