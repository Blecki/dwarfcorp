// ThreadSafeRandom.cs
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

namespace DwarfCorp
{
    /// <summary>
    ///     C# does not have a thread safe Random generator. This one
    ///     tries to transparently wrap Random in such a way that the
    ///     same object produces valid random numbers no matter which
    ///     thread is calling it.
    ///     Adapted from this Stackoverflow thread:
    ///     http://stackoverflow.com/questions/3049467/is-c-sharp-random-number-generator-thread-safe
    /// </summary>
    public class ThreadSafeRandom
    {
        private static readonly Random seedGenerator = new Random();
        [ThreadStatic] private static Random generator;
        private readonly bool hasSeed;
        private readonly int lastSeed = -1;

        public ThreadSafeRandom()
        {
            CheckThreadGenerator();
        }


        public ThreadSafeRandom(int seed)
        {
            hasSeed = true;
            lastSeed = seed;
            if (generator == null)
            {
                generator = new Random(seed);
            }
        }

        public void CheckThreadGenerator()
        {
            if (generator == null)
            {
                int seed;

                if (!hasSeed)
                {
                    lock (seedGenerator)
                    {
                        seed = seedGenerator.Next();
                    }
                }
                else
                {
                    seed = lastSeed;
                }

                generator = new Random(seed);
            }
        }

        public int Next()
        {
            CheckThreadGenerator();
            return generator.Next();
        }

        public double NextDouble()
        {
            CheckThreadGenerator();
            return generator.NextDouble();
        }

        public int Next(int a, int b)
        {
            CheckThreadGenerator();
            return generator.Next(a, b);
        }

        public int Next(int max)
        {
            CheckThreadGenerator();
            return generator.Next(max);
        }

        // TODO: Wrap more functions.
    }
}