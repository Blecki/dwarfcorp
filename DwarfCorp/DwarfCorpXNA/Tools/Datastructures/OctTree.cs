using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace DwarfCorp
{
    public class OctTreeNode
    {
        private const float MinimumSize = 4.0f;
        private const int SubdivideThreshold = 8;

        private class Entry
        {
            public Body Body;
            public BoundingBox BoundingBox;

            public Entry(Body Body, BoundingBox BoundingBox)
            {
                this.Body = Body;
                this.BoundingBox = BoundingBox;
            }
        }

        private BoundingBox Bounds;
        private OctTreeNode[] Children;
        private List<Entry> Items = new List<Entry>();
        private Vector3 Mid;

        public OctTreeNode(Vector3 Min, Vector3 Max)
        {
            Bounds = new BoundingBox(Min, Max);

            Mid = new Vector3((Min.X + Max.X) / 2,
                (Min.Y + Max.Y) / 2,
                (Min.Z + Max.Z) / 2);
        }

        public ContainmentType Contains(BoundingBox Box)
        {
            return Bounds.Contains(Box);
        }
        
        private void Subdivide()
        {
            //lock (Lock) //All calls are from inside already locked functions.
            {
                var Min = Bounds.Min;
                var Max = Bounds.Max;

                Children = new OctTreeNode[8]
                {
                    /*000*/ new OctTreeNode(new Vector3(Min.X, Min.Y, Min.Z), new Vector3(Mid.X, Mid.Y, Mid.Z)),
                    /*001*/ new OctTreeNode(new Vector3(Mid.X, Min.Y, Min.Z), new Vector3(Max.X, Mid.Y, Mid.Z)),
                    /*010*/ new OctTreeNode(new Vector3(Min.X, Mid.Y, Min.Z), new Vector3(Mid.X, Max.Y, Mid.Z)),
                    /*011*/ new OctTreeNode(new Vector3(Mid.X, Mid.Y, Min.Z), new Vector3(Max.X, Max.Y, Mid.Z)),

                    /*100*/ new OctTreeNode(new Vector3(Min.X, Min.Y, Mid.Z), new Vector3(Mid.X, Mid.Y, Max.Z)),
                    /*101*/ new OctTreeNode(new Vector3(Mid.X, Min.Y, Mid.Z), new Vector3(Max.X, Mid.Y, Max.Z)),
                    /*110*/ new OctTreeNode(new Vector3(Min.X, Mid.Y, Mid.Z), new Vector3(Mid.X, Max.Y, Max.Z)),
                    /*111*/ new OctTreeNode(new Vector3(Mid.X, Mid.Y, Mid.Z), new Vector3(Max.X, Max.Y, Max.Z))
                };
            }
        }

        public OctTreeNode Add(Body Body, BoundingBox BoundingBox)
        {
            return _Add(new Entry(Body, BoundingBox));
        }

        private OctTreeNode _Add(Entry Item)
        {
            lock (this)
            {
                var containment = Bounds.Contains(Item.BoundingBox);
                if (containment == ContainmentType.Disjoint) return null;

                if (Children == null && Items.Count == SubdivideThreshold && (Bounds.Max.X - Bounds.Min.X) > MinimumSize)
                {
                    Subdivide();
                    for (var i = 0; i < Items.Count; ++i)
                        for (var c = 0; c < 8; ++c)
                            if (Children[c]._Add(Items[i]) != null)
                                break;
                }

                if (Children != null)
                {
                    for (var i = 0; i < 8; ++i)
                    {
                        var cr = Children[i]._Add(Item);
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

        public void Remove(Body Body, BoundingBox BoundingBox)
        {
            lock (this)
            {
                if (!Bounds.Intersects(BoundingBox)) return;
                if (Children == null)
                {
                    Items.RemoveAll(t => Object.ReferenceEquals(t.Body, Body));
                }
                else
                {
                    for (int i = 0; i < 8; ++i)
                        Children[i].Remove(Body, BoundingBox);
                }
            }
        }
        
        public void EnumerateItems(HashSet<Body> Into)
        {
            lock (this)
            {
                if (Children == null)
                    for (var i = 0; i < Items.Count; ++i)
                        Into.Add(Items[i].Body);
                else
                    for (var i = 0; i < 8; ++i)
                        Children[i].EnumerateItems(Into);
            }
        }

        public void EnumerateItems(BoundingBox SearchBounds, HashSet<Body> Into)
        {
            lock (this)
            {
                switch (SearchBounds.Contains(Bounds))
                {
                    case ContainmentType.Disjoint:
                        return;
                    case ContainmentType.Intersects:
                        if (Children == null)
                        {
                            for (var i = 0; i < Items.Count; ++i)
                                if (Items[i].BoundingBox.Intersects(SearchBounds))
                                    Into.Add(Items[i].Body);
                        }
                        else
                        {
                            for (var i = 0; i < 8; ++i)
                                Children[i].EnumerateItems(SearchBounds, Into);
                        }
                        break;
                    case ContainmentType.Contains:
                        if (Children == null)
                            for (var i = 0; i < Items.Count; ++i)
                                Into.Add(Items[i].Body);
                        else
                            for (var i = 0; i < 8; ++i)
                                Children[i].EnumerateItems(Into);
                        break;
                }
            }
        }

        public void EnumerateItems(HashSet<Body> Into, Func<Body, bool> Filter)
        {
            lock (this)
            {
                if (Children == null)
                {
                    for (var i = 0; i < Items.Count; ++i)
                        if (Filter(Items[i].Body))
                            Into.Add(Items[i].Body);
                }
                else
                    for (var i = 0; i < 8; ++i)
                        Children[i].EnumerateItems(Into, Filter);
            }
        }

        public void EnumerateItems(BoundingBox SearchBounds, HashSet<Body> Into, Func<Body, bool> Filter)
        {
            lock (this)
            {
                switch (SearchBounds.Contains(Bounds))
                {
                    case ContainmentType.Disjoint:
                        return;
                    case ContainmentType.Intersects:
                        if (Children == null)
                        {
                            for (var i = 0; i < Items.Count; ++i)
                                if (Items[i].BoundingBox.Intersects(SearchBounds) && Filter(Items[i].Body))
                                    Into.Add(Items[i].Body);
                        }
                        else
                        {
                            for (var i = 0; i < 8; ++i)
                                Children[i].EnumerateItems(SearchBounds, Into, Filter);
                        }
                        break;
                    case ContainmentType.Contains:
                        if (Children == null)
                        {
                            for (var i = 0; i < Items.Count; ++i)
                                if (Filter(Items[i].Body))
                                    Into.Add(Items[i].Body);
                        }
                        else
                            for (var i = 0; i < 8; ++i)
                                Children[i].EnumerateItems(Into, Filter);
                        break;
                }
            }
        }

        public void EnumerateItems(BoundingFrustum SearchBounds, HashSet<Body> Into)
        {
            lock (this)
            {
                switch (SearchBounds.Contains(Bounds))
                {
                    case ContainmentType.Disjoint:
                        return;
                    case ContainmentType.Intersects:
                        if (Children == null)
                        {
                            for (var i = 0; i < Items.Count; ++i)
                                if (Items[i].BoundingBox.Intersects(SearchBounds))
                                    Into.Add(Items[i].Body);
                        }
                        else
                        {
                            for (var i = 0; i < 8; ++i)
                                Children[i].EnumerateItems(SearchBounds, Into);
                        }
                        break;
                    case ContainmentType.Contains:
                        if (Children == null)
                            for (var i = 0; i < Items.Count; ++i)
                                    Into.Add(Items[i].Body);
                        else
                            for (var i = 0; i < 8; ++i)
                                Children[i].EnumerateItems(Into);
                        break;
                }
            }
        }

        public void EnumerateItems(BoundingFrustum SearchBounds, HashSet<Body> Into, Func<Body, bool> Filter)
        {
            lock (this)
            {
                switch (SearchBounds.Contains(Bounds))
                {
                    case ContainmentType.Disjoint:
                        return;
                    case ContainmentType.Intersects:
                        if (Children == null)
                        {
                            for (var i = 0; i < Items.Count; ++i)
                            {
                                if (Filter(Items[i].Body) && FastIntersects(SearchBounds, Items[i].BoundingBox))
                                    Into.Add(Items[i].Body);
                            }
                        }
                        else
                        {
                            for (var i = 0; i < 8; ++i)
                                Children[i].EnumerateItems(SearchBounds, Into, Filter);
                        }
                        break;
                    case ContainmentType.Contains:
                        if (Children == null)
                        {
                            foreach (var item in Items)
                                if (Filter(item.Body))
                                    Into.Add(item.Body);
                        }
                        else
                            for (var i = 0; i < 8; ++i)
                                Children[i].EnumerateItems(Into, Filter);

                        break;
                }
            }
        }

        public IEnumerable<Tuple<int, BoundingBox>> EnumerateBounds(BoundingFrustum Frustum, int Depth = 0)
        {
            if (Frustum.Intersects(Bounds))
            {
                yield return Tuple.Create(Depth, Bounds);
                if (Children != null)
                    for (var i = 0; i < 8; ++i)
                        foreach (var r in Children[i].EnumerateBounds(Frustum, Depth + 1))
                            yield return r;
            }
        }

        //inspired by http://old.cescg.org/CESCG-2002/DSykoraJJelinek/
        private bool FastIntersectsPlane(Plane plane, BoundingBox box)
        {
            //Pick the corner furthest from the plane along the normal.
            Vector3 vmin = new Vector3(
                plane.Normal.X >= 0.0f ? box.Min.X : box.Max.X,
                plane.Normal.Y >= 0.0f ? box.Min.Y : box.Max.Y,
                plane.Normal.Z >= 0.0f ? box.Min.Z : box.Max.Z
            );
            //Project this point to the plane normal and see which side of the plane it lies.
            if (plane.DotNormal(vmin) > -plane.D)
                return false; //outside
            return true; //inside or //intersect
        }

        private bool FastIntersects(BoundingFrustum frustum, BoundingBox box)
        {
            if (!FastIntersectsPlane(frustum.Top, box)) return false; //outside
            if (!FastIntersectsPlane(frustum.Bottom, box)) return false; //outside
            if (!FastIntersectsPlane(frustum.Left, box)) return false; //outside
            if (!FastIntersectsPlane(frustum.Right, box)) return false; //outside
            if (!FastIntersectsPlane(frustum.Near, box)) return false; //outside
            if (!FastIntersectsPlane(frustum.Far, box)) return false; //outside
            return true; //inside or intersect
        }
    }
}
