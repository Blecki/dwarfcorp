using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace ABTest.SpacialHash
{
    public class SpatialHash<T>
    {
        public Dictionary<Point3, List<T>> HashMap { get; set; }

        public static int FloorInt(float s)
        {
            return (int)Math.Floor(s);
        }

        public SpatialHash()
        {
            HashMap = new Dictionary<Point3, List<T>>();
        }

        public List<T> GetItems(Point3 location)
        {
            if (HashMap.ContainsKey(location))
                return HashMap[location];
            else return null;
        }

       
        public void GetItemsInBox<TObject>(BoundingBox box, HashSet<TObject> items) where TObject : T
        {
            Point3 minPoint = new Point3(FloorInt(box.Min.X), FloorInt(box.Min.Y), FloorInt(box.Min.Z));
            Point3 maxPoint = new Point3(FloorInt(box.Max.X), FloorInt(box.Max.Y), FloorInt(box.Max.Z));
            Point3 iter = new Point3();
            for (iter.X = minPoint.X; iter.X <= maxPoint.X; iter.X++)
            {
                for (iter.Y = minPoint.Y; iter.Y <= maxPoint.Y; iter.Y++)
                {
                    for (iter.Z = minPoint.Z; iter.Z <= maxPoint.Z; iter.Z++)
                    {
                        List<T> itemsInVoxel = GetItems(iter);

                        if (itemsInVoxel == null) continue;

                        foreach (TObject item in itemsInVoxel.OfType<TObject>())
                        {
                            items.Add(item);
                        }
                    }
                }
            }
        }

        public void AddItem(Point3 location, T item)
        {
            if (HashMap.ContainsKey(location))
            {
                HashMap[location].Add(item);
            }
            else
            {
                List<T> items = new List<T> { item };
                HashMap[location] = items;
            }
        }


        public void AddItems(Point3 location, IEnumerable<T> items)
        {
            if (HashMap.ContainsKey(location))
            {
                HashMap[location].AddRange(items);
            }
            else
            {
                List<T> itemsToAdd = new List<T>();
                itemsToAdd.AddRange(items);
                HashMap[location] = itemsToAdd;
            }
        }

        public bool RemoveItem(Point3 location, T item)
        {
            if (!HashMap.ContainsKey(location)) return false;

            List<T> items = HashMap[location];
            bool removed = items.Remove(item);

            if (items.Count == 0)
            {
                HashMap.Remove(location);
            }
            return removed;
        }
    }
}
