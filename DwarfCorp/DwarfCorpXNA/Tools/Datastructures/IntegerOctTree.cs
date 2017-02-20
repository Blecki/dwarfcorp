using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace DwarfCorp
{
    public class IntegerBoundingBox
    {
        public Point3 Min;
        public Point3 Max;

        public int Width { get { return Max.X - Min.X; } }
        public int Height { get { return Max.Y - Min.Y; } }
        public int Depth { get { return Max.Z - Min.Z; } }

        public IntegerBoundingBox(Point3 Min, Point3 Max)
        {
            this.Min = Min;
            this.Max = Max;
        }

        public IntegerBoundingBox(Microsoft.Xna.Framework.BoundingBox Box)
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

        public bool Intersects(IntegerBoundingBox Other)
        {
            if (Min.X > Other.Max.X || Max.X < Other.Min.X) return false;
            if (Min.Y > Other.Max.Y || Max.Y < Other.Min.Y) return false;
            if (Min.Z > Other.Max.Z || Max.Z < Other.Min.Z) return false;
            return true;
        }
    }

    // Todo: Change to take object bounding boxes, not points.
    public class IntegerOctTreeNode<T>
    {
        public IntegerBoundingBox Bounds;
        public IntegerOctTreeNode<T>[] Children;
        public List<Tuple<T, IntegerBoundingBox>> Items = new List<Tuple<T, IntegerBoundingBox>>();
        private Point3 Mid;

        public IntegerOctTreeNode(Vector3 Min, Vector3 Max) : this(new Point3((int)Math.Floor(Min.X), (int)Math.Floor(Min.Y), (int)Math.Floor(Min.Z)), new Point3((int)Math.Ceiling(Max.X), (int)Math.Ceiling(Max.Y), (int)Math.Ceiling(Max.Z)))
        {

        }

        public IntegerOctTreeNode(Point3 Min, Point3 Max)
        {
            Bounds = new IntegerBoundingBox(Min, Max);
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

        public void AddItem(T Item, IntegerBoundingBox Point)
        {
            AddToTree(Tuple.Create(Item, Point), 8);
        }

        private void AddToTree(Tuple<T, IntegerBoundingBox> Item, int SubdivideThreshold)
        {
            if (!Bounds.Intersects(Item.Item2)) return;

            if (Children != null)
            {
                for (var i = 0; i < 8; ++i)
                    Children[i].AddToTree(Item, SubdivideThreshold);
            }
            else if (Items.Count == SubdivideThreshold && Bounds.Width > 8)
            {
                Subdivide();
                for (var i = 0; i < Items.Count; ++i)
                    for (var c = 0; c < 8; ++c)
                        Children[c].AddToTree(Items[i], SubdivideThreshold);
                for (var c = 0; c < 8; ++c)
                    Children[c].AddToTree(Item, SubdivideThreshold);
                Items.Clear();
            }
            else
            {
                Items.Add(Item);
            }
        }

        public void RemoveItem(T Item, IntegerBoundingBox Point)
        {
            RemoveFromTree(Tuple.Create(Item, Point));
        }

        private void RemoveFromTree(Tuple<T, IntegerBoundingBox> Item)
        {
            if (!Bounds.Intersects(Item.Item2)) return;
            if (Children == null)
                Items.RemoveAll(t => Object.ReferenceEquals(t.Item1, Item.Item1));
            else
                foreach (var child in Children)
                    child.RemoveFromTree(Item);
        }

        public void FindItemsAt(Point3 Point, HashSet<T> Results)
        {
            if (Bounds.Contains(Point))
            {
                if (Children == null)
                {
                    for (var i = 0; i < Items.Count; ++i)
                        if (Items[i].Item2.Contains(Point))
                            Results.Add(Items[i].Item1);
                }
                else
                {
                    for (var i = 0; i < 8; ++i)
                        Children[i].FindItemsAt(Point, Results);
                }
            }
        }


        public void FindItemsInBox(IntegerBoundingBox SearchBounds, HashSet<T> results)
        {
            if (SearchBounds.Intersects(Bounds))
            {
                if (Children == null)
                {
                    for (var i = 0; i < Items.Count; ++i)
                        if (Items[i].Item2.Intersects(SearchBounds))
                            results.Add(Items[i].Item1);
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
