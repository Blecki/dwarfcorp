using System;
using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using Newtonsoft.Json.Schema;


namespace DwarfCorp
{

    /// <summary>
    /// The overworld is a 2D map specifying biomes,
    /// temperature, terrain height, etc.  Chunks are generated
    /// from the overworld.
    /// </summary>
    public class Overworld
    {
        public enum WaterType
        {
            None,
            River,
            Lake,
            Ocean,
            Spring,
            Volcano
        }

        public enum ScalarFieldType
        {
            Erosion,
            Weathering,
            Faults,
            Height,
            Temperature,
            Rainfall,
            Factions
        }

        public struct MapData
        {
            public float Erosion 
            { 
                get 
                {
                    return (Erosion_) / 255.0f;
                }
                set
                {
                    Erosion_ = (byte)(Math.Min(Math.Max(value * 255.0f, 0.0f), 255.0f));
                }
            }

            public float Weathering
            {
                get
                {
                    return (Weathering_) / 255.0f;
                }
                set
                {
                    Weathering_ = (byte)(Math.Min(Math.Max(value * 255.0f, 0.0f), 255.0f));
                }
            }

            public float Faults
            {
                get
                {
                    return (Faults_) / 255.0f;
                }
                set
                {
                    Faults_ = (byte)(Math.Min(Math.Max(value * 255.0f, 0.0f), 255.0f));
                }
            }

            public float Height
            {
                get
                {
                    return (Height_) / 255.0f;
                }
                set
                {
                    Height_ = (byte)(Math.Min(Math.Max(value * 255.0f, 0.0f), 255.0f));
                }
            }

            public float Temperature
            {
                get { return (Temperature_)/255.0f; }
                set { Temperature_ = (byte)(Math.Min(Math.Max(value * 255.0f, 0.0f), 255.0f)); }
            }

            public float Rainfall
            {
                get { return (Rainfall_) / 255.0f; }
                set { Rainfall_ = (byte)(Math.Min(Math.Max(value * 255.0f, 0.0f), 255.0f)); }
            }

            public byte Faction { get; set; }

            private byte Erosion_;
            private byte Weathering_;
            private byte Faults_;
            private byte Height_;
            private byte Temperature_;
            private byte Rainfall_;
            public WaterType Water;
            public Biome Biome;

            public float GetValue(ScalarFieldType type)
            {
                switch(type)
                {
                    case ScalarFieldType.Erosion:
                        return Erosion;
                    case ScalarFieldType.Faults:
                        return Faults;
                    case ScalarFieldType.Height:
                        return Height;
                    case ScalarFieldType.Rainfall:
                        return Rainfall;
                    case ScalarFieldType.Temperature:
                        return Temperature;
                    case ScalarFieldType.Weathering:
                        return Weathering;
                    case ScalarFieldType.Factions:
                        return Faction;
                }

                return -1.0f;
            }

            public void SetValue(ScalarFieldType type, float value)
            {
                switch(type)
                {
                    case ScalarFieldType.Erosion:
                        Erosion = value;
                        break;
                    case ScalarFieldType.Faults:
                        Faults = value;
                        break;
                    case ScalarFieldType.Height:
                        Height = value;
                        break;
                    case ScalarFieldType.Rainfall:
                        Rainfall = value;
                        break;
                    case ScalarFieldType.Temperature:
                        Temperature = value;
                        break;
                    case ScalarFieldType.Weathering:
                        Weathering = value;
                        break;
                    case ScalarFieldType.Factions:
                        Faction = (byte) (value*255.0f);
                        break;
                }
            }
        }

        public static Dictionary<string, Color> JetColors = new Dictionary<string, Color>
        {
            {"Lowest", Color.Cyan},
            {"Low", Color.Blue},
            {"Med", Color.Yellow},
            {"High", Color.Red},
            {"Highest", Color.White}
        };

        public static Dictionary<string, Color> HeightColors = new Dictionary<string, Color>
        {
            {"Sea", new Color(30, 30, 150)},
            {"Water", new Color(50, 50, 255)},
            {"Shore", new Color(180, 180, 100)},
            {"Lowlands", new Color(50, 180, 40)},
            {"Highlands", new Color(20, 100, 20)},
            {"Mountains", new Color(80, 70, 50)},
            {"Peaks", new Color(200, 200, 200)},
        };


        public static Perlin heightNoise = new Perlin(PlayState.Random.Next());

