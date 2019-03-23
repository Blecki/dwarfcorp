// Datastructures.cs
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
using System.Threading;
using System.Collections.Concurrent;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    static class DebugHelper
    {
        public static void AssertNotNull<T>(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(String.Format("{0} is null.", typeof(T).Name));
            }
        }
    }

    /// <summary>
    /// Honestly, this is just a helper class where a bunch of other miscellanious
    /// stuff is thrown at this time. Most of it has to do with utilities for certain
    /// data structures (such as 2D or 3D arrays).
    /// </summary>
    internal class Datastructures
    {

        public static Vector2 SafeMeasure(SpriteFont font, string text)
        {
            Vector2 extents = Vector2.One;

            if(text == null)
            {
                return extents;
            }

            try
            {
                if (text.Contains("[color:"))
                {
                    // how far in x to offset from position
                    int currentOffset = 0;
                    int maxY = 0;
                    string[] splits = text.Split(new string[] { "[color:" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var str in splits)
                    {
                        // if this section starts with a color
                        if (str.StartsWith("#"))
                        {
                            // any subsequent msgs after the [/color] tag are defaultColor
                            string[] msgs = str.Substring(10).Split(new string[] { "[/color]" }, StringSplitOptions.RemoveEmptyEntries);
                            Vector2 measure = font.MeasureString(msgs[0]);
                            //
                            currentOffset += (int)measure.X;
                            maxY = Math.Max((int)measure.Y, maxY);
                            // there should only ever be one other string or none
                            if (msgs.Length == 2)
                            {
                                Vector2 measure1 = font.MeasureString(msgs[1]);
                                currentOffset += (int)measure1.X;
                                maxY = Math.Max((int)measure1.Y, maxY);
                            }
                        }
                        else
                        {
                            Vector2 measure2 = font.MeasureString(str);
                            currentOffset += (int)measure2.X;
                            maxY = Math.Max((int)measure2.Y, maxY);
                        }
                    }

                    extents.X = currentOffset;
                    extents.Y = maxY;
                }
                else
                {
                    extents = font.MeasureString(text);
                }
            }
            catch(ArgumentException e)
            {
                Console.Error.WriteLine(e.Message);
                extents.X = text.Length * 20;
            }

            return extents;
        }

        public static EventWaitHandle WaitFor(EventWaitHandle[] waitHandles)
        {
            int iHandle = WaitHandle.WaitAny(waitHandles, 500);

            if (iHandle == System.Threading.WaitHandle.WaitTimeout)
            {
                return null;
            }

            if (iHandle > 0 && iHandle < waitHandles.Length)
            {
                EventWaitHandle wh = (EventWaitHandle) waitHandles[iHandle];

                return wh;   
            }

            return null;
        }

        // Shuffle<T> does the same thing, just with a IList<T> as argument, and a new Random each time.

        public static List<int> RandomIndices(int max)
        {    
            List<int> toReturn = new List<int>(max);
            toReturn.AddRange(Enumerable.Range(0, max));
            
            while (max > 1)
            {
                max--;
                int r = MathFunctions.Random.Next(max + 1);
                int val = toReturn[r];
                toReturn[r] = toReturn[max];
                toReturn[max] = val;
            }

            return toReturn;
        }

        public static IEnumerable<TKey> RandomKeys<TKey, TValue>(IDictionary<TKey, TValue> dict)
        {
            Random rand = new Random();
            List<TKey> values = Enumerable.ToList(dict.Keys);


            int size = dict.Count;

            if(size > 0)
            {
                while(true)
                {
                    yield return values[rand.Next(size)];
                }
            }
        }

        public static T SelectRandom<T>(IEnumerable<T> list)
        {
            var enumerable = list as IList<T> ?? list.ToList();
            return enumerable.Count > 0 ? enumerable.ElementAt(MathFunctions.Random.Next(enumerable.Count())) : default(T);
        }

        public static IEnumerable<T> SelectRandom<T>(IEnumerable<T> list, int num)
        {
            var enumerable = new List<T>();
            enumerable.AddRange(list);
            enumerable.Shuffle();
            for (int i = 0; i < Math.Min(num, enumerable.Count); i++)
            {
                yield return enumerable[i];
            }
        }

        public static T[,] RotateClockwise<T>(T[,] A)
        {
            int nr = A.GetLength(0);
            int nc = A.GetLength(1);

            T[,] toReturn = new T[nc, nr];

            for(int r = 0; r < nc; r++)
            {
                for(int c = 0; c < nr; c++)
                {
                    toReturn[r, c] = A[c, r];
                }
            }

            for(int r = 0; r < nc; r++)
            {
                for(int c = 0; c < nr / 2; c++)
                {
                    Swap(ref toReturn[r, c], ref toReturn[r, nr - c - 1]);
                }
            }

            return toReturn;
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }
    }

    /// <summary>
    /// Class is a pair of type T. The pair is commutative, so Pair(A, B) == Pair(B, A)
    /// </summary>
    /// <typeparam name="T">The type of the pair.</typeparam>
    [JsonObject(IsReference = false)]
    public class Pair<T>
    {
        public override int GetHashCode()
        {
            unchecked
            {
                // this is a bad hashcode, but its necessary because Hash(A, B) must equal Hash(B, A)
                return EqualityComparer<T>.Default.GetHashCode(First) + EqualityComparer<T>.Default.GetHashCode(Second);
            }
        }

        public T First { get; set; }
        public T Second { get; set; }

        public Pair()
        {
            
        }

        public Pair(T first, T second)
        {
            First = first;
            Second = second;
        }

        public Pair(Pair<T> other) :
            this(other.First, other.Second)
        {
            
        }

        public bool IsSelfPair()
        {
            return First.Equals(Second);
        }

        public bool Contains(T obj)
        {
            return First.Equals(obj) || Second.Equals(obj);
        }

        public override bool Equals(object obj)
        {
            return obj is Pair<T> && Equals((Pair<T>) (obj));
        }

        public bool Equals(Pair<T> other)
        {
            return (other.First.Equals(First) && other.Second.Equals(Second)) ||
                   (other.First.Equals(Second) && other.Second.Equals(First));
        }
    }

}