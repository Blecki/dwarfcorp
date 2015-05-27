using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{


    [JsonObject(IsReference = true)]
    public class SpatialHash<T>
    {
        public Dictionary<Point3, List<T>> HashMap { get; set; }

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
            Point3 minPoint = new Point3(MathFunctions.FloorInt(box.Min.X), MathFunctions.FloorInt(box.Min.Y), MathFunctions.FloorInt(box.Min.Z));
            Point3 maxPoint = new Point3(MathFunctions.FloorInt(box.Max.X), MathFunctions.FloorInt(box.Max.Y), MathFunctions.FloorInt(box.Max.Z));
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
                List<T> items = new List<T> {item};
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
            bool removed =  items.Remove(item);

            if (items.Count == 0)
            {
                HashMap.Remove(location);
            }
            return removed;
        }
    }

    /// <summary>
    /// Maintains a number of labeled octrees, and allows collision
    /// queries for different kinds of objects in the world.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CollisionManager
    {
        [Flags]
        public enum CollisionType
        {
            None = 0,
            Static = 2,
            Dynamic = 4
        }

        public Dictionary<CollisionType, SpatialHash<IBoundedObject>> Hashes { get; set; }

        public CollisionManager()
        {
            
        }

        public CollisionManager(BoundingBox bounds)
        {
            Hashes = new Dictionary<CollisionType, SpatialHash<IBoundedObject>>();
            Hashes[CollisionType.Static] = new SpatialHash<IBoundedObject>();
            Hashes[CollisionType.Dynamic] = new SpatialHash<IBoundedObject>();
        }

        public void AddObject(IBoundedObject bounded, CollisionType type)
        {
            if(type == CollisionType.None)
            {
                return;
            }
            BoundingBox box = bounded.GetBoundingBox();
            Point3 minPoint = new Point3(MathFunctions.FloorInt(box.Min.X), MathFunctions.FloorInt(box.Min.Y), MathFunctions.FloorInt(box.Min.Z));
            Point3 maxPoint = new Point3(MathFunctions.FloorInt(box.Max.X), MathFunctions.FloorInt(box.Max.Y), MathFunctions.FloorInt(box.Max.Z));
            Point3 iter = new Point3();
            for (iter.X = minPoint.X; iter.X <= maxPoint.X; iter.X++)
            {
                for (iter.Y = minPoint.Y; iter.Y <= maxPoint.Y; iter.Y++)
                {
                    for (iter.Z = minPoint.Z; iter.Z <= maxPoint.Z; iter.Z++)
                    {
                        Hashes[type].AddItem(iter, bounded);
                    }
                }
            }

        }

        public void RemoveObject(IBoundedObject bounded, BoundingBox oldLocation, CollisionType type)
        {
            if(type == CollisionType.None)
            {
                return;
            }
            Point3 minPoint = new Point3(MathFunctions.FloorInt(oldLocation.Min.X), MathFunctions.FloorInt(oldLocation.Min.Y), MathFunctions.FloorInt(oldLocation.Min.Z));
            Point3 maxPoint = new Point3(MathFunctions.FloorInt(oldLocation.Max.X), MathFunctions.FloorInt(oldLocation.Max.Y), MathFunctions.FloorInt(oldLocation.Max.Z));
            Point3 iter = new Point3();
            for (iter.X = minPoint.X; iter.X <= maxPoint.X; iter.X++)
            {
                for (iter.Y = minPoint.Y; iter.Y <= maxPoint.Y; iter.Y++)
                {
                    for (iter.Z = minPoint.Z; iter.Z <= maxPoint.Z; iter.Z++)
                    {
                        Hashes[type].RemoveItem(iter, bounded);
                    }
                }
            }
        }


        public void GetObjectsIntersecting<TObject>(BoundingBox box, HashSet<TObject> set, CollisionType queryType) where TObject : IBoundedObject
        {
            switch((int) queryType)
            {
                case (int) CollisionType.Static:
                case (int) CollisionType.Dynamic:
                    Hashes[queryType].GetItemsInBox(box, set);
                    break;
                case ((int) CollisionType.Static | (int) CollisionType.Dynamic):
                    Hashes[CollisionType.Static].GetItemsInBox(box, set);
                    Hashes[CollisionType.Dynamic].GetItemsInBox(box, set);
                    break;
            }
        }

        public void GetObjectsIntersecting<TObject>(BoundingFrustum frustum, HashSet<TObject> set, CollisionType queryType) where TObject : IBoundedObject
        {
            List<SpatialHash<IBoundedObject>> hashes = new List<SpatialHash<IBoundedObject>>();
            switch ((int)queryType)
            {
                case (int)CollisionType.Static:
                case (int)CollisionType.Dynamic:
                    hashes.Add(Hashes[queryType]);
                    break;
                case ((int)CollisionType.Static | (int)CollisionType.Dynamic):
                    hashes.Add(Hashes[CollisionType.Static]);
                    hashes.Add(Hashes[CollisionType.Dynamic]);
                    break;
            }

            foreach (var obj in 
                from hash 
                    in hashes 
                from pair 
                    in hash.HashMap 
                where pair.Value != null && frustum.Contains(pair.Key.ToVector3()) == ContainmentType.Contains
                from obj in pair.Value
                where obj is TObject && !set.Contains((TObject)obj) && obj.GetBoundingBox().Intersects(frustum) 
                select obj)
            {
                set.Add((TObject) obj);
            }
           
        }

        public void GetObjectsIntersecting<TObject>(BoundingSphere sphere, HashSet<TObject> set, CollisionType queryType) where TObject : IBoundedObject
        {
            HashSet<TObject> intersectingBounds = new HashSet<TObject>();
            BoundingBox box = MathFunctions.GetBoundingBox(sphere);
            switch ((int)queryType)
            {
                case (int)CollisionType.Static:
                case (int)CollisionType.Dynamic:
                    Hashes[queryType].GetItemsInBox(box, intersectingBounds);
                    break;
                case ((int)CollisionType.Static | (int)CollisionType.Dynamic):
                    Hashes[CollisionType.Static].GetItemsInBox(box, intersectingBounds);
                    Hashes[CollisionType.Dynamic].GetItemsInBox(box, intersectingBounds);
                    break;
            }
            intersectingBounds.RemoveWhere(obj => !obj.GetBoundingBox().Intersects(sphere));
        }



        public void GetObjectsIntersecting<TObject>(Ray ray, HashSet<TObject> set, CollisionType queryType)
            where TObject : IBoundedObject
        {
            if (queryType == (CollisionType.Static | CollisionType.Dynamic))
            {
                GetObjectsIntersecting<TObject>(ray, set, CollisionType.Static);
                GetObjectsIntersecting<TObject>(ray, set, CollisionType.Dynamic);
            }
            else foreach(Point3 pos in MathFunctions.RasterizeLine(ray.Position, ray.Direction*100 + ray.Position))
            {
                if (queryType != CollisionType.Static && queryType != CollisionType.Dynamic) continue;

                List<IBoundedObject> obj = Hashes[queryType].GetItems(pos);
                if (obj == null) continue;
                foreach (TObject item in obj.OfType<TObject>().Where(item => !set.Contains(item) && ray.Intersects(item.GetBoundingBox()) != null))
                {
                    set.Add(item);
                }
            }
        }


        public List<T> GetVisibleObjects<T>(BoundingFrustum getFrustrum, CollisionType collisionType) where T : IBoundedObject
        {
            HashSet<T> objects = new HashSet<T>();
            GetObjectsIntersecting(getFrustrum, objects, collisionType);
            return objects.ToList();
        }

        public void DebugDraw()
        {
            foreach (var pair in Hashes)
            {
                foreach (var cell in pair.Value.HashMap)
                {
                    if(cell.Value != null)
                        Drawer2D.DrawText(cell.Value.Count + "", cell.Key.ToVector3() + Vector3.One * 0.5f, Color.White, Color.Black);
                }
            }

        }
    }

}