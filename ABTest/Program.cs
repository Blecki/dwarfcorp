using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Diagnostics;

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
            var ioct = new IntegerOct.IntegerOctTreeNode<SpacialHashTag>(new SpacialHash.Point3(0, 0, 0),
                new SpacialHash.Point3(cubeSize, cubeSize, cubeSize));
            var random = new Random();
            var tag = new SpacialHashTag(0); // Use same tag to avoid measuring object creation time.

            Debug.WriteLine("Running test: SpacialHash vs OctTree.");
            Debug.WriteLine("- Using Same Data -");
            Debug.WriteLine("-- Generating {0} random items.", iterations);

            var randomItems = new List<Tuple<SpacialHashTag, SpacialHash.Point3>>();
            for (var i = 0; i < iterations; ++i)
                randomItems.Add(Tuple.Create(new SpacialHashTag(i),
                    new SpacialHash.Point3(random.Next(cubeSize), random.Next(cubeSize), random.Next(cubeSize))));

            hash = new SpacialHash.SpatialHash<SpacialHashTag>();
            octtree = new Oct.NaiveOctTreeNode<SpacialHashTag>(
                new Vector3(0, 0, 0),
                new Vector3(cubeSize, cubeSize, cubeSize));

            // Test insertion
            Debug.WriteLine("-- Adding generated items.");

            var shInsertTestStart = DateTime.Now;

            foreach (var item in randomItems)
                hash.AddItem(item.Item2, item.Item1);

            var shInsertTestEnd = DateTime.Now;

            var otInsertTestStart = DateTime.Now;

            foreach (var item in randomItems)
                octtree.AddItem(item.Item1, item.Item2.AsVector3());

            var otInsertTestEnd = DateTime.Now;

            var ioInsertTestStart = DateTime.Now;

            foreach (var item in randomItems)
                ioct.AddItem(item.Item1, item.Item2);

            var ioInsertTestEnd = DateTime.Now;

            Debug.WriteLine("SH: {0}ms  OT: {1}ms  IO: {2}ms",
                (shInsertTestEnd - shInsertTestStart).TotalMilliseconds,
                (otInsertTestEnd - otInsertTestStart).TotalMilliseconds,
                (ioInsertTestEnd - ioInsertTestStart).TotalMilliseconds);
            Debug.WriteLine("");

            // Test searching
            Debug.WriteLine("-- Generating {0} random boxes.", iterations);
            var randomBoxes = new List<Microsoft.Xna.Framework.BoundingBox>();
            for (var i = 0; i < iterations; ++i)
            {
                var box = new SpacialHash.Point3(random.Next(cubeSize), random.Next(cubeSize), random.Next(cubeSize));
                var corner = new Microsoft.Xna.Framework.Vector3(box.X, box.Y, box.Z);
                randomBoxes.Add(new Microsoft.Xna.Framework.BoundingBox(
                    corner, corner + new Microsoft.Xna.Framework.Vector3(10, 10, 5)));
            }
            
            Debug.WriteLine("-- Find items in generated boxes.");

            var container = new HashSet<SpacialHashTag>();
            var shFindTestStart = DateTime.Now;
            foreach (var box in randomBoxes)
                hash.GetItemsInBox(box, container);
            var shFindTestEnd = DateTime.Now;

            container = new HashSet<SpacialHashTag>();
            var otFindTestStart = DateTime.Now;
            foreach (var box in randomBoxes)
                octtree.FindItemsInBox(new Oct.BoundingBox(box), container);
            var otFindTestEnd = DateTime.Now;

            container = new HashSet<SpacialHashTag>();
            var ioFindTestStart = DateTime.Now;
            foreach (var box in randomBoxes)
                ioct.FindItemsInBox(new IntegerOct.BoundingBox(box), container);
            var ioFindTestEnd = DateTime.Now;

            Debug.WriteLine("SH: {0}ms  OT: {1}ms  IO: {2}ms",
                (shFindTestEnd - shFindTestStart).TotalMilliseconds,
                (otFindTestEnd - otFindTestStart).TotalMilliseconds,
                (ioFindTestEnd - ioFindTestStart).TotalMilliseconds);
            Debug.WriteLine("");


            Debug.WriteLine("Consistency check: Same items found?");

            for (var i = 0; i < 10; ++i)
            {
                /*Debug.WriteLine("Test " + i + ": {0},{3} {1},{4} {2},{5}",
                    randomBoxes[i].Min.X,
                    randomBoxes[i].Min.Y,
                    randomBoxes[i].Min.Z,
                    randomBoxes[i].Max.X,
                    randomBoxes[i].Max.Y,
                    randomBoxes[i].Max.Z);*/

                var hashContainer = new HashSet<SpacialHashTag>();
                hash.GetItemsInBox(randomBoxes[i], hashContainer);

                var octContainer = new HashSet<SpacialHashTag>();
                octtree.FindItemsInBox(new Oct.BoundingBox(randomBoxes[i]), octContainer);

                var ioContainer = new HashSet<SpacialHashTag>();
                ioct.FindItemsInBox(new IntegerOct.BoundingBox(randomBoxes[i]), ioContainer);

                Debug.WriteLine("SH: {0}  OT: {1}  IO: {2}", hashContainer.Count, octContainer.Count, ioContainer.Count);

            /*    foreach (var found in hashContainer)
                {
                    Debug.Write("Item {0} - {1} {2} {3}", found.id,
                        randomItems[found.id].Item2.X,
                        randomItems[found.id].Item2.Y,
                        randomItems[found.id].Item2.Z);

                    if (octContainer.Contains(found))
                        Debug.Write(" (FOUND BY OCT)");
                    else
                    {
                        octtree.AddItem(randomItems[found.id].Item1, randomItems[found.id].Item2.AsVector3());
                    }
                    
                    Debug.WriteLine();
                }*/
            }

            //Debug.ReadLine();
        }
    }
}
