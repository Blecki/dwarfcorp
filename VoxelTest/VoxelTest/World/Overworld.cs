using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Threading;


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
            Rainfall
        }

        public struct MapData
        {
            public float Erosion;
            public float Weathering;
            public float Faults;
            public float Height;
            public float Temperature;
            public float Rainfall;
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
                }
            }
        }


        public static Perlin heightNoise = new Perlin(PlayState.Random.Next());

        public static List<Vector2> Volcanoes { get; set; }

        public static MapData[,] Map { get; set; }
        public static string Name { get; set; }

        public static ColorGradient JetGradient = null;

        public enum Biome
        {
            Desert,
            Grassland,
            Forest,
            Tundra,
            ColdForest,
            Jungle,
            Volcano
        }


        private static Vector2[] deltas2d =
        {
            new Vector2(-1, 0),
            new Vector2(1, 0),
            new Vector2(0, -1),
            new Vector2(0, 1)
        };

        public static Biome GetBiome(float temp, float rainfall, float heigh)
        {
            if(heigh > 0.9f)
            {
                return Biome.Tundra;
            }
            else if(rainfall < 0.2f)
            {
                if(temp < 0.2f || heigh > 0.9f)
                {
                    return Biome.Tundra;
                }
                else if(temp > 0.5)
                {
                    return Biome.Desert;
                }
                else
                {
                    return Biome.Grassland;
                }
            }
            else if(rainfall < 0.3f)
            {
                if(heigh > 0.9f)
                {
                    return Biome.Tundra;
                }
                else if(temp < 0.2f)
                {
                    return Biome.ColdForest;
                }
                else if(temp < 0.8f)
                {
                    return Biome.Grassland;
                }
                else
                {
                    return Biome.Desert;
                }
            }
            else
            {
                if(temp + rainfall > 1.7f)
                {
                    return Biome.Jungle;
                }
                else if(temp < 0.2f)
                {
                    return Biome.ColdForest;
                }
                else if(rainfall < 0.5f)
                {
                    return Biome.Grassland;
                }
                else
                {
                    return Biome.Forest;
                }
            }
        }

        public static void CreateUniformLand(GraphicsDevice graphics)
        {
            Map = new MapData[1000, 1000];

            for(int x = 0; x < 1000; x++)
            {
                for(int y = 0; y < 1000; y++)
                {
                    Map[x, y].Biome = Biome.Forest;
                    Map[x, y].Erosion = 1.0f;
                    Map[x, y].Weathering = 0.0f;
                    Map[x, y].Faults = 1.0f;
                    Map[x, y].Temperature = 0.6f;
                    Map[x, y].Rainfall = 0.6f;
                    Map[x, y].Height = 0.3f; //ComputeHeight(x, y, 1000, 1000, 5.0f, false);
                }
            }

            Color[] worldData = new Color[1000 * 1000];
            WorldGeneratorState.worldMap = new Texture2D(graphics, 1000, 1000);
            Overworld.TextureFromHeightMap("Height", Overworld.Map, Overworld.ScalarFieldType.Height, Overworld.Map.GetLength(0), Overworld.Map.GetLength(1), null, worldData, WorldGeneratorState.worldMap);
            Overworld.Name = "flat";
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

            return toReturn;
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
            double mean = W / 2;
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

            int kernelSizeX = 10;
            int kernelSizeY = 10;


            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    b[x, y] = 0;

                    for(int dx = 0; dx < kernelSizeX; dx++)
                    {
                        for(int dy = 0; dy < kernelSizeY; dy++)
                        {
                            int nx = x + dx - kernelSizeX / 2;
                            int ny = y + dy - kernelSizeY / 2;

                            if(nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                float a = array[nx, ny].GetValue(type);

                                float h = kernel[dx, dy] * a;
                                b[x, y] += h;
                            }
                            else
                            {
                                b[x, y] = array[x, y].GetValue(type);
                                goto exitloop;
                            }
                        }
                    }

                    exitloop:
                    continue;
                }
            }


            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    array[x, y].SetValue(type, b[x, y]);
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

        private static Perlin xDistort = new Perlin(PlayState.Random.Next());
        private static Perlin yDistort = new Perlin(PlayState.Random.Next());

        public static void Distort(int width, int height, float distortAmount, float distortScale, ScalarFieldType fieldType)
        {
            float[,] buffer = new float[width, height];

            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    buffer[x, y] = GetValue(Map, new Vector2(x, y) + new Vector2((xDistort.Noise(x * distortScale, y * distortScale, 0) * 2.0f - 1.0f) * distortAmount,
                        (yDistort.Noise(x * distortScale, y * distortScale, 0) * 2.0f - 1.0f) * distortAmount), fieldType);
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

            return MathFunctions.LinearCombination(x, y, x1, y1, x2, y2, q11, q22, q21, q22);
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
            float width = worldWidth;
            float height = worldHeight;

            float mountainWidth = 0.04f;
            float mountain = (float) Math.Pow(noise(x, y, 0, mountainWidth), 1);
            float continentSize = 0.03f;
            float continent = noise(x, y, 10, continentSize);
            float hillSize = 0.1f;
            float hill = noise(x, y, 20, hillSize) * 0.02f;
            float smallNoiseSize = 0.15f;
            float smallnoise = noise(x, y, 100, smallNoiseSize) * 0.01f;
            float cliffiness = 0;

            float dx = abs(x - width / 2);
            float dy = abs(y - height / 2);

            float squareDist = sqrt(pow(dx, 2) + pow(dy, 2));

            float h = pow(clamp((continent * mountain) + hill, 0, 1), 1);

            h += smallnoise + cliffiness;

            h += 0.4f;

            if(erode)
            {
                h *= LinearInterpolate(new Vector2(x, y), Map, ScalarFieldType.Faults);
                h += LinearInterpolate(new Vector2(x, y), Map, ScalarFieldType.Weathering);
                h *= LinearInterpolate(new Vector2(x, y), Map, ScalarFieldType.Erosion);
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
            Texture2D worldMap)
        {
            if(JetGradient == null)
            {
                List<ColorStop> stops = new List<ColorStop>();
                ColorStop first = new ColorStop();
                first.m_color = new Color(0, 255, 255);
                first.m_position = 0.0f;

                ColorStop second = new ColorStop();
                second.m_color = new Color(0, 0, 255);
                second.m_position = 0.2f;


                ColorStop third = new ColorStop();
                third.m_color = new Color(255, 255, 0);
                third.m_position = 0.4f;

                ColorStop fourth = new ColorStop();
                fourth.m_color = new Color(255, 0, 0);
                fourth.m_position = 0.8f;

                ColorStop fifth = new ColorStop();
                fifth.m_color = new Color(255, 255, 255);
                fifth.m_position = 1.0f;

                stops.Add(first);
                stops.Add(second);
                stops.Add(third);
                stops.Add(fourth);
                stops.Add(fifth);


                JetGradient = new ColorGradient(stops);
            }

            int DEEP_WATER = 0;
            int WATER = 1;
            int SAND = 2;
            int PLAINS = 3;
            int HILLS = 4;
            int MOUNTAINS = 5;
            int PEAKS = 6;
            int SNOWCAP = 7;
            int COLDFOREST = 8;
            int FOREST = 9;
            int GRASSLAND = 10;
            int JUNGLE = 11;
            int TUNDRA = 12;
            int DESERT = 13;
            int RIVER = 14;
            int VOLCANO = 15;

            Color[] colorIndex = new Color[16];
            colorIndex[DEEP_WATER] = new Color(30, 30, 150);
            colorIndex[WATER] = new Color(50, 50, 255);
            colorIndex[SAND] = new Color(180, 180, 100);
            colorIndex[PLAINS] = new Color(50, 180, 40);
            colorIndex[HILLS] = new Color(20, 100, 20);
            colorIndex[MOUNTAINS] = new Color(80, 70, 50);
            colorIndex[PEAKS] = new Color(100, 100, 100);
            colorIndex[SNOWCAP] = new Color(200, 200, 200);
            colorIndex[COLDFOREST] = new Color(200, 255, 200);
            colorIndex[FOREST] = new Color(50, 150, 50);
            colorIndex[GRASSLAND] = new Color(50, 255, 40);
            colorIndex[JUNGLE] = new Color(20, 100, 20);
            colorIndex[TUNDRA] = new Color(200, 200, 200);
            colorIndex[DESERT] = new Color(180, 180, 100);
            colorIndex[RIVER] = new Color(80, 80, 255);
            colorIndex[VOLCANO] = new Color(255, 200, 0);

            int stepX = map.GetLength(0) / width;
            int stepY = map.GetLength(1) / height;

            for(int tx = 0; tx < width; tx++)
            {
                for(int ty = 0; ty < height; ty++)
                {
                    int x = tx * stepX;
                    int y = ty * stepY;
                    int index = 0;
                    float h1 = map[x, y].GetValue(type);
                    if(h1 < 0.1f)
                    {
                        index = DEEP_WATER;
                    }
                    else if(h1 >= 0.1f && h1 <= 0.17f)
                    {
                        index = WATER;
                    }
                    else if(displayMode == "Biomes")
                    {
                        if(map[x, y].Water == WaterType.River)
                        {
                            index = RIVER;
                        }
                        else
                        {
                            Biome biome = Map[x, y].Biome;

                            switch(biome)
                            {
                                case Biome.ColdForest:
                                    index = COLDFOREST;
                                    break;
                                case Biome.Forest:
                                    index = FOREST;
                                    break;
                                case Biome.Grassland:
                                    index = GRASSLAND;
                                    break;
                                case Biome.Jungle:
                                    index = JUNGLE;
                                    break;
                                case Biome.Tundra:
                                    index = TUNDRA;
                                    break;
                                case Biome.Desert:
                                    index = DESERT;
                                    break;
                                case Biome.Volcano:
                                    index = VOLCANO;
                                    break;
                            }
                        }
                    }
                    else if(displayMode == "Height")
                    {
                        if(map[x, y].Water == WaterType.River)
                        {
                            index = RIVER;
                        }
                        else if(map[x, y].Water == WaterType.Volcano)
                        {
                            index = VOLCANO;
                        }
                        else if(h1 >= 0.2f && h1 < 0.21f)
                        {
                            index = SAND;
                        }
                        else if(h1 >= 0.21f && h1 < 0.4f)
                        {
                            index = PLAINS;
                        }
                        else if(h1 >= 0.4f && h1 < 0.6f)
                        {
                            index = HILLS;
                        }
                        else if(h1 >= 0.6f && h1 < 0.9f)
                        {
                            index = MOUNTAINS;
                        }
                        else
                        {
                            index = SNOWCAP;
                        }
                    }


                    if(displayMode == "Gray")
                    {
                        Color toDraw = JetGradient.GetColor(h1);
                        //Color toDraw = new Color(h1, 0.5f * h1, 1.0f - h1 * 0.5f);
                        worldData[y * width + x] = toDraw;
                    }
                    else
                    {
                        Color ci = colorIndex[index];
                        Color toDraw = new Color((float) (ci.R) * (h1 + 0.5f) / 255.0f, (float) (ci.G * (h1 + 0.5f)) / 255.0f, (float) (ci.B * (h1 + 0.5f)) / 255.0f);
                        worldData[ty * width + tx] = toDraw;
                    }
                }
            }

            if(imageMutex != null)
            {
                imageMutex.WaitOne();
            }

            worldMap.SetData<Color>(worldData);

            if(imageMutex != null)
            {
                imageMutex.ReleaseMutex();
            }
        }
    }

}