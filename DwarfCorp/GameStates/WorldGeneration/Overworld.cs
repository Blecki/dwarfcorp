using System;
using System.Collections.Generic;
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using Newtonsoft.Json.Schema;
using Math = System.Math;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// The overworld is a 2D map specifying biomes,
    /// temperature, terrain height, etc.  Chunks are generated
    /// from the overworld.
    /// </summary>
    public class Overworld
    {
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

        public static LibNoise.Perlin heightNoise = new LibNoise.Perlin()
        {
            Frequency = 0.7f,
            Lacunarity = 0.8f,
            NoiseQuality = NoiseQuality.Standard,
            OctaveCount = 4,
            Seed = MathFunctions.Random.Next(),
            Persistence = 0.2f
        };

        public List<Vector2> Volcanoes { get; set; }
        public OverworldCell[,] Map { get; set; }
        public string Name { get; set; }
        public List<Faction> NativeFactions { get; set; }
        public List<ColonyCell> ColonyCells;
        public static ColorGradient JetGradient = null;

        public Overworld(int Width, int Height)
        {
            Map = new OverworldCell[Width, Height];
            ColonyCells = ColonyCell.DeriveFromTexture("World\\colonies");
        }

        public static float LinearInterpolate(Vector2 position, OverworldCell[,] map, OverworldField fieldType)
        {
            float x = position.X;
            float y = position.Y;
            float x1 = (int) MathFunctions.Clamp((float) Math.Ceiling(x), 0, map.GetLength(0) - 2);
            float y1 = (int) MathFunctions.Clamp((float) Math.Ceiling(y), 0, map.GetLength(1) - 2);
            float x2 = (int) MathFunctions.Clamp((float) Math.Floor(x), 0, map.GetLength(0) - 2);
            float y2 = (int) MathFunctions.Clamp((float) Math.Floor(y), 0, map.GetLength(1) - 2);

            if(Math.Abs(x1 - x2) < 0.5f)
                x1 = x1 + 1;
            
            if(Math.Abs(y1 - y2) < 0.5f)
                y1 = y1 + 1;
         
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

        public static float[,] GenerateHeightMapLookup(int width, int height)
        {
            float[,] toReturn = new float[width, height];

            const float mountainWidth = 0.04f;
            const float continentSize = 0.03f;
            const float hillSize = 0.1f;
            const float smallNoiseSize = 0.15f;
            const float cliffHeight = 0.1f;
            const float invCliffHeight = 1.0f/cliffHeight;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float mountain = (float)Math.Pow(OverworldImageOperations.noise(heightNoise, x, y, 0, mountainWidth), 1);
                    float continent = OverworldImageOperations.noise(heightNoise, x, y, 10, continentSize);
                    float hill = OverworldImageOperations.noise(heightNoise, x, y, 20, hillSize) * 0.02f;
                    float smallnoise = OverworldImageOperations.noise(heightNoise, x, y, 100, smallNoiseSize) * 0.01f;
                    float cliffs = OverworldImageOperations.noise(heightNoise, x, y, 200, continentSize) + 0.5f;
                    float h = OverworldImageOperations.pow(OverworldImageOperations.clamp((continent * mountain) + hill, 0, 1), 1);
                    h += smallnoise;
                    h += 0.4f;
                    h = ((int)(h * invCliffHeight)) * cliffHeight;
                    toReturn[x, y] = h;
                }
            }

            return toReturn;
        }

        public static void GenerateHeightMapFromLookup(OverworldCell[,] Map, float[,] lookup, int width, int height, float globalScale, bool erode)
        {
            if (!erode)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Map[x, y].Height = OverworldImageOperations.clamp(lookup[x, y], 0, 1);
                    }
                }
            }
            else
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float h = lookup[x, y];
                        Vector2 vec = new Vector2(x, y);
                        h *= LinearInterpolate(vec, Map, OverworldField.Faults);
                        h += LinearInterpolate(vec, Map, OverworldField.Weathering);
                        h *= LinearInterpolate(vec, Map, OverworldField.Erosion);
                        Map[x, y].Height = OverworldImageOperations.clamp(h, 0, 1);
                    }
                }
            }
        }

        public static void GenerateSaveTexture(OverworldCell[,] map, Color[] worldData)
        {
            for (var x = 0; x < map.GetLength(0); ++x)
                for (var y = 0; y < map.GetLength(1); ++y)
                    worldData[(y * map.GetLength(0)) + x] = new Color(map[x, y].Height_, map[x, y].Faction, (byte)map[x, y].Biome, (byte)255);
        }

        public static void DecodeSaveTexture(
            OverworldCell[,] map,
            int width,
            int height,
            Color[] worldData)
        {
            for (var x = 0; x < width; ++x)
                for (var y = 0; y < height; ++y)
                {
                    var color = worldData[(y * width) + x];
                    map[x, y].Height_ = color.R;
                    map[x, y].Faction = color.G;
                    map[x, y].Biome = color.B;
                    map[x, y].Rainfall_ = (byte)(BiomeLibrary.GetBiome(map[x, y].Biome).Rain * 255);
                    map[x, y].Temperature = (float)(BiomeLibrary.GetBiome(map[x, y].Biome).Temp);
                }
        }

        public static void TextureFromHeightMap(string displayMode,
            OverworldCell[,] map,
            List<Faction> NativeFactions,
            OverworldField type,
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
                    var biome = map[x, y].Biome;
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

                        if (factionColor > 0 && factionColor <= NativeFactions.Count)
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
                        var ci = Color.Black;
                        if (displayMode == "Biomes" && index != "Water" && index != "Sea")
                        {
                            var _biome = BiomeLibrary.GetBiome(biome);
                            if (_biome != null)
                                ci = _biome.MapColor;
                        }
                        else
                            ci = HeightColors[index];

                        var toDraw = new Color((float) (ci.R) * (h1 + 0.5f) / 255.0f, (float) (ci.G * (h1 + 0.5f)) / 255.0f, (float) (ci.B * (h1 + 0.5f)) / 255.0f);
                        worldData[ty * width + tx] = toDraw;
                    }
                }
            }

            if(imageMutex != null)
                imageMutex.WaitOne();

            GameState.Game.GraphicsDevice.Textures[0] = null;

            if (worldMap.IsDisposed || worldMap.GraphicsDevice.IsDisposed)
                worldMap = new Texture2D(GameState.Game.GraphicsDevice, width, height);

            worldMap.SetData(worldData);

            if(imageMutex != null)
                imageMutex.ReleaseMutex();
        }

        public static Vector2 WorldToOverworld(Vector2 worldXZ, float scale, Vector2 origin)
        {
            return worldXZ / scale + origin;
        }

        public static Vector2 WorldToOverworld(Vector3 worldXYZ, float scale, Vector2 origin)
        {
            return WorldToOverworld(new Vector2(worldXYZ.X, worldXYZ.Z), scale, origin);
        }

        public static BiomeData GetBiomeAt(OverworldCell[,] Map, Vector3 worldPos, float scale, Vector2 origin)
        {
            DebugHelper.AssertNotNull(Map);
            Vector2 v = WorldToOverworld(worldPos, scale, origin);
            var biome = Map[(int)MathFunctions.Clamp(v.X, 0, Map.GetLength(0) - 1), (int)MathFunctions.Clamp(v.Y, 0, Map.GetLength(1) - 1)].Biome;
            return BiomeLibrary.GetBiome(biome);
        }

        public static float GetValueAt(OverworldCell[,] Map, Vector3 worldPos, OverworldField fieldType, float scale, Vector2 origin)
        {
            Vector2 v = WorldToOverworld(worldPos, scale, origin);
            return OverworldImageOperations.GetValue(Map, v, fieldType);
        }

    }

}
