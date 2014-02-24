using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    // C# does not have a thread safe Random generator. This one
    // tries to transparently wrap Random in such a way that the
    // same object produces valid random numbers no matter which
    // thread is calling it.
    // Adapted from this Stackoverflow thread:
    // http://stackoverflow.com/questions/3049467/is-c-sharp-random-number-generator-thread-safe
    public class ThreadSafeRandom
    {
        private static readonly Random seedGenerator = new Random();
        private bool hasSeed = false;
        private int lastSeed = -1;

        [ThreadStatic] private static Random generator;

        // If we've entered a new thread, it's possible that
        // this RNG needs to be re-created.
        // Before each random sample we need to call this.
        // Additionally, we need to generate *random* seeds.
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
