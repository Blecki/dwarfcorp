using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace ABTest
{
    class SpacialHashTag
    {
        public int id;
        
        public SpacialHashTag(int id)
        {
            this.id = id;
        }

        public override int GetHashCode()
        {
            return id;
        }

        public Oct.NaiveOctTreeNode<SpacialHashTag> OwnerNode;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var iterations = 100000;
            var cubeSize = 128;
            var hash = new SpacialHash.SpatialHash<SpacialHashTag>();
            var octtree = new Oct.NaiveOctTreeNode<SpacialHashTag>(new Vector3(0, 0, 0),
                new Vector3(cubeSize, cubeSize, cubeSize));
            var random = new Random();
            var tag = new SpacialHashTag(0); // Use same tag to avoid measuring object creation time.

            Console.WriteLine("Running test: SpacialHash vs OctTree.");

            // Test insertion
            Console.WriteLine();
            Console.WriteLine("Adding {0} Random Items", iterations);            

            var shInsertTestStart = DateTime.Now;

            for (var i = 0; i < iterations; ++i)
                hash.AddItem(
                    new SpacialHash.Point3(random.Next(cubeSize), random.Next(cubeSize), random.Next(cubeSize)),
                    tag);

            var shInsertTestEnd = DateTime.Now;

            var otInsertTestStart = DateTime.Now;

            for (var i = 0; i < iterations; ++i)
                octtree.AddItem(
                    tag,
                    new Vector3(random.Next(cubeSize), random.Next(cubeSize), random.Next(cubeSize)));

            var otInsertTestEnd = DateTime.Now;

            Console.WriteLine("SH: {0}ms  OT: {1}ms",
                (shInsertTestEnd - shInsertTestStart).TotalMilliseconds,
                (otInsertTestEnd - otInsertTestStart).TotalMilliseconds);
            Console.WriteLine();

            // Test searching
            
            Console.WriteLine("Find items in random 10x10x5 cube {0} times", iterations);
            var container = new HashSet<SpacialHashTag>();

            var shFindTestStart = DateTime.Now;
            for (var i = 0; i < iterations; ++i)
            {
                var box = new SpacialHash.Point3(random.Next(cubeSize), random.Next(cubeSize), random.Next(cubeSize));
                var corner = new Microsoft.Xna.Framework.Vector3(box.X, box.Y, box.Z);
                hash.GetItemsInBox(new Microsoft.Xna.Framework.BoundingBox(
                    corner, corner + new Microsoft.Xna.Framework.Vector3(10, 10, 5)), container);
            }
            var shFindTestEnd = DateTime.Now;

            container = new HashSet<SpacialHashTag>();
            var otFindTestStart = DateTime.Now;
            for (var i = 0; i < iterations; ++i)
            {
                var box = new SpacialHash.Point3(random.Next(cubeSize), random.Next(cubeSize), random.Next(cubeSize));
                var corner = new Microsoft.Xna.Framework.Vector3(box.X, box.Y, box.Z);
                octtree.FindItemsInBox(new Oct.BoundingBox(
                    corner, corner + new Vector3(10, 10, 5)), container);
            }
            var otFindTestEnd = DateTime.Now;

            Console.WriteLine("SH: {0}ms  OT: {1}ms",
                (shFindTestEnd - shFindTestStart).TotalMilliseconds,
                (otFindTestEnd - otFindTestStart).TotalMilliseconds);
            Console.WriteLine();
            

            // Test using same data for each container
            Console.WriteLine("- Using Same Data -");
            Console.WriteLine();
            Console.WriteLine("-- Generating {0} random items.", iterations);

            var randomItems = new List<Tuple<SpacialHashTag, SpacialHash.Point3>>();
            for (var i = 0; i < iterations; ++i)
                randomItems.Add(Tuple.Create(new SpacialHashTag(i),
                    new SpacialHash.Point3(random.Next(cubeSize), random.Next(cubeSize), random.Next(cubeSize))));

            hash = new SpacialHash.SpatialHash<SpacialHashTag>();
            octtree = new Oct.NaiveOctTreeNode<SpacialHashTag>(
                new Vector3(0, 0, 0),
                new Vector3(cubeSize, cubeSize, cubeSize));

            // Test insertion
            Console.WriteLine("-- Adding generated items.");

            shInsertTestStart = DateTime.Now;

            foreach (var item in randomItems)
                hash.AddItem(item.Item2, item.Item1);

            shInsertTestEnd = DateTime.Now;

            otInsertTestStart = DateTime.Now;

            foreach (var item in randomItems)
                octtree.AddItem(item.Item1, item.Item2.AsVector3());

            otInsertTestEnd = DateTime.Now;

            Console.WriteLine("SH: {0}ms  OT: {1}ms",
                (shInsertTestEnd - shInsertTestStart).TotalMilliseconds,
                (otInsertTestEnd - otInsertTestStart).TotalMilliseconds);
            Console.WriteLine();

            // Test searching
            Console.WriteLine("-- Generating {0} random boxes.", iterations);
            var randomBoxes = new List<Microsoft.Xna.Framework.BoundingBox>();
            for (var i = 0; i < iterations; ++i)
            {
                var box = new SpacialHash.Point3(random.Next(cubeSize), random.Next(cubeSize), random.Next(cubeSize));
                var corner = new Microsoft.Xna.Framework.Vector3(box.X, box.Y, box.Z);
                randomBoxes.Add(new Microsoft.Xna.Framework.BoundingBox(
                    corner, corner + new Microsoft.Xna.Framework.Vector3(10, 10, 5)));
            }
            
            Console.WriteLine("-- Find items in generated boxes.");

            container = new HashSet<SpacialHashTag>();
            shFindTestStart = DateTime.Now;
            foreach (var box in randomBoxes)
                hash.GetItemsInBox(box, container);
            shFindTestEnd = DateTime.Now;

            container = new HashSet<SpacialHashTag>();
            otFindTestStart = DateTime.Now;
            foreach (var box in randomBoxes)
                octtree.FindItemsInBox(new Oct.BoundingBox(box), container);
            otFindTestEnd = DateTime.Now;

            Console.WriteLine("SH: {0}ms  OT: {1}ms",
                (shFindTestEnd - shFindTestStart).TotalMilliseconds,
                (otFindTestEnd - otFindTestStart).TotalMilliseconds);
            Console.WriteLine();
            
            /*Console.WriteLine("Consistency check: Same items found?");

            for (var i = 0; i < 1; ++i)
            {
                Console.WriteLine("Test " + i + ": {0},{3} {1},{4} {2},{5}",
                    randomBoxes[i].Min.X,
                    randomBoxes[i].Min.Y,
                    randomBoxes[i].Min.Z,
                    randomBoxes[i].Max.X,
                    randomBoxes[i].Max.Y,
                    randomBoxes[i].Max.Z);

                var hashContainer = new HashSet<SpacialHashTag>();
                hash.GetItemsInBox(randomBoxes[i], hashContainer);

                var octContainer = new HashSet<SpacialHashTag>();
                octtree.FindItemsInBox(new Oct.BoundingBox(randomBoxes[i]), octContainer);

                Console.WriteLine("SH: {0} items. OT: {1} items.", hashContainer.Count, octContainer.Count);

                foreach (var found in hashContainer)
                {
                    Console.Write("Item {0} - {1} {2} {3}", found.id,
                        randomItems[found.id].Item2.X,
                        randomItems[found.id].Item2.Y,
                        randomItems[found.id].Item2.Z);

                    if (octContainer.Contains(found))
                        Console.Write(" (FOUND BY OCT)");
                    else
                    {
                        octtree.AddItem(randomItems[found.id].Item1, randomItems[found.id].Item2.AsVector3());
                    }
                    
                    Console.WriteLine();
                }
            }*/

            Console.ReadLine();
        }
    }
}
