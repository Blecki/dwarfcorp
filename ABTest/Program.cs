using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace ABTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //var random = new Random();
            //var randomVoxels = 100;
            //var iterations = 10;

            //var naiveVoxelsStored = 16 * 64 * 16;
            //var sumSparseVoxelsStored = 0;

            //for (int i = 0; i < iterations; ++i)
            //{
            //    var sparseVoxels = new SparseVoxelTree(new DwarfCorp.Point3(0, 0, 0), new DwarfCorp.Point3(16, 64, 16), 0);

            //    for (int v = 0; v < randomVoxels; ++v)
            //    {
            //        var coord = new DwarfCorp.Point3(random.Next(16), random.Next(64), random.Next(16));
            //        sparseVoxels.SetVoxel(coord, v % 10);
            //    }

            //    var memoryUsage = sparseVoxels.CalculateMemoryUsage();
            //    Console.WriteLine(i.ToString() + " Sparse tree: " + memoryUsage.Item2 + " voxels stored with " + memoryUsage.Item1 + " bytes overhead.");
            //    Console.WriteLine(i.ToString() + " Sparse storage usage as % of naive: " + ((float)memoryUsage.Item2 / (float)naiveVoxelsStored));

            //    sumSparseVoxelsStored += memoryUsage.Item2;

            //}

            //Console.WriteLine("Total sparse voxels: " + sumSparseVoxelsStored + " - % " + ((float)sumSparseVoxelsStored / ((float)naiveVoxelsStored * iterations)));
            var top = new SVT.Level_TOP(0);
            top.SetVoxel(new SVT.MiniPoint3(0, 16, 0), 1);


            var l2 = new SVT.Level2(new SVT.MiniPoint3(0, 0, 0), 0);
            TestLevel(2, 2, 2, (c, v) => l2.SetVoxel(c, v), (c) => l2.GetVoxel(c));
            var l4 = new SVT.Level4(new SVT.MiniPoint3(0, 0, 0), 0);
            TestLevel(4, 4, 4, (c, v) => l4.SetVoxel(c, v), (c) => l4.GetVoxel(c));
            var l8 = new SVT.Level8(new SVT.MiniPoint3(0, 0, 0), 0);
            TestLevel(8, 8, 8, (c, v) => l8.SetVoxel(c, v), (c) => l8.GetVoxel(c));
            var l16 = new SVT.Level16(new SVT.MiniPoint3(0, 0, 0), 0);
            TestLevel(16, 16, 16, (c, v) => l16.SetVoxel(c, v), (c) => l16.GetVoxel(c));
            var lTOP = new SVT.Level_TOP(0);
            TestLevel(16, 64, 16, (c, v) => lTOP.SetVoxel(c, v), (c) => lTOP.GetVoxel(c));


            Console.ReadLine();
        }

        private static void TestLevel(int X, int Y, int Z, Action<SVT.MiniPoint3, int> Set, Func<SVT.MiniPoint3, int> Get)
        {
            Console.WriteLine("Testing Level " + Y);
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
                            bad += 1;

            Console.WriteLine("Good: " + good + " Bad: " + bad);
        }

        private static int CalcTestValue(byte x, byte y, byte z)
        {
            return (x << 16) + (y << 8) + z;
        }
    }
}
