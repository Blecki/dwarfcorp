using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DwarfCorp;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DwarfCorpXNATests
{
    [TestClass]
    public class TestOctree
    {
        public class TestObj
        {
            public TestObj(int v)
            {
                val = v;
            }
            int val;
        }

        public BoundingBox AddPoint(DwarfCorp.OctTreeNode<TestObj> octree, TestObj val, float x, float y, float z)
        {
            var box = new BoundingBox(new Vector3(x - 0.01f, y - 0.01f, z - 0.01f), new Vector3(x + 0.01f, y + 0.01f, z + 0.01f));
            octree.Add(val, box);
            return box;
        }

        [TestMethod]
        public void TestAdd()
        {
            DwarfCorp.OctTreeNode<TestObj> octree = new DwarfCorp.OctTreeNode<TestObj>(new Vector3(-40, -40, -40), new Vector3(40, 40, 40));
            var obj = new TestObj(0);
            AddPoint(octree, obj, -4, -4, -4);
            HashSet<TestObj> set = new HashSet<TestObj>();
            octree.EnumerateItems(new BoundingBox(new Vector3(-4, -4, -4), new Vector3(0, 0, 0)), set);
            Assert.AreEqual(set.Count, 1);
            foreach (var pt in set)
            {
                Assert.AreEqual(pt, obj);
            }
        }

        [TestMethod]
        public void TestAddRemove()
        {
            DwarfCorp.OctTreeNode<TestObj> octree = new DwarfCorp.OctTreeNode<TestObj>(new Vector3(-40, -40, -40), new Vector3(40, 40, 40));
            Assert.IsTrue(octree.IsLeaf());
            List<TestObj> objects = new List<TestObj>();
            List<BoundingBox> boxes = new List<BoundingBox>();
            int k = 0;
            for (float dx = -30; dx < 30; dx+=10)
            {
                for (float dy = -30; dy < 30; dy+=10)
                {
                    for (float dz = -30; dz < 30; dz+=10)
                    {
                        var obj = new TestObj(k++);
                        objects.Add(obj);
                        var box = AddPoint(octree, obj, dx, dy, dz);
                        boxes.Add(box);
                    }
                }
            }

            Assert.IsFalse(octree.IsLeaf());

            for (int i = 0; i < boxes.Count; i++)
            {
                HashSet<TestObj> set = new HashSet<TestObj>();
                octree.EnumerateItems(boxes[i], set);

                Assert.AreEqual(1, set.Count);
                foreach(var item in set)
                {
                    Assert.AreEqual(objects[i], item);
                }
                var numRemoved = octree.Remove(objects[i], boxes[i]);
                Assert.IsTrue(numRemoved > 0);
            }

            Assert.IsTrue(octree.IsLeaf());
        }
    }
}
