using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace ABTest
{
#if DO_ABTEST
    class Program
    {
        static void Main(string[] args)
        {
            var random = new Random();

            int iterations = 10;
            int voxels = 65000;
            int voxelTypes = 10;

            long totalManaged = 0;
            long totalNative = 0;
            long totalNaive = 0;
            var managed = new SVT.Level_TOP(0);
            var native = new SparseVoxelTreePerformanceTest.Chunk(0);
            var naive = new int[16 * 64 * 16];

            Console.WriteLine("Testing performance of Managed / Native / Naive");
            for (var i = 0; i < iterations; ++i)
            {
                var managedResults = TestLevel(16, 64, 16, (c, v) => managed.SetVoxel(c, v), (c) => managed.GetVoxel(c));
                var nativeResults = TestLevel(16, 64, 16, (c, v) => native.SetVoxel(c.X, c.Y, c.Z, v), (c) => native.GetVoxel(c.X, c.Y, c.Z));
                var naiveResults = TestLevel(16, 64, 16, (c, v) =>
                   naive[(c.Y * 16 * 16) + (c.Z * 16) + c.X] = v, (c) =>
                   naive[(c.Y * 16 * 16) + (c.Z * 16) + c.X]);

                Console.WriteLine(String.Format("MC:{0} MT:{1} NC:{2} NT:{3} DIFF:{4:0.000} NAC:{5} NAT:{6} NADIFF:{7:0.000}",
                    managedResults.Item1,
                    managedResults.Item3,
                    nativeResults.Item1,
                    nativeResults.Item3,
                    (float)nativeResults.Item3 / (float)managedResults.Item3,
                    naiveResults.Item1,
                    naiveResults.Item3,
                    (float)nativeResults.Item3 / (float)naiveResults.Item3));

                totalManaged += managedResults.Item3;
                totalNative += nativeResults.Item3;
                totalNaive += naiveResults.Item3;
            }

            Console.WriteLine(String.Format("Overall Diff: {0} Naive Diff: {1}",
                (float)totalNative / (float)totalManaged,
                (float)totalNative / (float)totalNaive));

            Console.WriteLine(String.Format("Worst case memory comparison - Native: {0} Naive: {1}",
                native.GetMemoryUsage(), 4 * 16 * 64 * 16));


            long memoryUsed = 0;

            Console.WriteLine("Comparing memory usage using {0} voxels from pool of {1} voxel types", voxels, voxelTypes);
            for (var i = 0; i < iterations; ++i)
            {
                var nativeMemSizeTest = new SparseVoxelTreePerformanceTest.Chunk(0);
                for (var v = 0; v < voxels; ++v)
                    nativeMemSizeTest.SetVoxel((byte)random.Next(16), (byte)random.Next(45), (byte)random.Next(16), random.Next(voxelTypes) + 1);

                nativeMemSizeTest.Compact();
                var mem = nativeMemSizeTest.GetMemoryUsage();
                Console.WriteLine("M:{0} %:{1}",
                    mem,
                    (float)mem / (float)(16 * 64 * 16 * 4));

                memoryUsed += mem;
            }

            Console.WriteLine("Overall M:{0} %:{1}",
                memoryUsed,
                (float)memoryUsed / (float)(16 * 64 * 16 * 4 * 10));

            Console.ReadLine();
        }

        private static Tuple<int, int, long> TestLevel(int X, int Y, int Z, Action<SVT.MiniPoint3, int> Set, Func<SVT.MiniPoint3, int> Get)
        {
            var watch = Stopwatch.StartNew();

            var good = 0;
            var bad = 0;

            for (byte x = 0; x < X; ++x)
                for (byte y = 0; y < Y; ++y)
                    for (byte z = 0; z < Z; ++z)
                        Set(new SVT.MiniPoint3(x, y, z), CalcTestValue(x, y, z));

            for (byte x = 0; x < X; ++x)
                for (byte z = 0; z < Z; ++z) // Order here intentionally 'wrong'.
                    for (byte y = 0; y < Y; ++y)
                        if (Get(new SVT.MiniPoint3(x, y, z)) == CalcTestValue(x, y, z))
                            good += 1;
                        else
                        {
                            Console.WriteLine("Fail at " + x + ", " + y + ", " + z);

                            bad += 1;
                        }
            watch.Stop();

            return Tuple.Create(good, bad, watch.ElapsedTicks);
        }

        private static int CalcTestValue(byte x, byte y, byte z)
        {
            return (x << 16) + (y << 8) + z;
        }
    }
#else
    class Program
    {
        static void Main(string[] args)
        {
        }
    }


#endif
}
