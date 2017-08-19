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

        public Dictionary<CollisionType, OctTreeNode<IBoundedObject>> Hashes { get; set; }

        public CollisionManager()
        {
            
        }

        public CollisionManager(BoundingBox bounds)
        {
            Hashes = new Dictionary<CollisionType, OctTreeNode<IBoundedObject>>();
            Hashes[CollisionType.Static] = new OctTreeNode<IBoundedObject>(bounds.Min, bounds.Max);
            Hashes[CollisionType.Dynamic] = new OctTreeNode<IBoundedObject>(bounds.Min, bounds.Max);
        }

        public void AddObject(IBoundedObject bounded, CollisionType type)
        {
            if(type == CollisionType.None)
                return;

            Hashes[type].AddItem(bounded, bounded.GetBoundingBox());
        }

        public void RemoveObject(IBoundedObject bounded, BoundingBox oldLocation, CollisionType type)
        {
            if(type == CollisionType.None)
                return;

            Hashes[type].RemoveItem(bounded, oldLocation);
        }

        public IEnumerable<IBoundedObject> EnumerateIntersectingObjects(BoundingBox box, CollisionType queryType)
        {
            switch((int) queryType)
            {
                case (int) CollisionType.Static:
                case (int) CollisionType.Dynamic:
                    return Hashes[queryType].EnumerateItems(box);
                case ((int) CollisionType.Static | (int) CollisionType.Dynamic):
                    return Hashes[CollisionType.Static].EnumerateItems(box).Concat(Hashes[CollisionType.Dynamic].EnumerateItems(box));
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}