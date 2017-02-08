using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace ABTest.Oct
{
    public class BoundingBox
    {
        public Vector3 Min;
        public Vector3 Max;

        private static float Fudge = 0.01f;

        public BoundingBox(Vector3 Min, Vector3 Max)
        {
            this.Min = Min - new Vector3(Fudge, Fudge, Fudge);
            this.Max = Max + new Vector3(Fudge, Fudge, Fudge);
        }

        public BoundingBox(Microsoft.Xna.Framework.BoundingBox Box)
        {
            this.Min = Box.Min;
            this.Max = Box.Max;
        }

        public bool Contains(Vector3 P)
        {
            return P.X + Fudge >= Min.X && P.X - Fudge <= Max.X &&
                   P.Y + Fudge >= Min.Y && P.Y - Fudge <= Max.Y &&
                   P.Z + Fudge >= Min.Z && P.Z - Fudge <= Max.Z;
        }

        public bool Intersects(BoundingBox Other)
        {
            if (Min.X + Fudge > Other.Max.X || Max.X - Fudge < Other.Min.X) return false;
            if (Min.Y + Fudge > Other.Max.Y || Max.Y - Fudge < Other.Min.Y) return false;
            if (Min.Z + Fudge > Other.Max.Z || Max.Z - Fudge < Other.Min.Z) return false;
            return true;
        }
    }

    public class NaiveOctTreeNode<T>
    {
        public BoundingBox Bounds;
        public NaiveOctTreeNode<T>[] Children;
        public List<Tuple<T, Vector3>> Items;

        public NaiveOctTreeNode(Vector3 Min, Vector3 Max)
        {
            this.Bounds = new BoundingBox(Min, Max);
        }

        private void Subdivide()
        {
            var Min = Bounds.Min;
            var Max = Bounds.Max;
            var Mid = (Min + Max) / 2.0f;

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

        public void AddItem(T Item, Vector3 Point)
        {
            AddToTree(Tuple.Create(Item, Point), 8);
        }

        private void AddToTree(Tuple<T, Vector3> Item, int SubdivideThreshold)
        {
            if (!Bounds.Contains(Item.Item2)) return;

            if (Items == null)
            {
                if (Children == null)
                {
                    Items = new List<Tuple<T, Vector3>>();
                    Items.Add(Item);
                    (Item.Item1 as SpacialHashTag).OwnerNode = this as NaiveOctTreeNode<SpacialHashTag>;
                    return;
                }
                else
                {
                    for (var i = 0; i < 8; ++i)
                        Children[i].AddToTree(Item, SubdivideThreshold);
                }
            }
            else
            {
                if (Items.Count == SubdivideThreshold && (Bounds.Max.X - Bounds.Min.X) > 4)
                {
                    Subdivide();
                    for (var i = 0; i < Items.Count; ++i)
                        for (var c = 0; c < 8; ++c)
                            Children[c].AddToTree(Items[i], SubdivideThreshold);
                    Items = null;
                }
                else
                {
                    Items.Add(Item);
                    (Item.Item1 as SpacialHashTag).OwnerNode = this as NaiveOctTreeNode<SpacialHashTag>;
                }
            }
        }

        public void FindItemsInBox(BoundingBox SearchBounds, HashSet<T> results)
        {
            if (SearchBounds.Intersects(Bounds))
            {
                if (Children == null)
                {
                    if (Items == null) return;
                    for (var i = 0; i < Items.Count; ++i)
                        if (SearchBounds.Contains(Items[i].Item2))
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
