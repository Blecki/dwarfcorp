// VertexNoise.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    
    /// <summary>
    /// This is used to apply random perlin noise to the positions of vertices.
    /// Used to make fancy, wavy terrain that isn't exactly square.
    /// </summary>
    internal class VertexNoise
    {
        private static Perlin VertexNoiseX = new Perlin(World.Random.Next());
        private static Perlin VertexNoiseY = new Perlin(World.Random.Next());
        private static Perlin VertexNoiseZ = new Perlin(World.Random.Next());
        private static Perlin GlobalVertexNoiseX = new Perlin(World.Random.Next());
        private static Perlin GlobalVertexNoiseY = new Perlin(World.Random.Next());
        private static Perlin GlobalVertexNoiseZ = new Perlin(World.Random.Next());
        private static float NoiseScale = 0.1f;
        private static float NoiseMagnitude = 0.35f;
        private static float GlobalNoiseScale = 0.01f;
        private static float GlobalNoiseMagnitude = 0.1f;
        private static int RepeatingTextureSize = 32;
        private static Vector3[][][] RepeatingTexture;


        public static Vector3[][][] GenerateRepeatingTexture(float w, float h, float d)
        {
            Vector3[][][] toReturn = new Vector3[(int) w][][];

            float whd = w * h * d;
            for(int x = 0; x < w; x++)
            {
                toReturn[x] = new Vector3[(int) h][];
                for(int y = 0; y < h; y++)
                {
                    toReturn[x][y] = new Vector3[(int) d];
                    for(int z = 0; z < d; z++)
                    {
                        toReturn[x][y][z] = (
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
            if (MathFunctions.HasNan(position))
            {
                return Vector3.Zero;
            }
            if(RepeatingTexture == null)
            {
                RepeatingTexture = GenerateRepeatingTexture(RepeatingTextureSize, RepeatingTextureSize, RepeatingTextureSize);
            }
            float modX = Math.Abs(position.X) % RepeatingTextureSize;
            float modY = Math.Abs(position.Y) % RepeatingTextureSize;
            float modZ = Math.Abs(position.Z) % RepeatingTextureSize;

            return new Vector3(0, RepeatingTexture[(int) modX][(int) modY][(int) modZ].Y, 0);
        }


        public static Vector3 GetRandomNoiseVector(float x, float y, float z)
        {
            return GetRandomNoiseVector(new Vector3(x, y, z));
        }

        public static Vector3 GetRandomNoiseVector(Vector3 position)
        {
            float x = VertexNoiseX.Noise((float) position.X * NoiseScale, (float) position.Y * NoiseScale, (float) position.Z * NoiseScale) * NoiseMagnitude - NoiseMagnitude / 2.0f;
            float y = VertexNoiseY.Noise((float) position.X * NoiseScale, (float) position.Y * NoiseScale, (float) position.Z * NoiseScale) * NoiseMagnitude - NoiseMagnitude / 2.0f;
            float z = VertexNoiseZ.Noise((float) position.X * NoiseScale, (float) position.Y * NoiseScale, (float) position.Z * NoiseScale) * NoiseMagnitude - NoiseMagnitude / 2.0f;

            float gx = GlobalVertexNoiseX.Noise((float) position.X * GlobalNoiseScale, (float) position.Y * GlobalNoiseScale, (float) position.Z * GlobalNoiseScale) * GlobalNoiseMagnitude - GlobalNoiseMagnitude / 2.0f;
            float gy = GlobalVertexNoiseY.Noise((float) position.X * GlobalNoiseScale, (float) position.Y * GlobalNoiseScale, (float) position.Z * GlobalNoiseScale) * GlobalNoiseMagnitude - GlobalNoiseMagnitude / 2.0f;
            float gz = GlobalVertexNoiseZ.Noise((float) position.X * GlobalNoiseScale, (float) position.Y * GlobalNoiseScale, (float) position.Z * GlobalNoiseScale) * GlobalNoiseMagnitude - GlobalNoiseMagnitude / 2.0f;

            return new Vector3((float) x + (float) gx, (float) y + (float) gy, (float) z + (float) gz);
        }

        public static Vector3[] WarpPoints(Vector3[] points, Vector3 scale, Vector3 translation)
        {
            Vector3[] toReturn = new Vector3[points.Length];

            for(int i = 0; i < points.Length; i++)
            {
                Vector3 scaled = new Vector3(points[i].X * scale.X + translation.X, points[i].Y * scale.Y + translation.Y, points[i].Z * scale.Z + translation.Z);
                toReturn[i] = points[i] + GetNoiseVectorFromRepeatingTexture(scaled);
            }

            return toReturn;
        }
    }

}