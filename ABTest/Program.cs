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
            var random = new Random();
            var randomPrisms = 10000;
            var iterations = 100000;
            var repeat = 10;
            var cubeSize = 128;
            var octtree = new DwarfCorp.OctTreeNode<int>(new Vector3(0, 0, 0), new Vector3(cubeSize, cubeSize, cubeSize));

            for (int i = 0; i < randomPrisms; ++i)
            {
                var min = new Vector3(random.Next(cubeSize), random.Next(cubeSize), random.Next(cubeSize));
                octtree.AddItem(i, new BoundingBox(min, min + Vector3.One));
            }

            var randomVolumes = new List<BoundingBox>();
            for (var i = 0; i < iterations; ++i)
            {
                var min = new Vector3(random.Next(cubeSize), random.Next(cubeSize), random.Next(cubeSize));
                randomVolumes.Add(new BoundingBox(min, min + new Vector3(random.Next(3, 20), random.Next(3, 20), random.Next(3, 20))));
            }

            var stopwatch = new Stopwatch();
            var enumeratorTotal = 0l;
            var hashTotal = 0l;
            var total = 0l;

            for (var i = 0; i < repeat; ++i)
            {
                stopwatch.Start();
                var totalHit = 0;
                foreach (var volume in randomVolumes)
                    totalHit += octtree.EnumerateItems(volume).Count();
                stopwatch.Stop();
                enumeratorTotal += stopwatch.ElapsedTicks;
                Console.WriteLine("HIT: " + totalHit + "  - Old speed " + stopwatch.ElapsedTicks);

                stopwatch.Reset();
                stopwatch.Start();
                totalHit = 0;
                foreach (var volume in randomVolumes)
                {
                    var hash = new HashSet<int>();
                    octtree.EnumerateItems(volume, hash);
                    foreach (var x in hash)
                        total += x;
                    totalHit += hash.Count;
                }
                stopwatch.Stop();
                hashTotal += stopwatch.ElapsedTicks;
                Console.WriteLine("HIT: " + totalHit + "  - New speed " + stopwatch.ElapsedTicks);
            }

            Console.WriteLine("ENUM: " + enumeratorTotal);
            Console.WriteLine("HASH: " + hashTotal);

            {
                var totalHit = 0;
                foreach (var volume in randomVolumes)
                    totalHit += octtree.EnumerateItems(volume).Distinct().Count();
                Console.WriteLine("HIT: " + totalHit);
            }

            Console.WriteLine(total);
            Console.WriteLine((float)hashTotal / (float)enumeratorTotal);


            Console.ReadLine();
        }
    }
}
