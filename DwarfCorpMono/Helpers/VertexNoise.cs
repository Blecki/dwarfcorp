using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    class ColorNoise
    {
        static Perlin VertexNoiseX = new Perlin(PlayState.random.Next());
        static Perlin VertexNoiseY = new Perlin(PlayState.random.Next());
        static Perlin VertexNoiseZ = new Perlin(PlayState.random.Next());
        static Perlin GlobalVertexNoiseX = new Perlin(PlayState.random.Next());
        static Perlin GlobalVertexNoiseY = new Perlin(PlayState.random.Next());
        static Perlin GlobalVertexNoiseZ = new Perlin(PlayState.random.Next());

        static float NoiseScale = 0.4f;
        static float NoiseMagnitude = 100.0f;
        static float GlobalNoiseScale = 0.003f;
        static float GlobalNoiseMagnitude = 10.0f;


        public static Color GetRandomLightness(Vector3 position)
        {
            float x = VertexNoiseX.Noise((float)position.X * NoiseScale, (float)position.Y * NoiseScale, (float)position.Z * NoiseScale) * NoiseMagnitude;
            float gx = GlobalVertexNoiseX.Noise((float)position.X * GlobalNoiseScale, (float)position.Y * GlobalNoiseScale, (float)position.Z * GlobalNoiseScale) * GlobalNoiseMagnitude;
            float rx = (float)(x + gx);
            return new Color(rx, rx, rx);
        }

        public static Color GetRandomColor(Vector3 position)
        {
            float x = VertexNoiseX.Noise((float)position.X * NoiseScale, (float)position.Y * NoiseScale, (float)position.Z * NoiseScale) * NoiseMagnitude;
            float gx = GlobalVertexNoiseX.Noise((float)position.X * GlobalNoiseScale, (float)position.Y * GlobalNoiseScale, (float)position.Z * GlobalNoiseScale) * GlobalNoiseMagnitude;
            float y = VertexNoiseY.Noise((float)position.X * NoiseScale, (float)position.Y * NoiseScale, (float)position.Z * NoiseScale) * NoiseMagnitude;
            float gy = GlobalVertexNoiseY.Noise((float)position.X * GlobalNoiseScale, (float)position.Y * GlobalNoiseScale, (float)position.Z * GlobalNoiseScale) * GlobalNoiseMagnitude;
            float z = VertexNoiseZ.Noise((float)position.X * NoiseScale, (float)position.Y * NoiseScale, (float)position.Z * NoiseScale) * NoiseMagnitude;
            float gz = GlobalVertexNoiseZ.Noise((float)position.X * GlobalNoiseScale, (float)position.Y * GlobalNoiseScale, (float)position.Z * GlobalNoiseScale) * GlobalNoiseMagnitude;
            float rx = (float)(x + gx);
            float ry = (float)(y + gy);
            float rz = (float)(z + gz);
            return new Color(rx, ry, rz);
        }

    }

    class VertexNoise
    {
        static Perlin VertexNoiseX = new Perlin(PlayState.random.Next());
        static Perlin VertexNoiseY = new Perlin(PlayState.random.Next());
        static Perlin VertexNoiseZ = new Perlin(PlayState.random.Next());
        static Perlin GlobalVertexNoiseX = new Perlin(PlayState.random.Next());
        static Perlin GlobalVertexNoiseY = new Perlin(PlayState.random.Next());
        static Perlin GlobalVertexNoiseZ = new Perlin(PlayState.random.Next());
        static float NoiseScale = 0.1f;
        static float NoiseMagnitude = 0.35f;
        static float GlobalNoiseScale = 0.01f;
        static float GlobalNoiseMagnitude = 0.1f;
        static int RepeatingTextureSize = 32;
        static Vector3[][][] RepeatingTexture;



        public static Vector3[][][] GenerateRepeatingTexture(float w, float h, float d)
        {
            Vector3[][][] toReturn = new Vector3[(int)w][][];

            float whd = w * h * d;
            for(int x = 0; x < w; x++)
            {
                toReturn[x] = new Vector3[(int)h][];
                for(int y = 0; y < h; y++)
                {
                    toReturn[x][y] = new Vector3[(int)d];
                    for(int z = 0; z < d; z++)
                    {
                        toReturn[x][ y][ z] = (
                                               GetRandomNoiseVector(x, y, z) * (w - x) * (h - y) * (d - z) +
                                               GetRandomNoiseVector(x - w, y, z) * (x) * (h - y) * (d - z) +
                                               GetRandomNoiseVector(x - w, y - h, z) * (x) * (y) * (d - z) +
                                               GetRandomNoiseVector(x, y - h, z) * (w - x) * (y) * (d - z) +
                                               GetRandomNoiseVector(x, y, z - d) * (w - x) * (h - y) * (d) +
                                               GetRandomNoiseVector(x - w, y, z - d) * (x) * (h - y) * (d) +
                                               GetRandomNoiseVector(x - w, y - h, z - d) * (x) * (y) * (d) +
                                               GetRandomNoiseVector(x, y - h, z - d) * (w - x) * (y) * (d)
                                        ) / (whd);

                    }
                }
            }

            return toReturn;
        }

        public static Vector3 GetNoiseVectorFromRepeatingTexture(Vector3 position)
        {
            if (RepeatingTexture == null)
            {
                RepeatingTexture = GenerateRepeatingTexture(RepeatingTextureSize, RepeatingTextureSize, RepeatingTextureSize);
            }
            float modX = Math.Abs(position.X) % RepeatingTextureSize;
            float modY = Math.Abs(position.Y) % RepeatingTextureSize;
            float modZ = Math.Abs(position.Z) % RepeatingTextureSize;

            return new Vector3(0, RepeatingTexture[(int)modX][ (int)modY][ (int)modZ].Y, 0);
             
        }


        public static Vector3 GetRandomNoiseVector(float x, float y, float z)
        {
            return GetRandomNoiseVector(new Vector3(x, y, z));
        }

        public static Vector3 GetRandomNoiseVector(Vector3 position)
        {
            float x = VertexNoiseX.Noise((float)position.X * NoiseScale, (float)position.Y * NoiseScale, (float)position.Z * NoiseScale) * NoiseMagnitude - NoiseMagnitude / 2.0f;
            float y = VertexNoiseY.Noise((float)position.X * NoiseScale, (float)position.Y * NoiseScale, (float)position.Z * NoiseScale) * NoiseMagnitude - NoiseMagnitude / 2.0f;
            float z = VertexNoiseZ.Noise((float)position.X * NoiseScale, (float)position.Y * NoiseScale, (float)position.Z * NoiseScale) * NoiseMagnitude - NoiseMagnitude / 2.0f;

            float gx = GlobalVertexNoiseX.Noise((float)position.X * GlobalNoiseScale, (float)position.Y * GlobalNoiseScale, (float)position.Z * GlobalNoiseScale) * GlobalNoiseMagnitude - GlobalNoiseMagnitude / 2.0f;
            float gy = GlobalVertexNoiseY.Noise((float)position.X * GlobalNoiseScale, (float)position.Y * GlobalNoiseScale, (float)position.Z * GlobalNoiseScale) * GlobalNoiseMagnitude - GlobalNoiseMagnitude / 2.0f;
            float gz = GlobalVertexNoiseZ.Noise((float)position.X * GlobalNoiseScale, (float)position.Y * GlobalNoiseScale, (float)position.Z * GlobalNoiseScale) * GlobalNoiseMagnitude - GlobalNoiseMagnitude / 2.0f;

            return new Vector3((float)x + (float)gx, (float)y + (float)gy, (float)z + (float)gz);
        }

        public static Vector3[] WarpPoints(Vector3[] points, Vector3 scale, Vector3 translation)
        {
            Vector3[] toReturn = new Vector3[points.Length];

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 scaled = new Vector3(points[i].X * scale.X + translation.X,  points[i].Y * scale.Y + translation.Y, points[i].Z * scale.Z + translation.Z);
                toReturn[i] = points[i] + GetNoiseVectorFromRepeatingTexture(scaled);
            }

            return toReturn;
        }
    }
}
