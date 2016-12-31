﻿// CollisionManager.cs
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
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A spatial hash is a hash map which maps 3D grid coordinates to a list of objects.
    /// </summary>
    /// <typeparam name="T">Object type stored in the spatial hash.</typeparam>
    [JsonObject(IsReference = true)]
    public class SpatialHash<T>
    {
        public SpatialHash()
        {
            HashMap = new Dictionary<Point3, List<T>>();
        }

        /// <summary>
        /// Gets or sets the hash map.
        /// </summary>
        /// <value>
        /// The hash map.
        /// </value>
        public Dictionary<Point3, List<T>> HashMap { get; set; }

        /// <summary>
        /// Gets the items at the specified location..
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>A list of items at that location.</returns>
        public List<T> GetItems(Point3 location)
        {
            if (HashMap.ContainsKey(location))
                return HashMap[location];
            return null;
        }

        /// <summary>
        /// Gets the items at the specified voxel.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <returns>A list of items at that voxel.</returns>
        public List<T> GetItems(Voxel voxel)
        {
            return GetItems(new Point3(MathFunctions.FloorInt(voxel.Position.X + 0.5f),
                MathFunctions.FloorInt(voxel.Position.Y + 0.5f),
                MathFunctions.FloorInt(voxel.Position.Z + 0.5f)));
        }

        /// <summary>
        /// Gets the items intersecting the bounding box.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="box">The box.</param>
        /// <param name="items">The items.</param>
        public void GetItemsInBox<TObject>(BoundingBox box, HashSet<TObject> items) where TObject : T
        {
            var minPoint = new Point3(MathFunctions.FloorInt(box.Min.X), MathFunctions.FloorInt(box.Min.Y),
                MathFunctions.FloorInt(box.Min.Z));
            var maxPoint = new Point3(MathFunctions.FloorInt(box.Max.X), MathFunctions.FloorInt(box.Max.Y),
                MathFunctions.FloorInt(box.Max.Z));
            var iter = new Point3();
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

        /// <summary>
        /// Adds the item to the specified location.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="item">The item.</param>
        public void AddItem(Point3 location, T item)
        {
            if (HashMap.ContainsKey(location))
            {
                HashMap[location].Add(item);
            }
            else
            {
                var items = new List<T> {item};
                HashMap[location] = items;
            }
        }


        /// <summary>
        /// Adds the items to the specified location.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="items">The items.</param>
        public void AddItems(Point3 location, IEnumerable<T> items)
        {
            if (HashMap.ContainsKey(location))
            {
                HashMap[location].AddRange(items);
            }
            else
            {
                var itemsToAdd = new List<T>();
                itemsToAdd.AddRange(items);
                HashMap[location] = itemsToAdd;
            }
        }

        /// <summary>
        /// Removes the item from the specified location.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="item">The item.</param>
        /// <returns>true if the item could be removed.</returns>
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

    /// <summary>
    ///     Maintains a number of labeled spatial hashes, and allows collision
    ///     queries for different kinds of objects in the world.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CollisionManager
    {
        /// <summary>
        /// Objects are either static, dynamic or both.
        /// </summary>
        [Flags]
        public enum CollisionType
        {
            None = 0,
            Static = 2,
            Dynamic = 4
        }

        public CollisionManager()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollisionManager"/> class.
        /// </summary>
        /// <param name="bounds">The bounds of the world (unused for now).</param>
        public CollisionManager(BoundingBox bounds)
        {
            Hashes = new Dictionary<CollisionType, SpatialHash<IBoundedObject>>();
            Hashes[CollisionType.Static] = new SpatialHash<IBoundedObject>();
            Hashes[CollisionType.Dynamic] = new SpatialHash<IBoundedObject>();
        }

        /// <summary>
        /// A dictionary of different spatial hash maps for each collision type.
        /// </summary>
        /// <value>
        /// The hashes.
        /// </value>
        public Dictionary<CollisionType, SpatialHash<IBoundedObject>> Hashes { get; set; }

        /// <summary>
        /// Adds the specified object to the hash map.
        /// </summary>
        /// <param name="bounded">The bounded object to add.</param>
        /// <param name="type">The type of the object..</param>
        public void AddObject(IBoundedObject bounded, CollisionType type)
        {
            if (type == CollisionType.None)
            {
                return;
            }
            BoundingBox box = bounded.GetBoundingBox();
            var minPoint = new Point3(MathFunctions.FloorInt(box.Min.X), MathFunctions.FloorInt(box.Min.Y),
                MathFunctions.FloorInt(box.Min.Z));
            var maxPoint = new Point3(MathFunctions.FloorInt(box.Max.X), MathFunctions.FloorInt(box.Max.Y),
                MathFunctions.FloorInt(box.Max.Z));
            var iter = new Point3();

            // Add the object to every grid cell that its bounding box intersects.
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

        /// <summary>
        /// Removes the bounded object from a location it was previously in.
        /// </summary>
        /// <param name="bounded">The bounded.</param>
        /// <param name="oldLocation">The old location.</param>
        /// <param name="type">The type.</param>
        public void RemoveObject(IBoundedObject bounded, BoundingBox oldLocation, CollisionType type)
        {
            if (type == CollisionType.None)
            {
                return;
            }
            var minPoint = new Point3(MathFunctions.FloorInt(oldLocation.Min.X),
                MathFunctions.FloorInt(oldLocation.Min.Y), MathFunctions.FloorInt(oldLocation.Min.Z));
            var maxPoint = new Point3(MathFunctions.FloorInt(oldLocation.Max.X),
                MathFunctions.FloorInt(oldLocation.Max.Y), MathFunctions.FloorInt(oldLocation.Max.Z));
            var iter = new Point3();
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

        /// <summary>
        /// Gets a list of bounded objects at a voxel.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <param name="queryType">Type of the query.</param>
        /// <returns>A list of bounded objects intersecting the voxel.</returns>
        public List<IBoundedObject> GetObjectsAt(Voxel voxel, CollisionType queryType)
        {
            return GetObjectsAt(new Point3(MathFunctions.FloorInt(voxel.Position.X),
                MathFunctions.FloorInt(voxel.Position.Y), MathFunctions.FloorInt(voxel.Position.Z)), queryType);
        }

        /// <summary>
        /// Gets a list of objects at a point.
        /// </summary>
        /// <param name="pos">The position.</param>
        /// <param name="queryType">Type of the query.</param>
        /// <returns>The list of objects intersecting that point.</returns>
        public List<IBoundedObject> GetObjectsAt(Point3 pos, CollisionType queryType)
        {
            var toReturn = new List<IBoundedObject>();
            switch ((int) queryType)
            {
                case (int) CollisionType.Static:
                case (int) CollisionType.Dynamic:
                    toReturn = Hashes[queryType].GetItems(pos);
                    break;
                case ((int) CollisionType.Static | (int) CollisionType.Dynamic):
                    toReturn.AddRange(Hashes[CollisionType.Static].GetItems(pos));
                    toReturn.AddRange(Hashes[CollisionType.Dynamic].GetItems(pos));
                    break;
            }
            return toReturn;
        }


        /// <summary>
        /// Gets a list of bounded objects intersecting a bounding box.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="box">The box.</param>
        /// <param name="set">The set intersecting the box.</param>
        /// <param name="queryType">Type of the query.</param>
        public void GetObjectsIntersecting<TObject>(BoundingBox box, HashSet<TObject> set, CollisionType queryType)
            where TObject : IBoundedObject
        {
            switch ((int) queryType)
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

        /// <summary>
        /// Gets a list of bounded objects intersecting the given bounding frustum.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="frustum">The frustum.</param>
        /// <param name="set">The set.</param>
        /// <param name="queryType">Type of the query.</param>
        public void GetObjectsIntersecting<TObject>(BoundingFrustum frustum, HashSet<TObject> set,
            CollisionType queryType) where TObject : IBoundedObject
        {
            var hashes = new List<SpatialHash<IBoundedObject>>();
            switch ((int) queryType)
            {
                case (int) CollisionType.Static:
                case (int) CollisionType.Dynamic:
                    hashes.Add(Hashes[queryType]);
                    break;
                case ((int) CollisionType.Static | (int) CollisionType.Dynamic):
                    hashes.Add(Hashes[CollisionType.Static]);
                    hashes.Add(Hashes[CollisionType.Dynamic]);
                    break;
            }

            BoundingBox frustumBox = MathFunctions.GetBoundingBox(frustum.GetCorners());

            foreach (IBoundedObject obj in 
                from hash
                    in hashes
                from pair
                    in hash.HashMap
                where pair.Value != null && frustumBox.Contains(pair.Key.ToVector3()) == ContainmentType.Contains
                from obj in pair.Value
                where obj is TObject && !set.Contains((TObject) obj) && obj.GetBoundingBox().Intersects(frustum)
                select obj)
            {
                set.Add((TObject) obj);
            }
        }

        /// <summary>
        /// Gets the list of objects intersecting the given bounding sphere.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="sphere">The sphere.</param>
        /// <param name="set">The set of objects intersecting the given bounding sphere.</param>
        /// <param name="queryType">Type of the query.</param>
        public void GetObjectsIntersecting<TObject>(BoundingSphere sphere, HashSet<TObject> set, CollisionType queryType)
            where TObject : IBoundedObject
        {
            var intersectingBounds = new HashSet<TObject>();
            BoundingBox box = MathFunctions.GetBoundingBox(sphere);
            switch ((int) queryType)
            {
                case (int) CollisionType.Static:
                case (int) CollisionType.Dynamic:
                    Hashes[queryType].GetItemsInBox(box, intersectingBounds);
                    break;
                case ((int) CollisionType.Static | (int) CollisionType.Dynamic):
                    Hashes[CollisionType.Static].GetItemsInBox(box, intersectingBounds);
                    Hashes[CollisionType.Dynamic].GetItemsInBox(box, intersectingBounds);
                    break;
            }
            intersectingBounds.RemoveWhere(obj => !obj.GetBoundingBox().Intersects(sphere));
        }

        /// <summary>
        /// Gets the objects intersectin the given ray.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="ray">The ray.</param>
        /// <param name="set">The set.</param>
        /// <param name="queryType">Type of the query.</param>
        public void GetObjectsIntersecting<TObject>(Ray ray, HashSet<TObject> set, CollisionType queryType)
            where TObject : IBoundedObject
        {
            if (queryType == (CollisionType.Static | CollisionType.Dynamic))
            {
                GetObjectsIntersecting(ray, set, CollisionType.Static);
                GetObjectsIntersecting(ray, set, CollisionType.Dynamic);
            }
            else
                foreach (Point3 pos in MathFunctions.RasterizeLine(ray.Position, ray.Direction*100 + ray.Position))
                {
                    if (queryType != CollisionType.Static && queryType != CollisionType.Dynamic) continue;

                    List<IBoundedObject> obj = Hashes[queryType].GetItems(pos);
                    if (obj == null) continue;
                    foreach (
                        TObject item in
                            obj.OfType<TObject>()
                                .Where(item => !set.Contains(item) && ray.Intersects(item.GetBoundingBox()) != null))
                    {
                        set.Add(item);
                    }
                }
        }


        /// <summary>
        /// Gets the list of objects visible to the given bounding frustum.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="getFrustrum">The get frustrum.</param>
        /// <param name="collisionType">Type of the collision.</param>
        /// <returns>A list of objects intersecting the frustum.</returns>
        public List<T> GetVisibleObjects<T>(BoundingFrustum getFrustrum, CollisionType collisionType)
            where T : IBoundedObject
        {
            var objects = new HashSet<T>();
            GetObjectsIntersecting(getFrustrum, objects, collisionType);
            return objects.ToList();
        }

        /// <summary>
        /// Draws dummy text telling us how many objects are in each hash.
        /// </summary>
        public void DebugDraw()
        {
            foreach (var pair in Hashes)
            {
                foreach (var cell in pair.Value.HashMap)
                {
                    if (cell.Value != null)
                        Drawer2D.DrawText(cell.Value.Count + "", cell.Key.ToVector3() + Vector3.One*0.5f, Color.White,
                            Color.Black);
                }
            }
        }
    }
}