        public static List<Vector2> Volcanoes { get; set; }
        
        public static MapData[,] Map { get; set; }
        public static string Name { get; set; }
        public static List<Faction> NativeFactions { get; set; }

        public static ColorGradient JetGradient = null;

        public enum Biome
        {
            Desert,
            Grassland,
            Forest,
            Tundra,
            Taiga,
            Jungle,
            Waste
        }


        private static Vector2[] deltas2d =
        {
            new Vector2(-1, 0),
            new Vector2(1, 0),
            new Vector2(0, -1),
            new Vector2(0, 1)
        };

        public static Biome GetBiome(float temp, float rainfall, float height)
        {

            Overworld.Biome closest = Biome.Waste;
            float closestDist = float.MaxValue;
            foreach (var pair in BiomeLibrary.Biomes)
            {
                float dist = Math.Abs(pair.Value.Temp - temp) + Math.Abs(pair.Value.Rain - rainfall) +
                             Math.Abs(pair.Value.Height - height);

                if (dist < closestDist)
                {
                    closest = pair.Key;
                    closestDist = dist;
                }
            }

            return closest;

        }

     
        #region image_processing

        public static Vector2 GetMinNeighbor(float[,] heightMap, Vector2 pos)
        {
            float toReturn = float.MaxValue;
            Vector2 vec = Vector2.Zero;
            for(float dx = -1; dx < 2; dx++)
            {
                for(float dy = -1; dy < 2; dy++)
                {
                    Vector2 nVec = new Vector2(dx, dy);
                    float hn = GetHeight(heightMap, pos + nVec);

                    if(hn < toReturn)
                    {
                        toReturn = hn;
                        vec = nVec;
                    }
                }
            }

            return vec;
        }

        public static Vector2 ApproximateGradient(float[,] heightMap, Vector2 pos)
        {
            float hx = GetHeight(heightMap, pos + new Vector2(1, 0));
            float hy = GetHeight(heightMap, pos + new Vector2(0, 1));
            float ch = GetHeight(heightMap, pos);
            Vector2 toReturn = new Vector2(hx - ch, hy - ch);

            return  toReturn;
        }

        public static void MinBlend(MapData[,] heightMap, Vector2 pos, float height, Overworld.ScalarFieldType type)
        {
            int x = Math.Max(Math.Min((int) pos.X, heightMap.GetLength(0) - 1), 0);
            int y = Math.Max(Math.Min((int) pos.Y, heightMap.GetLength(1) - 1), 0);

            float orig = heightMap[x, y].GetValue(type);
            heightMap[x, y].SetValue(type, Math.Min(orig, height));
        }

        public static void AddHeight(float[,] heightMap, Vector2 pos, float height)
        {
            int x = Math.Max(Math.Min((int) pos.X, heightMap.GetLength(0) - 1), 0);
            int y = Math.Max(Math.Min((int) pos.Y, heightMap.GetLength(1) - 1), 0);

            heightMap[x, y] += height;
        }

        public static void MultHeight(float[,] heightMap, Vector2 pos, float height)
        {
            int x = Math.Max(Math.Min((int) pos.X, heightMap.GetLength(0) - 1), 0);
            int y = Math.Max(Math.Min((int) pos.Y, heightMap.GetLength(1) - 1), 0);

            heightMap[x, y] *= height;
        }

        public static void SetHeight(float[,] heightMap, Vector2 pos, float height)
        {
            int x = Math.Max(Math.Min((int) pos.X, heightMap.GetLength(0) - 1), 0);
            int y = Math.Max(Math.Min((int) pos.Y, heightMap.GetLength(1) - 1), 0);

            heightMap[x, y] = height;
        }

        public static float GetHeight(float[,] heightMap, Vector2 pos)
        {
            int x = Math.Max(Math.Min((int) pos.X, heightMap.GetLength(0) - 1), 0);
            int y = Math.Max(Math.Min((int) pos.Y, heightMap.GetLength(1) - 1), 0);

            return heightMap[x, y];
        }

        public static float GetValue(MapData[,] map, Vector2 pos, ScalarFieldType value)
        {
            int x = Math.Max(Math.Min((int) pos.X, map.GetLength(0) - 1), 0);
            int y = Math.Max(Math.Min((int) pos.Y, map.GetLength(1) - 1), 0);

            return map[x, y].GetValue(value);
        }

