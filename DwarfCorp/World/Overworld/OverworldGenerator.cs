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
    public class OverworldGenerator
    {
        public enum GenerationState
        {
            NotStarted,
            Generating,
            Finished
        }

        public GenerationState CurrentState { get; private set; }

        private Overworld Overworld;
        public string LoadingMessage = "";
        private Thread genThread;
        public float Progress = 0.0f;
        private Random Random;

        public OverworldGenerator(Overworld Overworld, bool ClearOverworld)
        {
            CurrentState = GenerationState.NotStarted;
            this.Overworld = Overworld;

            Random = new Random(Overworld.Seed);

            if (ClearOverworld)
                Overworld.Map = new OverworldMap(Overworld.Width, Overworld.Height);
        }

        public void Abort()
        {
            if (genThread != null && genThread.IsAlive)
                genThread.Abort();
        }

        // Todo: Should belong to preview
        public IEnumerable<KeyValuePair<string, Color>> GetSpawnStats()
        {
            var biomes = new HashSet<byte>();
            var spawnRect = Overworld.InstanceSettings.Cell.Bounds;

            for (int x = Math.Max(spawnRect.X, 0); x < Math.Min(spawnRect.X + spawnRect.Width, Overworld.Width - 1); x++)
                for (int y = Math.Max(spawnRect.Y, 0); y < Math.Min(spawnRect.Y + spawnRect.Height, Overworld.Height - 1); y++)
                    biomes.Add(Overworld.Map.Map[x, y].Biome);

            if (Overworld.InstanceSettings.Cell.Faction == null)
                yield return new KeyValuePair<string, Color>("Unclaimed land.", Color.White);
            else
                yield return new KeyValuePair<string, Color>(String.Format("Claimed by: {0}", Overworld.InstanceSettings.Cell.Faction.Name), Color.White);
            
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
              
        public void Generate()
        {
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
            foreach (var faction in Overworld.Natives.Where(n => n.InteractiveFaction))
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

            for(int i = 0; i < (int) Overworld.GenerationSettings.NumVolcanoes; i++) // Todo: Need to move the random used for world generation into settings.
            {
                Vector2 randomPos = new Vector2((float) (Random.NextDouble() * width), (float) (Random.NextDouble() * height));
                float maxFaults = Overworld.Map.Map[(int) randomPos.X, (int) randomPos.Y].Height;
                for(int j = 0; j < volcanoSamples; j++)
                {
                    Vector2 randomPos2 = new Vector2((float) (Random.NextDouble() * width), (float) (Random.NextDouble() * height));
                    float faults = Overworld.Map.Map[(int) randomPos2.X, (int) randomPos2.Y].Height;

                    if(faults > maxFaults)
                    {
                        randomPos = randomPos2;
                        maxFaults = faults;
                    }
                }

                for(int dx = -(int) volcanoSize; dx <= (int) volcanoSize; dx++)
                {
                    for(int dy = -(int) volcanoSize; dy <= (int) volcanoSize; dy++)
                    {
                        int x = (int) MathFunctions.Clamp(randomPos.X + dx, 0, width - 1);
                        int y = (int) MathFunctions.Clamp(randomPos.Y + dy, 0, height - 1);

                        float dist = (float) Math.Sqrt(dx * dx + dy * dy);
                        float fDist = (float) Math.Sqrt((dx / 3.0f) * (dx / 3.0f) + (dy / 3.0f) * (dy / 3.0f));

                        float f = (float) (Math.Pow(Math.Sin(fDist), 3.0f) + 1.0f) * 0.2f;
                        Overworld.Map.Map[x, y].Height += f;

                        if(dist <= 2)
                        {
                            Overworld.Map.Map[x, y].Height = 0.1f;
                        }

                        if(dist < volcanoSize)
                            Overworld.Map.Map[x, y].Biome = BiomeLibrary.GetBiome("Waste").Biome;
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
                CurrentState = GenerationState.Generating;
                
                LoadingMessage = "Init..";
                OverworldMap.heightNoise.Seed = Overworld.Seed;
                Overworld.Map.Map = new OverworldCell[Overworld.Width, Overworld.Height];

                Progress = 0.01f;

                LoadingMessage = "Height Map ...";
                float[,] heightMapLookup = null;
                heightMapLookup = OverworldMap.GenerateHeightMapLookup(Overworld.Width, Overworld.Height);
                Overworld.Map.CreateHeightFromLookup(heightMapLookup);

                Progress = 0.05f;

                int numRains = (int)Overworld.GenerationSettings.NumRains;
                int rainLength = 250;
                int numRainSamples = 3;

                for (int x = 0; x < Overworld.Width; x++)
                {
                    for (int y = 0; y < Overworld.Height; y++)
                    {
                        Overworld.Map.Map[x, y].Erosion = 1.0f;
                        Overworld.Map.Map[x, y].Weathering = 0;
                        Overworld.Map.Map[x, y].Faults = 1.0f;
                    }
                }

                LoadingMessage = "Climate";
                for (int x = 0; x < Overworld.Width; x++)
                    for (int y = 0; y < Overworld.Height; y++)
                        Overworld.Map.Map[x, y].Temperature = ((float)(y) / (float)(Overworld.Height)) * Overworld.GenerationSettings.TemperatureScale;

                OverworldImageOperations.Distort(Overworld.Map.Map, Overworld.Width, Overworld.Height, 30.0f, 0.005f, OverworldField.Temperature);
                for (int x = 0; x < Overworld.Width; x++)
                    for (int y = 0; y < Overworld.Height; y++)
                        Overworld.Map.Map[x, y].Temperature = Math.Max(Math.Min(Overworld.Map.Map[x, y].Temperature, 1.0f), 0.0f);
        
                int numVoronoiPoints = (int)Overworld.GenerationSettings.NumFaults;

                Progress = 0.1f;
                LoadingMessage = "Faults ...";

                Voronoi(Overworld.Width, Overworld.Height, numVoronoiPoints);
                Overworld.Map.CreateHeightFromLookupWithErosion(heightMapLookup);

                Progress = 0.2f;

                Overworld.Map.CreateHeightFromLookupWithErosion(heightMapLookup);

                Progress = 0.25f;

                LoadingMessage = "Erosion...";
                var buffer = new float[Overworld.Width, Overworld.Height];
                Erode(Overworld.Width, Overworld.Height, Overworld.GenerationSettings.SeaLevel, Overworld.Map.Map, numRains, rainLength, numRainSamples, buffer);
                Overworld.Map.CreateHeightFromLookupWithErosion(heightMapLookup);

                Progress = 0.9f;

                LoadingMessage = "Blur.";
                OverworldImageOperations.Blur(Overworld.Map.Map, Overworld.Width, Overworld.Height, OverworldField.Erosion);

                LoadingMessage = "Generate height.";
                Overworld.Map.CreateHeightFromLookupWithErosion(heightMapLookup);

                LoadingMessage = "Rain";
                CalculateRain(Overworld.Width, Overworld.Height);

                LoadingMessage = "Biome";
                for (int x = 0; x < Overworld.Width; x++)
                    for (int y = 0; y < Overworld.Height; y++)
                        Overworld.Map.Map[x, y].Biome = BiomeLibrary.GetBiomeForConditions(Overworld.Map.Map[x, y].Temperature, Overworld.Map.Map[x, y].Rainfall, Overworld.Map.Map[x, y].Height).Biome;

                LoadingMessage = "Volcanoes";
                GenerateVolcanoes(Overworld.Width, Overworld.Height);

                LoadingMessage = "Factions";
                FactionSet library = new FactionSet();
                library.Initialize(null, new CompanyInformation());

                Overworld.Natives = new List<OverworldFaction>();
                foreach (var fact in library.Factions)
                    Overworld.Natives.Add(fact.Value.ParentFaction); // Todo: Don't create a whole faction just to grab the overworldfactions from them.
                for (int i = 0; i < Overworld.GenerationSettings.NumCivilizations; i++)
                    Overworld.Natives.Add(library.GenerateOverworldFaction(Overworld, i, Overworld.GenerationSettings.NumCivilizations));
                Diplomacy.Initialize(Overworld);

                Overworld.ColonyCells = new CellSet("World\\colonies");
                Overworld.InstanceSettings = new InstanceSettings(Overworld.ColonyCells.GetCellAt(16, 0));

                SeedCivs();
                GrowCivs();

                for (int x = 0; x < Overworld.Width; x++)
                {
                    Overworld.Map.Map[x, 0] = Overworld.Map.Map[x, 1];
                    Overworld.Map.Map[x, Overworld.Height - 1] = Overworld.Map.Map[x, Overworld.Height - 2];
                }

                for (int y = 0; y < Overworld.Height; y++)
                {
                    Overworld.Map.Map[0, y] = Overworld.Map.Map[1, y];
                    Overworld.Map.Map[Overworld.Width - 1, y] = Overworld.Map.Map[Overworld.Width - 2, y];
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
                float currentMoisture = Overworld.GenerationSettings.RainfallScale * 10;
                for (int x = 0; x < width; x++)
                {
                    float h = Overworld.Map.Map[x, y].Height;
                    bool isWater = h < Overworld.GenerationSettings.SeaLevel;

                    if (isWater)
                    {
                        currentMoisture += MathFunctions.Rand(0.1f, 0.3f);
                        currentMoisture = Math.Min(currentMoisture, Overworld.GenerationSettings.RainfallScale * 20);
                        Overworld.Map.Map[x, y].Rainfall = 0.5f;
                    }
                    else
                    {
                        float rainAmount = currentMoisture * 0.017f * h + currentMoisture * 0.0006f;
                        currentMoisture -= rainAmount;
                        float evapAmount = MathFunctions.Rand(0.01f, 0.02f);
                        currentMoisture += evapAmount;
                        Overworld.Map.Map[x, y].Rainfall = rainAmount * Overworld.GenerationSettings.RainfallScale * Overworld.Width * 0.015f;
                    }
                }
            }

            OverworldImageOperations.Distort(Overworld.Map.Map, width, height, 5.0f, 0.03f, OverworldField.Rainfall);
        }

        internal void LoadDummy()
        {
            CurrentState = GenerationState.Finished;
            Progress = 1.0f;
        }

        private void Voronoi(int width, int height, int numVoronoiPoints)
        {
            List<List<Vector2>> vPoints = new List<List<Vector2>>();
            List<float> rands = new List<float>();

            for(int i = 0; i < numVoronoiPoints; i++)
            {
                Vector2 v = GetEdgePoint(width, height);

                for(int j = 0; j < 4; j++)
                {
                    List<Vector2> line = new List<Vector2>();
                    rands.Add(1.0f);

                    line.Add(v);
                    v += new Vector2(MathFunctions.Rand() - 0.5f, MathFunctions.Rand() - 0.5f) * Overworld.Width * 0.5f;
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
                for(int y = 0; y < height; y++)
                    Overworld.Map.Map[x, y].Faults = GetVoronoiValue(nodes, x, y);

            ScaleMap(Overworld.Map.Map, width, height, OverworldField.Faults);
            OverworldImageOperations.Distort(Overworld.Map.Map, width, height, 20, 0.01f, OverworldField.Faults);
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
                    int randX = Random.Next(1, width - 1);
                    int randY = Random.Next(1, height - 1);

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

                    OverworldImageOperations.MinBlend(Overworld.Map.Map, currentPos, erosionRate * OverworldImageOperations.GetValue(Overworld.Map.Map, currentPos, OverworldField.Erosion), OverworldField.Erosion);

                    velocity = 0.1f * g + 0.7f * velocity + 0.2f * MathFunctions.RandVector2Circle();
                    currentPos += velocity;
                }
            }
        }

        private void Weather(int width, int height, float T, Vector2[] neighbs, float[,] buffer)
        {
            for(int x = 0; x < width; x++)
                for(int y = 0; y < height; y++)
                    buffer[x, y] = Overworld.Map.Map[x, y].Height * Overworld.Map.Map[x, y].Faults;

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
                            OverworldImageOperations.AddValue(Overworld.Map.Map, p + maxDiffNeigh, OverworldField.Weathering, (float)(maxDiff * 0.4f));
                            OverworldImageOperations.AddValue(Overworld.Map.Map, p, OverworldField.Weathering, (float)(-maxDiff * 0.4f));
                        }
                    }
                }

                for(int x = 0; x < width; x++)
                {
                    for(int y = 0; y < height; y++)
                    {
                        Vector2 p = new Vector2(x, y);
                        float w = OverworldImageOperations.GetValue(Overworld.Map.Map, p, OverworldField.Weathering);
                        OverworldImageOperations.AddHeight(buffer, p, w);
                        Overworld.Map.Map[x, y].Weathering = 0.0f;
                    }
                }
            }

            for(int x = 0; x < width; x++)
                for(int y = 0; y < height; y++)
                    Overworld.Map.Map[x, y].Weathering = buffer[x, y] - Overworld.Map.Map[x, y].Height * Overworld.Map.Map[x, y].Faults;
        }

        private Vector2 GetEdgePoint(int width, int height)
        {
            return new Vector2(Random.Next(0, width), Random.Next(0, height));
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

            return (float) (1e-2*(maxNode.dist / Overworld.Width));
        }


        public Point? GetRandomLandPoint(OverworldCell[,] map)
        {
            const int maxIters = 1000;
            int i = 0;
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            while (i < maxIters)
            {
                int x = Random.Next(0, width);
                int y = Random.Next(0, height);

                if (map[x, y].Height > Overworld.GenerationSettings.SeaLevel)
                {
                    return new Point(x, y);
                }

                i++;
            }

            return null;
        }

        public void SeedCivs()
        {
            foreach (var civ in Overworld.Natives)
                if (civ.InteractiveFaction && !civ.IsCorporate)
                {
                    var randomCell = Overworld.ColonyCells.EnumerateCells().Where(c => c.Faction == null).SelectRandom();
                    randomCell.Faction = civ;
                }
        }

        public void GrowCivs()
        {
            while (true)
            {
                var cellsChanged = 0;
                foreach (var cell in Overworld.ColonyCells.EnumerateCells().Where(c => c.Faction == null).OrderBy(c => Random.NextDouble()))
                {
                    var neighborCiv = Overworld.ColonyCells.EnumerateManhattanNeighbors(cell).Where(c => c.Faction != null).SelectRandom();
                    if (neighborCiv != null)
                    {
                        cell.Faction = neighborCiv.Faction;
                        cellsChanged += 1;
                    }
                }

                if (cellsChanged == 0)
                    return;
            }
        }
    }
}
