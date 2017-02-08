using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using ABTest.SpacialHash;
using System.Diagnostics;

namespace ABTest.IntegerOct
{
    public class BoundingBox
    {
        public Point3 Min;
        public Point3 Max;

        public int Width { get { return Max.X - Min.X; } }
        public int Height { get { return Max.Y - Min.Y; } }
        public int Depth { get { return Max.Z - Min.Z; } }

        public BoundingBox(Point3 Min, Point3 Max)
        {
            this.Min = Min;
            this.Max = Max;
        }

        public BoundingBox(Microsoft.Xna.Framework.BoundingBox Box)
        {
            this.Min = new Point3((int)Math.Floor(Box.Min.X), (int)Math.Floor(Box.Min.Y), (int)Math.Floor(Box.Min.Z));
            this.Max = new Point3((int)Math.Ceiling(Box.Max.X), (int)Math.Ceiling(Box.Max.Y), (int)Math.Ceiling(Box.Max.Z));
        }

        /// <summary>
        /// Inclusive at both boundaries
        /// </summary>
        /// <param name="P"></param>
        /// <returns></returns>
        public bool Contains(Point3 P)
        {
            return P.X >= Min.X && P.X <= Max.X &&
                   P.Y >= Min.Y && P.Y <= Max.Y &&
                   P.Z >= Min.Z && P.Z <= Max.Z;
        }

        public bool Intersects(BoundingBox Other)
        {
            if (Min.X > Other.Max.X || Max.X < Other.Min.X) return false;
            if (Min.Y > Other.Max.Y || Max.Y < Other.Min.Y) return false;
            if (Min.Z > Other.Max.Z || Max.Z < Other.Min.Z) return false;
            return true;
        }
    }

    public class IntegerOctTreeNode<T>
    {
        public BoundingBox Bounds;
        public IntegerOctTreeNode<T>[] Children;
        public List<Tuple<T, Point3>> Items = new List<Tuple<T, Point3>>();
        private Point3 Mid;

        public IntegerOctTreeNode(Point3 Min, Point3 Max)
        {
            Bounds = new BoundingBox(Min, Max);
            Bounds.Max.X = Bounds.Min.X + NextPowerOfTwo(Bounds.Width);
            Bounds.Max.Y = Bounds.Min.Y + NextPowerOfTwo(Bounds.Height);
            Bounds.Max.Z = Bounds.Min.Z + NextPowerOfTwo(Bounds.Depth);

            Mid = new Point3(Min.X + Bounds.Width / 2, Min.Y + Bounds.Height / 2, Min.Z + Bounds.Depth / 2);

            Debug.Assert(NextPowerOfTwo(Mid.X - Min.X) == (Mid.X - Min.X));
        }

        private int NextPowerOfTwo(int N)
        {
            var r = 1;
            while (r < N)
                r <<= 1;
            return r;
        }

        private void Subdivide()
        {
            var Min = Bounds.Min;
            var Max = Bounds.Max;            

            Children = new IntegerOctTreeNode<T>[8]
            {
                /*000*/ new IntegerOctTreeNode<T>(new Point3(Min.X, Min.Y, Min.Z), new Point3(Mid.X, Mid.Y, Mid.Z)),
                /*001*/ new IntegerOctTreeNode<T>(new Point3(Mid.X, Min.Y, Min.Z), new Point3(Max.X, Mid.Y, Mid.Z)),
                /*010*/ new IntegerOctTreeNode<T>(new Point3(Min.X, Mid.Y, Min.Z), new Point3(Mid.X, Max.Y, Mid.Z)),
                /*011*/ new IntegerOctTreeNode<T>(new Point3(Mid.X, Mid.Y, Min.Z), new Point3(Max.X, Max.Y, Mid.Z)),

                /*100*/ new IntegerOctTreeNode<T>(new Point3(Min.X, Min.Y, Mid.Z), new Point3(Mid.X, Mid.Y, Max.Z)),
                /*101*/ new IntegerOctTreeNode<T>(new Point3(Mid.X, Min.Y, Mid.Z), new Point3(Max.X, Mid.Y, Max.Z)),
                /*110*/ new IntegerOctTreeNode<T>(new Point3(Min.X, Mid.Y, Mid.Z), new Point3(Mid.X, Max.Y, Max.Z)),
                /*111*/ new IntegerOctTreeNode<T>(new Point3(Mid.X, Mid.Y, Mid.Z), new Point3(Max.X, Max.Y, Max.Z))
            };
        }

        private int Bin(Point3 P)
        {
            var x = (P.X < Mid.X) ? 0 : 1;
            var y = (P.Y < Mid.Y) ? 0 : 2;
            var z = (P.Z < Mid.Z) ? 0 : 4;
            return x + y + z;
        }

        public void AddItem(T Item, Point3 Point)
        {
            AddToTree(Tuple.Create(Item, Point), 8);
        }

        private void AddToTree(Tuple<T, Point3> Item, int SubdivideThreshold)
        {
            if (!Bounds.Contains(Item.Item2)) return;

            if (Children != null)
            {
                Children[Bin(Item.Item2)].AddToTree(Item, SubdivideThreshold);
            }
            else if (Items.Count == SubdivideThreshold && Bounds.Width > 8)
            {
                Subdivide();
                for (var i = 0; i < Items.Count; ++i)
                    Children[Bin(Items[i].Item2)].AddToTree(Items[i], SubdivideThreshold);
                Items.Clear();
            }
            else
            {
                Items.Add(Item);
            }
        }

        public void FindItemsInBox(BoundingBox SearchBounds, HashSet<T> results)
        {
            if (SearchBounds.Intersects(Bounds))
            {
                if (Children == null)
                {
                    for (var i = 0; i < Items.Count; ++i)
                    {
                        var P = Items[i].Item2;
                        if (P.X >= SearchBounds.Min.X && P.X <= SearchBounds.Max.X &&
                            P.Y >= SearchBounds.Min.Y && P.Y <= SearchBounds.Max.Y &&
                            P.Z >= SearchBounds.Min.Z && P.Z <= SearchBounds.Max.Z)
                            results.Add(Items[i].Item1);
                    }
                }
                else
                {
                    for (var i = 0; i < 8; ++i)
                        Children[i].FindItemsInBox(SearchBounds, results);
                }
            }
        }
    }
}