        public static WaterType GetWater(MapData[,] map, Vector2 pos)
        {
            int x = Math.Max(Math.Min((int) pos.X, map.GetLength(0) - 1), 0);
            int y = Math.Max(Math.Min((int) pos.Y, map.GetLength(1) - 1), 0);

            return map[x, y].Water;
        }


        public static void AddValue(MapData[,] map, Vector2 pos, ScalarFieldType value, float amount)
        {
            int x = Math.Max(Math.Min((int) pos.X, map.GetLength(0) - 1), 0);
            int y = Math.Max(Math.Min((int) pos.Y, map.GetLength(1) - 1), 0);

            map[x, y].SetValue(value, map[x, y].GetValue(value) + amount);
        }

        public static void MultValue(MapData[,] heightMap, Vector2 pos, ScalarFieldType value, float height)
        {
            int x = Math.Max(Math.Min((int) pos.X, heightMap.GetLength(0) - 1), 0);
            int y = Math.Max(Math.Min((int) pos.Y, heightMap.GetLength(1) - 1), 0);
            float c = heightMap[x, y].GetValue(value);
            heightMap[x, y].SetValue(value, c * height);
        }

        public static void SetWater(MapData[,] heightMap, Vector2 pos, WaterType waterType)
        {
            int x = Math.Max(Math.Min((int) pos.X, heightMap.GetLength(0) - 1), 0);
            int y = Math.Max(Math.Min((int) pos.Y, heightMap.GetLength(1) - 1), 0);
            heightMap[x, y].Water = waterType;
        }

        public static float noise(float x, float y, float z, float s)
        {
            return (float) heightNoise.Noise(x * s, y * s, z * s);
        }

        private static float clamp(float x, float min, float max)
        {
            return Math.Max(Math.Min(x, max), min);
        }

        private static float pow(float x, float y)
        {
            return (float) Math.Pow(x, y);
        }

        private static float abs(float x)
        {
            return (float) Math.Abs(x);
        }

        private static float sqrt(float x)
        {
            return (float) Math.Sqrt(x);
        }

        public static float[,] CalculateGaussianKernel(int W, double sigma)
        {
            float[,] kernel = new float[W, W];
            double mean = W / 2.0;
            float sum = 0.0f;
            for(int x = 0; x < W; ++x)
            {
                for(int y = 0; y < W; ++y)
                {
                    kernel[x, y] = (float) (Math.Exp(-0.5 * (Math.Pow((x - mean) / sigma, 2.0) + Math.Pow((y - mean) / sigma, 2.0)))
                                            / (2 * Math.PI * sigma * sigma));
                    sum += kernel[x, y];
                }
            }

            for(int x = 0; x < W; ++x)
            {
                for(int y = 0; y < W; ++y)
                {
                    kernel[x, y] *= (1.0f) / sum;
                }
            }

            return kernel;
        }

        public static void Blur(MapData[,] array, int width, int height, ScalarFieldType type)
        {
            float[,] b = new float[width, height];
            float[,] kernel = CalculateGaussianKernel(10, 0.75f);

            const int kernelSizeX = 10;
            const int kernelSizeY = 10;


            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    b[x, y] = array[x, y].GetValue(type);
                }
            }

