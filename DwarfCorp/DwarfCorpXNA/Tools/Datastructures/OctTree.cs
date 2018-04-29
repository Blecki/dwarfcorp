using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace DwarfCorp
{
    public class OctTreeNode<T>
    {
        private const float MinimumSize = 4.0f;
        private const int SubdivideThreshold = 8;

        public BoundingBox Bounds;
        public OctTreeNode<T>[] Children;
        public List<Tuple<T, BoundingBox>> Items = new List<Tuple<T, BoundingBox>>();
        private Vector3 Mid;
        private static object Lock = new object();
        public OctTreeNode(Vector3 Min, Vector3 Max)
        {
            Bounds = new BoundingBox(Min, Max);

            Mid = new Vector3((Min.X + Max.X) / 2,
                (Min.Y + Max.Y) / 2,
                (Min.Z + Max.Z) / 2);
        }
        
        private void Subdivide()
        {
            lock (Lock)
            {
                var Min = Bounds.Min;
                var Max = Bounds.Max;

                Children = new OctTreeNode<T>[8]
                {
                    /*000*/ new OctTreeNode<T>(new Vector3(Min.X, Min.Y, Min.Z), new Vector3(Mid.X, Mid.Y, Mid.Z)),
                    /*001*/ new OctTreeNode<T>(new Vector3(Mid.X, Min.Y, Min.Z), new Vector3(Max.X, Mid.Y, Mid.Z)),
                    /*010*/ new OctTreeNode<T>(new Vector3(Min.X, Mid.Y, Min.Z), new Vector3(Mid.X, Max.Y, Mid.Z)),
                    /*011*/ new OctTreeNode<T>(new Vector3(Mid.X, Mid.Y, Min.Z), new Vector3(Max.X, Max.Y, Mid.Z)),

                    /*100*/ new OctTreeNode<T>(new Vector3(Min.X, Min.Y, Mid.Z), new Vector3(Mid.X, Mid.Y, Max.Z)),
                    /*101*/ new OctTreeNode<T>(new Vector3(Mid.X, Min.Y, Mid.Z), new Vector3(Max.X, Mid.Y, Max.Z)),
                    /*110*/ new OctTreeNode<T>(new Vector3(Min.X, Mid.Y, Mid.Z), new Vector3(Mid.X, Max.Y, Max.Z)),
                    /*111*/ new OctTreeNode<T>(new Vector3(Mid.X, Mid.Y, Mid.Z), new Vector3(Max.X, Max.Y, Max.Z))
                };
            }
        }

        public void AddItem(T Item, BoundingBox Point)
        {
            AddToTree(Tuple.Create(Item, Point));
        }

        // Todo: Need version that returns highest level node that fully contains item.
        private void AddToTree(Tuple<T, BoundingBox> Item)
        {
            lock (Lock)
            {
                if (!Bounds.Intersects(Item.Item2)) return;

                if (Children != null)
                {
                    for (var i = 0; i < 8; ++i)
                        Children[i].AddToTree(Item);
                }
                else if (Items.Count == SubdivideThreshold && (Bounds.Max.X - Bounds.Min.X) > MinimumSize)
                {
                    Subdivide();
                    for (var i = 0; i < Items.Count; ++i)
                        for (var c = 0; c < 8; ++c)
                            Children[c].AddToTree(Items[i]);
                    for (var c = 0; c < 8; ++c)
                        Children[c].AddToTree(Item);
                    Items.Clear();
                }
                else
                {
                    Items.Add(Item);
                }
            }
        }

        public OctTreeNode<T> AddToTreeEx(Tuple<T, BoundingBox> Item)
        {
            lock (Lock)
            {
                var containment = Bounds.Contains(Item.Item2);
                if (containment == ContainmentType.Disjoint) return null;

                if (Children == null && Items.Count == SubdivideThreshold && (Bounds.Max.X - Bounds.Min.X) > MinimumSize)
                {
                    Subdivide();
                    for (var i = 0; i < Items.Count; ++i)
                        for (var c = 0; c < 8; ++c)
                            if (Children[c].AddToTreeEx(Items[i]) != null)
                                break;
                }

                if (Children != null)
                {
                    for (var i = 0; i < 8; ++i)
                    {
                        var cr = Children[i].AddToTreeEx(Item);
                        if (cr != null)
                            return cr;
                    }
                }
                else
                    Items.Add(Item);

                if (containment == ContainmentType.Contains)
                    return this;

                return null;
            }
        }

        public void RemoveItem(T Item, BoundingBox Box)
        {
            RemoveFromTree(Tuple.Create(Item, Box));
        }

        private int RemoveFromTree(Tuple<T, BoundingBox> Item)
        {
            lock (Lock)
            {
                if (!Bounds.Intersects(Item.Item2)) return 0;
                if (Children == null)
                {
                    Items.RemoveAll(t => Object.ReferenceEquals(t.Item1, Item.Item1));
                    return Items.Count;
                }
                else
                {
                    var r = 0;
                    for (int i = 0; i < 8; ++i)
                        r += Children[i].RemoveFromTree(Item);

                    //if (r == 0)
                    //    Children = null;

                    return r;
                }
            }
        }

        public IEnumerable<T> EnumerateItems(BoundingBox SearchBounds)
        {
            lock (Lock)
            {
                if (SearchBounds.Intersects(Bounds))
                {
                    if (Children == null)
                    {
                        for (var i = 0; i < Items.Count; ++i)
                            if (Items[i].Item2.Intersects(SearchBounds))
                                yield return Items[i].Item1;
                    }
                    else
                    {
                        for (var i = 0; i < 8; ++i)
                            foreach (var item in Children[i].EnumerateItems(SearchBounds))
                                yield return item;
                    }
                }   
            }
        }

        public void EnumerateItems(BoundingBox SearchBounds, HashSet<T> Into)
        {
            lock (Lock)
            {
                if (SearchBounds.Intersects(Bounds))
                {
                    if (Children == null)
                    {
                        for (var i = 0; i < Items.Count; ++i)
                            if (Items[i].Item2.Intersects(SearchBounds))
                                Into.Add(Items[i].Item1);
                    }
                    else
                    {
                        for (var i = 0; i < 8; ++i)
                            Children[i].EnumerateItems(SearchBounds, Into);
                    }
                }
            }
        }

        public void EnumerateItems(BoundingBox SearchBounds, HashSet<T> Into, Func<T, bool> Filter)
        {
            lock (Lock)
            {
                if (SearchBounds.Intersects(Bounds))
                {
                    if (Children == null)
                    {
                        for (var i = 0; i < Items.Count; ++i)
                            if (Items[i].Item2.Intersects(SearchBounds) && Filter(Items[i].Item1))
                                Into.Add(Items[i].Item1);
                    }
                    else
                    {
                        for (var i = 0; i < 8; ++i)
                            Children[i].EnumerateItems(SearchBounds, Into, Filter);
                    }
                }
            }
        }

        public void EnumerateItems(BoundingFrustum SearchBounds, HashSet<T> Into)
        {
            lock (Lock)
            {
                if (SearchBounds.Intersects(Bounds))
                {
                    if (Children == null)
                    {
                        for (var i = 0; i < Items.Count; ++i)
                            if (Items[i].Item2.Intersects(SearchBounds))
                                Into.Add(Items[i].Item1);
                    }
                    else
                    {
                        for (var i = 0; i < 8; ++i)
                            Children[i].EnumerateItems(SearchBounds, Into);
                    }
                }
            }
        }

        public void EnumerateAll(HashSet<T> Into)
        {
            lock (Lock)
            {
                if (Children == null)
                {
                    for (var i = 0; i < Items.Count; ++i)
                        Into.Add(Items[i].Item1);
                }
                else
                {
                    for (var i = 0; i < 8; ++i)
                        Children[i].EnumerateAll(Into);
                }
            }
        }

        public IEnumerable<T> EnumerateItems(BoundingFrustum SearchBounds)
        {
            lock (Lock)
            {
                if (!SearchBounds.Intersects(Bounds)) yield break;
                if (Children == null)
                {
                    foreach (var t in Items.Where(t => t.Item2.Intersects(SearchBounds)))
                        yield return t.Item1;
                }
                else
                {
                    for (var i = 0; i < 8; ++i)
                        foreach (var item in Children[i].EnumerateItems(SearchBounds))
                            yield return item;
                }
            }
        }

        public void EnumerateBounds(int Depth, Action<BoundingBox, int> Callback)
        {
            Callback(Bounds, Depth);
            if (Children != null)
                for (var i = 0; i < 8; ++i)
                    Children[i].EnumerateBounds(Depth + 1, Callback);
        }
    }
}
