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
    // Todo: Why is this static?
    /// <summary>
    /// The overworld is a 2D map specifying biomes,
    /// temperature, terrain height, etc.  Chunks are generated
    /// from the overworld.
    /// </summary>
    public static partial class OverworldImageOperations
    {
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

        public static void MinBlend(OverworldCell[,] heightMap, Vector2 pos, float height, OverworldField type)
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

        public static float GetValue(OverworldCell[,] map, Vector2 pos, OverworldField value)
        {
            DebugHelper.AssertNotNull(map);
            int x = Math.Max(Math.Min((int) pos.X, map.GetLength(0) - 1), 0);
            int y = Math.Max(Math.Min((int) pos.Y, map.GetLength(1) - 1), 0);

            return map[x, y].GetValue(value);
        }

        public static void AddValue(OverworldCell[,] map, Vector2 pos, OverworldField value, float amount)
        {
            DebugHelper.AssertNotNull(map);
            int x = Math.Max(Math.Min((int) pos.X, map.GetLength(0) - 1), 0);
            int y = Math.Max(Math.Min((int) pos.Y, map.GetLength(1) - 1), 0);

            map[x, y].SetValue(value, map[x, y].GetValue(value) + amount);
        }

        public static void MultValue(OverworldCell[,] heightMap, Vector2 pos, OverworldField value, float height)
        {
            DebugHelper.AssertNotNull(heightMap);
            int x = Math.Max(Math.Min((int) pos.X, heightMap.GetLength(0) - 1), 0);
            int y = Math.Max(Math.Min((int) pos.Y, heightMap.GetLength(1) - 1), 0);
            float c = heightMap[x, y].GetValue(value);
            heightMap[x, y].SetValue(value, c * height);
        }

        public static float noise(LibNoise.Perlin heightNoise, float x, float y, float z, float s)
        {
            return (float) heightNoise.GetValue(x*s, y*s, z*s);
        }

        public static float clamp(float x, float min, float max)
        {
            return Math.Max(Math.Min(x, max), min);
        }

        public static float pow(float x, float y)
        {
            return (float) Math.Pow(x, y);
        }

        public static float abs(float x)
        {
            return (float) Math.Abs(x);
        }

        public static float sqrt(float x)
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

        public static void Blur(OverworldCell[,] array, int width, int height, OverworldField type)
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

        private static Vector2[] deltas2d =
        {
            new Vector2(-1, 0),
            new Vector2(1, 0),
            new Vector2(0, -1),
            new Vector2(0, 1)
        };

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

        private static readonly Perlin XDistort = new Perlin(MathFunctions.Random.Next());
        private static readonly Perlin YDistort = new Perlin(MathFunctions.Random.Next());

        public static void Distort(OverworldCell[,] Map, int width, int height, float distortAmount, float distortScale, OverworldField fieldType)
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
    }
}
