// Extensions.cs
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
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A special static class which "extends" several different XNA objects.
    /// This is an obscure C# feature which allows you to add methods to existing objects.
    /// </summary>
    public static class Extensions
    {
        public static T SelectRandom<T>(this IEnumerable<T> list)
        {
            var enumerable = list as IList<T> ?? list.ToList();
            return enumerable.Count > 0 ? enumerable.ElementAt(MathFunctions.Random.Next(enumerable.Count())) : default(T);
        }

        public static T SelectRandom<T>(this IEnumerable<T> list, Random Random)
        {
            var enumerable = list as IList<T> ?? list.ToList();
            return enumerable.Count > 0 ? enumerable.ElementAt(Random.Next(enumerable.Count())) : default(T);
        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static BoundingBox Expand(this BoundingBox box, float amount)
        {
            BoundingBox expandedBoundingBox = box;
            expandedBoundingBox.Min -= new Vector3(amount, amount, amount);
            expandedBoundingBox.Max += new Vector3(amount, amount, amount);

            return expandedBoundingBox;
        }

        public static BoundingBox Offset(this BoundingBox box, Vector3 Amount)
        {
            return new BoundingBox(box.Min + Amount, box.Max + Amount);
        }

        public static BoundingBox Offset(this BoundingBox Box, float X, float Y, float Z)
        {
            return Box.Offset(new Vector3(X, Y, Z));
        }

        public static Vector3 Center(this BoundingBox box)
        {
            return (box.Max + box.Min) * 0.5f;
        }

        public static float Length2DSquared(this Vector3 vec)
        {
            return vec.X * vec.X + vec.Z * vec.Z;
        }


        public static float Length2D(this Vector3 vec)
        {
            return (float)Math.Sqrt(vec.X * vec.X + vec.Z * vec.Z);
        }

        public static Vector3 Extents(this BoundingBox box)
        {
            return (box.Max - box.Min) * 0.5f;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;

            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void ShuffleSeedRandom<T>(this IList<T> list, int count = -1)
        {
            int n = 0;
            if (count < 0)
                n = list.Count;
            else
                n = count;

            while (n > 1)
            {
                n--;
                int k = MathFunctions.Random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }


        /// <summary>
        /// Creates an ARGB hex string representation of the <see cref="Color"/> value.
        /// </summary>
        /// <param name="color">The <see cref="Color"/> value to parse.</param>
        /// <param name="includeHash">Determines whether to include the hash mark (#) character in the string.</param>
        /// <returns>A hex string representation of the specified <see cref="Color"/> value.</returns>
        public static string ToHex(this Color color, bool includeHash)
        {
            string[] argb = 
            {
                color.A.ToString("X2"),
                color.R.ToString("X2"),
                color.G.ToString("X2"),
                color.B.ToString("X2"),
            };
            return (includeHash ? "#" : string.Empty) + string.Join(string.Empty, argb);
        }

        /// <summary>
        /// Creates a <see cref="Color"/> value from an ARGB or RGB hex string.  The string may
        /// begin with or without the hash mark (#) character.
        /// </summary>
        /// <param name="hexString">The ARGB hex string to parse.</param>
        /// <returns>
        /// A <see cref="Color"/> value as defined by the ARGB or RGB hex string.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if the string is not a valid ARGB or RGB hex value.</exception>
        public static Color ToColor(this string hexString)
        {
            if (hexString.StartsWith("#"))
                hexString = hexString.Substring(1);
            uint hex = uint.Parse(hexString, global::System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            Color color = Color.White;
            if (hexString.Length == 8)
            {
                color.A = (byte)(hex >> 24);
                color.R = (byte)(hex >> 16);
                color.G = (byte)(hex >> 8);
                color.B = (byte)(hex);
            }
            else if (hexString.Length == 6)
            {
                color.R = (byte)(hex >> 16);
                color.G = (byte)(hex >> 8);
                color.B = (byte)(hex);
            }
            else
            {
                throw new InvalidOperationException("Invald hex representation of an ARGB or RGB color value.");
            }
            return color;
        }
    }
}
