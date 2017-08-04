// CollisionManager.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    //[JsonObject(IsReference = true)]
    //public class SpatialHash<T>
    //{
    //    public Dictionary<Point3, List<T>> HashMap { get; set; }

    //    public SpatialHash()
    //    {
    //        HashMap = new Dictionary<Point3, List<T>>();
    //    }

    //    public List<T> GetItems(Point3 location)
    //    {
    //        if (HashMap.ContainsKey(location))
    //            return HashMap[location];
    //        else return null;
    //    }

    //    public List<T> GetItems(DestinationVoxel voxel)
    //    {
    //        return GetItems(new Point3(MathFunctions.FloorInt(voxel.Position.X),
    //                    MathFunctions.FloorInt(voxel.Position.Y),
    //                    MathFunctions.FloorInt(voxel.Position.Z)));
    //    }

    //    public void GetItemsInBox<TObject>(BoundingBox box, HashSet<TObject> items) where TObject : T
    //    {
    //        Point3 minPoint = new Point3(MathFunctions.FloorInt(box.Min.X), MathFunctions.FloorInt(box.Min.Y), MathFunctions.FloorInt(box.Min.Z));
    //        Point3 maxPoint = new Point3(MathFunctions.FloorInt(box.Max.X), MathFunctions.FloorInt(box.Max.Y), MathFunctions.FloorInt(box.Max.Z));
    //        Point3 iter = new Point3();
    //        for (iter.X = minPoint.X; iter.X <= maxPoint.X; iter.X++)
    //        {
    //            for (iter.Y = minPoint.Y; iter.Y <= maxPoint.Y; iter.Y++)
    //            {
    //                for (iter.Z = minPoint.Z; iter.Z <= maxPoint.Z; iter.Z++)
    //                {
    //                    List<T> itemsInVoxel = GetItems(iter);

    //                    if (itemsInVoxel == null) continue;

    //                    foreach (TObject item in itemsInVoxel.OfType<TObject>())
    //                    {
    //                        items.Add(item);
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    public void AddItem(Point3 location, T item)
    //    {
    //        if (HashMap.ContainsKey(location))
    //        {
    //            HashMap[location].Add(item);
    //        }
    //        else
    //        {
    //            List<T> items = new List<T> {item};
    //            HashMap[location] = items;
    //        }
    //    }


    //    public void AddItems(Point3 location, IEnumerable<T> items)
    //    {
    //        if (HashMap.ContainsKey(location))
    //        {
    //            HashMap[location].AddRange(items);
    //        }
    //        else
    //        {
    //            List<T> itemsToAdd = new List<T>();
    //            itemsToAdd.AddRange(items);
    //            HashMap[location] = itemsToAdd;
    //        }
    //    }

    //    public bool RemoveItem(Point3 location, T item)
    //    {
    //        if (!HashMap.ContainsKey(location)) return false;

    //        List<T> items = HashMap[location];
    //        bool removed =  items.Remove(item);

    //        if (items.Count == 0)
    //        {
    //            HashMap.Remove(location);
    //        }
    //        return removed;
    //    }
    //}

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
            Dynamic = 4,
            Both = Static | Dynamic
        }

        public Dictionary<CollisionType, IntegerOctTreeNode<IBoundedObject>> Hashes { get; set; }

        public CollisionManager()
        {
            
        }

        public CollisionManager(BoundingBox bounds)
        {
            Hashes = new Dictionary<CollisionType, IntegerOctTreeNode<IBoundedObject>>();
            Hashes[CollisionType.Static] = new IntegerOctTreeNode<IBoundedObject>(bounds.Min, bounds.Max);
            Hashes[CollisionType.Dynamic] = new IntegerOctTreeNode<IBoundedObject>(bounds.Min, bounds.Max);
        }

        public void AddObject(IBoundedObject bounded, CollisionType type)
        {
            if(type == CollisionType.None)
                return;

            Hashes[type].AddItem(bounded, new IntegerBoundingBox(bounded.GetBoundingBox()));
        }

        public void RemoveObject(IBoundedObject bounded, BoundingBox oldLocation, CollisionType type)
        {
            if(type == CollisionType.None)
                return;

            Hashes[type].RemoveItem(bounded, new IntegerBoundingBox(oldLocation));
        }

        public List<IBoundedObject> GetObjectsAt(VoxelHandle V, CollisionType queryType)
        {
            return GetObjectsAt(new Point3(V.Coordinate.X, V.Coordinate.Y, V.Coordinate.Z), queryType);
        }

        public List<IBoundedObject> GetObjectsAt(Point3 pos, CollisionType queryType)
        {
            HashSet<IBoundedObject> toReturn = new HashSet<IBoundedObject>();
            switch ((int)queryType)
            {
                case (int)CollisionType.Static:
                case (int)CollisionType.Dynamic:
                    Hashes[queryType].FindItemsAt(pos, toReturn);
                    break;
                case ((int)CollisionType.Static | (int)CollisionType.Dynamic):
                    Hashes[CollisionType.Static].FindItemsAt(pos, toReturn);
                    Hashes[CollisionType.Dynamic].FindItemsAt(pos, toReturn);
                    break;
            }
            return toReturn.ToList();
        }


        public void GetObjectsIntersecting(BoundingBox box, HashSet<IBoundedObject> set, CollisionType queryType)
        {
            switch((int) queryType)
            {
                case (int) CollisionType.Static:
                case (int) CollisionType.Dynamic:
                    Hashes[queryType].FindItemsInBox(new IntegerBoundingBox(box), set);
                    break;
                case ((int) CollisionType.Static | (int) CollisionType.Dynamic):
                    Hashes[CollisionType.Static].FindItemsInBox(new IntegerBoundingBox(box), set);
                    Hashes[CollisionType.Dynamic].FindItemsInBox(new IntegerBoundingBox(box), set);
                    break;
            }
        }

        public IEnumerable<IBoundedObject> EnumerateIntersectingObjects(BoundingBox Box, CollisionType CollisionType)
        {
            var hashSet = new HashSet<IBoundedObject>();
            GetObjectsIntersecting(Box, hashSet, CollisionType);
            return hashSet;
        }
     
    }
}