            for(int x = kernelSizeX; x < width - kernelSizeX; x++)
            {
                for(int y =kernelSizeY; y < height - kernelSizeY; y++)
                {
                    b[x, y] = 0.0f;
                    for(int dx = 0; dx < kernelSizeX; dx++)
                    {
                        for(int dy = 0; dy < kernelSizeY; dy++)
                        {
                            int nx = x + dx - kernelSizeX / 2;
                            int ny = y + dy - kernelSizeY / 2;

                            float a = array[nx, ny].GetValue(type);
                            float h = kernel[dx, dy] * a;
                            b[x, y] += h;
                        }
                    }
                }
            }


            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    array[x, y].SetValue(type, (float)(b[x, y]));
                }
            }
        }

        public static float ComputeMaxSlope(float[,] heightMap, Vector2 pos)
        {
            float h = GetHeight(heightMap, pos);

            float max = 0;
            for(int i = 0; i < 4; i++)
            {
                float s = Math.Abs(h - GetHeight(heightMap, pos + deltas2d[i]));

                if(s > max)
                {
                    max = s;
                }
            }

            return max;
        }

        private static readonly Perlin XDistort = new Perlin(PlayState.Random.Next());
        private static readonly Perlin YDistort = new Perlin(PlayState.Random.Next());

        public static void Distort(int width, int height, float distortAmount, float distortScale, ScalarFieldType fieldType)
        {
            float[,] buffer = new float[width, height];

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    buffer[x, y] = GetValue(Map, new Vector2(x, y) + new Vector2((XDistort.Noise(x * distortScale, y * distortScale, 0) * 2.0f - 1.0f) * distortAmount,
                        (YDistort.Noise(x * distortScale, y * distortScale, 0) * 2.0f - 1.0f) * distortAmount), fieldType);
                }
            }

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    Map[x, y].SetValue(fieldType, buffer[x, y]);
                }
            }
        }

        #endregion

        public static float LinearInterpolate(Vector2 position, MapData[,] map, ScalarFieldType fieldType)
        {
            float x = position.X;
            float y = position.Y;
            float x1 = (int) MathFunctions.Clamp((float) Math.Ceiling(x), 0, map.GetLength(0) - 2);
            float y1 = (int) MathFunctions.Clamp((float) Math.Ceiling(y), 0, map.GetLength(1) - 2);
            float x2 = (int) MathFunctions.Clamp((float) Math.Floor(x), 0, map.GetLength(0) - 2);
            float y2 = (int) MathFunctions.Clamp((float) Math.Floor(y), 0, map.GetLength(1) - 2);

            if(Math.Abs(x1 - x2) < 0.5f)
            {
                x1 = x1 + 1;
            }

            if(Math.Abs(y1 - y2) < 0.5f)
            {
                y1 = y1 + 1;
            }


            float q11 = map[(int) x1, (int) y1].GetValue(fieldType);
            float q12 = map[(int) x1, (int) y2].GetValue(fieldType);
            float q21 = map[(int) x2, (int) y1].GetValue(fieldType);
            float q22 = map[(int) x2, (int) y2].GetValue(fieldType);

            return MathFunctions.LinearCombination(x, y, x1, y1, x2, y2, q11, q12, q21, q22);
        }


        public static float Interpolate(float wx, float wy, float globalScale, float[,] map)
        {
            float x = (wx) / globalScale;
            float y = (wy) / globalScale;


            return MathFunctions.LinearInterpolate(new Vector2(x, y), map);
        }


        public static float ComputeHeight(float wx, float wy, float worldWidth, float worldHeight, float globalScale, bool erode)
        {
            float x = (wx) / globalScale;
            float y = (wy) / globalScale;

            const float mountainWidth = 0.04f;
            float mountain = (float) Math.Pow(noise(x, y, 0, mountainWidth), 1);
            const float continentSize = 0.03f;
            float continent = noise(x, y, 10, continentSize);
            const float hillSize = 0.1f;
            float hill = noise(x, y, 20, hillSize) * 0.02f;
            const float smallNoiseSize = 0.15f;
            float smallnoise = noise(x, y, 100, smallNoiseSize) * 0.01f;

            float h = pow(clamp((continent * mountain) + hill, 0, 1), 1);
            h += smallnoise;
            h += 0.4f;

            if(erode)
            {
                Vector2 vec = new Vector2(x, y);
                h *= LinearInterpolate(vec, Map, ScalarFieldType.Faults);
                h += LinearInterpolate(vec, Map, ScalarFieldType.Weathering);
                h *= LinearInterpolate(vec, Map, ScalarFieldType.Erosion);
            }

            h = clamp(h, 0, 1);

            return h;
        }

        public static void GenerateHeightMap(int width, int height, float globalScale, bool erode)
        {
            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    Map[x, y].Height = ComputeHeight(x, y, width, height, globalScale, erode);
                }
            }
        }


        public static void TextureFromHeightMap(string displayMode,
            MapData[,] map,
            ScalarFieldType type,
            int width, int height,
            Mutex imageMutex,
            Color[] worldData,
            Texture2D worldMap, float sealevel)
        {
            if(JetGradient == null)
            {
                List<ColorStop> stops = new List<ColorStop>();
                ColorStop first = new ColorStop
                {
                    m_color = new Color(0, 255, 255),
                    m_position = 0.0f
                };

                ColorStop second = new ColorStop
                {
                    m_color = new Color(0, 0, 255),
                    m_position = 0.2f
                };


                ColorStop third = new ColorStop
                {
                    m_color = new Color(255, 255, 0),
                    m_position = 0.4f
                };

                ColorStop fourth = new ColorStop
                {
                    m_color = new Color(255, 0, 0),
                    m_position = 0.8f
                };

                ColorStop fifth = new ColorStop
                {
                    m_color = new Color(255, 255, 255),
                    m_position = 1.0f
                };

                stops.Add(first);
                stops.Add(second);
                stops.Add(third);
                stops.Add(fourth);
                stops.Add(fifth);


                JetGradient = new ColorGradient(stops);
            }


            int stepX = map.GetLength(0) / width;
            int stepY = map.GetLength(1) / height;
            string index = "";
            for(int tx = 0; tx < width; tx++)
            {
                for(int ty = 0; ty < height; ty++)
                {
                    int x = tx * stepX;
                    int y = ty * stepY;
   
                    float h1 = map[x, y].GetValue(type);
                    Biome biome = Map[x, y].Biome;
                    if(h1 < 0.1f)
                    {
                        index = "Sea";
                    }
                    else if(h1 >= 0.1f && h1 <= sealevel)
                    {
                        index = "Water";
                    }
                    else if(displayMode == "Biomes")
                    {
                        index = "Biome";
                    }
                    else if(displayMode == "Height")
                    {
                        if(h1 >= 0.2f && h1 < 0.21f)
                        {
                            index = "Shore";
                        }
                        else if(h1 >= 0.21f && h1 < 0.4f)
                        {
                            index = "Lowlands";
                        }
                        else if(h1 >= 0.4f && h1 < 0.6f)
                        {
                            index = "Highlands";
                        }
                        else if(h1 >= 0.6f && h1 < 0.9f)
                        {
                            index = "Mountains";
                        }
                        else
                        {
                            index = "Peaks";
                        }
                    }

                    if(displayMode == "Gray")
                    {
                        Color toDraw = JetGradient.GetColor(h1);
                        worldData[y * width + x] = toDraw;
                    }
                    else if (displayMode == "Factions")
                    {
                        float h2 = map[x, y].Height;
                        byte factionColor = map[x, y].Faction;
                        

                        Color ci = Color.DarkBlue;

                        if (factionColor > 0)
                        {
                            bool inside = x > 0 && x < width - 1 && y > 0 && y < height - 1;
                            ci = NativeFactions[factionColor - 1].PrimaryColor;
                           if(inside && 
                               (map[x + 1, y].Faction != factionColor || 
                               map[x - 1, y].Faction != factionColor || 
                               map[x, y - 1].Faction != factionColor || 
                               map[x, y + 1].Faction != factionColor ||
                               map[x + 1, y + 1].Faction != factionColor ||
                               map[x - 1, y - 1].Faction != factionColor || 
                               map[x + 1, y - 1].Faction != factionColor ||
                               map[x - 1, y + 1].Faction != factionColor))
                           {
                                    ci = NativeFactions[factionColor - 1].SecondaryColor;
                           }
                        }
                        else if (h2 > sealevel)
                        {
                            ci = Color.Gray;
                        }

                        Color toDraw = new Color((float)(ci.R) * (h2 + 0.5f) / 255.0f, (float)(ci.G * (h2 + 0.5f)) / 255.0f, (float)(ci.B * (h2 + 0.5f)) / 255.0f);
                        worldData[ty * width + tx] = toDraw;
                    }
                    else
                    {
                        Color ci = displayMode == "Biomes"  && index != "Water" && index != "Sea" ? BiomeLibrary.Biomes[biome].MapColor : HeightColors[index];
                        Color toDraw = new Color((float) (ci.R) * (h1 + 0.5f) / 255.0f, (float) (ci.G * (h1 + 0.5f)) / 255.0f, (float) (ci.B * (h1 + 0.5f)) / 255.0f);
                        worldData[ty * width + tx] = toDraw;
                    }
                }
            }

            if(imageMutex != null)
            {
                imageMutex.WaitOne();
            }

            GameState.Game.GraphicsDevice.Textures[0] = null;
            worldMap.SetData(worldData);

            if(imageMutex != null)
            {
                imageMutex.ReleaseMutex();
            }
        }

        public static void CreateHillsLand(GraphicsDevice graphics)
        {
            PlayState.SeaLevel = 0.17f;
            int size = 512;
            Map = new MapData[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float temp = ComputeHeight(x, y, size, size, 3.0f, false);
                    float rain = ComputeHeight(x, y, size, size, 2.0f, false);
                    float height = ComputeHeight(x, y, size, size, 1.6f, false);
                    Map[x, y].Erosion = 1.0f;
                    Map[x, y].Weathering = 0;
                    Map[x, y].Faults = 1.0f;
                    Map[x, y].Temperature = (float)(temp * 1.0f);
                    Map[x, y].Rainfall = (float)(rain * 1.0f);
                    Map[x, y].Biome = GetBiome(temp, rain, height);
                    Map[x, y].Height = height;
                }
            }

            Color[] worldData = new Color[size * size];
            WorldGeneratorState.worldMap = new Texture2D(graphics, size, size);
            Overworld.TextureFromHeightMap("Height", Overworld.Map, Overworld.ScalarFieldType.Height, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), null, worldData, WorldGeneratorState.worldMap, PlayState.SeaLevel);
            Overworld.Name = "hills" + PlayState.Random.Next(9999);
        }

        public static void CreateCliffsLand(GraphicsDevice graphicsDevice)
        {
            PlayState.SeaLevel = 0.17f;
            int size = 512;
            Map = new MapData[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float height = ComputeHeight(x * 2.0f, y * 2.0f, size, size, 1.6f, false);

                    if (height < 0.3f)
                    {
                        height = 0.08f;
                    }
                    else if (height < 0.6f)
                    {
                        height = 0.3f;
                    }
                    else if (height < 0.8f)
                    {
                        height = 0.5f;
                    }
                    else
                    {
                        height = 0.8f;
                    }
                    
                    Map[x, y].Biome = Biome.Forest;
                    Map[x, y].Erosion = 1.0f;
                    Map[x, y].Weathering = 0;
                    Map[x, y].Faults = 1.0f;
                    Map[x, y].Temperature = 0.6f;
                    Map[x, y].Rainfall = 0.6f;
                    Map[x, y].Height = height;
                }
            }


            Color[] worldData = new Color[size * size];
            WorldGeneratorState.worldMap = new Texture2D(graphicsDevice, size, size);
            Overworld.TextureFromHeightMap("Height", Overworld.Map, Overworld.ScalarFieldType.Height, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), null, worldData, WorldGeneratorState.worldMap, PlayState.SeaLevel);
            Overworld.Name = "Cliffs_" + PlayState.Random.Next(9999);
        }

        public static void CreateUniformLand(GraphicsDevice graphics)
        {
            PlayState.SeaLevel = 0.17f;
            int size = 512;
            Map = new MapData[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Map[x, y].Biome = Biome.Grassland;
                    Map[x, y].Erosion = 1.0f;
                    Map[x, y].Weathering = 0.0f;
                    Map[x, y].Faults = 1.0f;
                    Map[x, y].Temperature = size;
                    Map[x, y].Rainfall = size;
                    Map[x, y].Height = 0.3f; //ComputeHeight(x, y, size0, size0, 5.0f, false);
                }
            }

            Color[] worldData = new Color[size * size];
            WorldGeneratorState.worldMap = new Texture2D(graphics, size, size);
            Overworld.TextureFromHeightMap("Height", Overworld.Map, Overworld.ScalarFieldType.Height, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), null, worldData, WorldGeneratorState.worldMap, PlayState.SeaLevel);
            Overworld.Name = "flat_" + PlayState.Random.Next(9999);
        }

        public static void CreateOceanLand(GraphicsDevice graphicsDevice)
        {
            PlayState.SeaLevel = 0.17f;
            int size = 512;
            Map = new MapData[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Map[x, y].Biome = Biome.Grassland;
                    Map[x, y].Erosion = 1.0f;
                    Map[x, y].Weathering = 0;
                    Map[x, y].Faults = 1.0f;
                    Map[x, y].Temperature = size;
                    Map[x, y].Rainfall = size;
                    Map[x, y].Height = 0.05f; //ComputeHeight(x, y, size0, size0, 5.0f, false);
                }
            }

            Color[] worldData = new Color[size * size];
            WorldGeneratorState.worldMap = new Texture2D(graphicsDevice, size, size);
            Overworld.TextureFromHeightMap("Height", Overworld.Map, Overworld.ScalarFieldType.Height, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), null, worldData, WorldGeneratorState.worldMap, PlayState.SeaLevel);
            Overworld.Name = "flat";
        }

    }

}