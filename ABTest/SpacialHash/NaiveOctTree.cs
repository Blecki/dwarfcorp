//#define USEBIN

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ABTest.Oct
{
    public class NaiveOctTreeNode<T>
    {
        public BoundingBox Bounds;
        public NaiveOctTreeNode<T>[] Children;
        public List<Tuple<T, Vector3>> Items = new List<Tuple<T, Vector3>>(8);
        public Vector3 Mid;

        public NaiveOctTreeNode(Vector3 Min, Vector3 Max)
        {
            this.Bounds = new BoundingBox(Min, Max);
            Mid = (Min + Max) / 2.0f;
        }

        private void Subdivide()
        {
            var Min = Bounds.Min;
            var Max = Bounds.Max;

            Children = new NaiveOctTreeNode<T>[8]
            {
                /*000*/ new NaiveOctTreeNode<T>(new Vector3(Min.X, Min.Y, Min.Z), new Vector3(Mid.X, Mid.Y, Mid.Z)),
                /*001*/ new NaiveOctTreeNode<T>(new Vector3(Mid.X, Min.Y, Min.Z), new Vector3(Max.X, Mid.Y, Mid.Z)),
                /*010*/ new NaiveOctTreeNode<T>(new Vector3(Min.X, Mid.Y, Min.Z), new Vector3(Mid.X, Max.Y, Mid.Z)),
                /*011*/ new NaiveOctTreeNode<T>(new Vector3(Mid.X, Mid.Y, Min.Z), new Vector3(Max.X, Max.Y, Mid.Z)),

                /*100*/ new NaiveOctTreeNode<T>(new Vector3(Min.X, Min.Y, Mid.Z), new Vector3(Mid.X, Mid.Y, Max.Z)),
                /*101*/ new NaiveOctTreeNode<T>(new Vector3(Mid.X, Min.Y, Mid.Z), new Vector3(Max.X, Mid.Y, Max.Z)),
                /*110*/ new NaiveOctTreeNode<T>(new Vector3(Min.X, Mid.Y, Mid.Z), new Vector3(Mid.X, Max.Y, Max.Z)),
                /*111*/ new NaiveOctTreeNode<T>(new Vector3(Mid.X, Mid.Y, Mid.Z), new Vector3(Max.X, Max.Y, Max.Z))
            };
        }

#if USEBIN
        private int Bin(Vector3 P)
        {
            var x = (P.X < Mid.X) ? 0 : 1;
            var y = (P.Y < Mid.Y) ? 0 : 2;
            var z = (P.Z < Mid.Z) ? 0 : 4;
            return x + y + z;
        }
#endif

        public void AddItem(T Item, Vector3 Point)
        {
            if (Bounds.Contains(Point) != ContainmentType.Disjoint)
                AddToTree(Tuple.Create(Item, Point), 8);
        }

        private void AddToTree(Tuple<T, Vector3> Item, int SubdivideThreshold)
        {
#if USEBIN == false
            // This is not needed for the Bin version as Bin keeps us in the right spot.
            if (Bounds.Contains(Item.Item2) == ContainmentType.Disjoint) return;
#endif
            if (Children != null)
            {
#if USEBIN
                Children[Bin(Item.Item2)].AddToTree(Item, SubdivideThreshold);
#else
                for (var i = 0; i < 8; ++i)
                    Children[i].AddToTree(Item, SubdivideThreshold);
#endif
            }
            else if (Items.Count == SubdivideThreshold && (Bounds.Max.X - Bounds.Min.X) > 4)
            {
                Subdivide();
#if USEBIN
                for (var i = 0; i < Items.Count; ++i)
                    Children[Bin(Items[i].Item2)].AddToTree(Items[i], SubdivideThreshold);
                Children[Bin(Item.Item2)].AddToTree(Item, SubdivideThreshold);

                for (var i = 0; i < 8; ++i)
                    Children[i].AddToTree(Item, SubdivideThreshold);
#else
                for (var i = 0; i < Items.Count; ++i)
                    for (var c = 0; c < 8; ++c)
                        Children[c].AddToTree(Items[i], SubdivideThreshold);
                for (var c = 0; c < 8; ++c)
                    Children[c].AddToTree(Item, SubdivideThreshold);
#endif
            }
            else
            {
                Items.Add(Item);
            }
        }

        public void FindItems(BoundingBox SearchBounds, HashSet<T> results)
        {
            ContainmentType c = SearchBounds.Contains(Bounds);
            if (c == ContainmentType.Disjoint) return;
            if (c == ContainmentType.Intersects)
                FindItemsInBox(SearchBounds, results, c);
            else
                AddAllItems(SearchBounds, results);
        }

        private void FindItemsInBox(BoundingBox SearchBounds, HashSet<T> results, ContainmentType cType)
        {
            if (Children == null)
            {
                for (var i = 0; i < Items.Count; ++i)
                    if (SearchBounds.Contains(Items[i].Item2) != ContainmentType.Disjoint)
                        results.Add(Items[i].Item1);
            }
            else
            {
                for (var i = 0; i < 8; ++i)
                {
                    ContainmentType c = SearchBounds.Contains(Children[i].Bounds);
                    if (c == ContainmentType.Disjoint) continue;
                    if (c == ContainmentType.Intersects)
                        Children[i].FindItemsInBox(SearchBounds, results, c);
                    else
                        Children[i].AddAllItems(SearchBounds, results);
                }
            }
        }

        private void AddAllItems(BoundingBox SearchBounds, HashSet<T> results)
        {
            if (Children == null)
            {
                for (var i = 0; i < Items.Count; ++i)
                {
                    results.Add(Items[i].Item1);
                }
            }
            else
            {
                for (var i = 0; i < 8; ++i)
                    Children[i].AddAllItems(SearchBounds, results);
            }
        }
    }
}
