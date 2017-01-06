// Octree.cs
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
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// This data structure divides 3D space into 3D octants (8 splits per node) in a tree structure.
    /// At the base of the tree are bounded objects. Makes it efficient to query for collisions.
    /// </summary>
    [JsonObject(IsReference =  true)]
    public class Octree
    {
        public int MaxDepth { get; set; }
        public int MaxObjectsPerNode { get; set; }
        public int MinObjectsPerNode { get; set; }
        public BoundingBox Bounds { get; set; }
        public OctreeNode Root { get; set; }
        public bool DebugDraw { get; set; }

        [JsonIgnore]
        public ReaderWriterLockSlim Lock { get; set; }

        [JsonIgnore]
        public Dictionary<IBoundedObject, OctreeNode> ObjectsToNodes { get; set; }

        [JsonIgnore]
        protected Dictionary<IBoundedObject, bool> ObjectsToUpdate { get; set; }
        public Timer UpdateTimer { get; set; }

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            ComputeObjectsToNodes();
        }

        public Octree()
        {
            Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            ObjectsToNodes = new Dictionary<IBoundedObject, OctreeNode>();
            ObjectsToUpdate = new Dictionary<IBoundedObject, bool>();
        }

        public Octree(BoundingBox bounds, int maxDepth, int maxObjectsPerNode, int minObjectsPerNode)
        {
            Bounds = bounds;
            MaxDepth = maxDepth;
            MaxObjectsPerNode = maxObjectsPerNode;
            MinObjectsPerNode = minObjectsPerNode;
            Root = new OctreeNode(Bounds, this, 0, null);
            DebugDraw = false;
            Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            ObjectsToNodes = new Dictionary<IBoundedObject, OctreeNode>();
            ObjectsToUpdate = new Dictionary<IBoundedObject, bool>();
            UpdateTimer = new Timer(1.0f, false);
        }

        protected void ComputeObjectsToNodes()
        {
            Root.ComputeObjectsToNodesRecursive(ObjectsToNodes);
        }

        public void AddUpdate(IBoundedObject obj)
        {
            lock(ObjectsToUpdate)
            {
                ObjectsToUpdate[obj] = true;
            }
        }

        public void AddObjectRecursive(IBoundedObject obj)
        {
            if(ObjectsToNodes.ContainsKey(obj))
            {
                return;
            }
            else
            {
                Root.AddObjectRecursive(obj);
            }
        }

        public bool RemoveObject(IBoundedObject obj)
        {
            if(!ObjectsToNodes.ContainsKey(obj))
            {
                return false;
            }
            else
            {
                OctreeNode node = ObjectsToNodes[obj];
                return node.RemoveObject(obj);
            }
        }


        public void ExpandAndRebuild()
        {
            Lock.EnterWriteLock();
            ObjectsToNodes.Clear();
            List<IBoundedObject> objectsInTree = Root.MergeRecursive();

            BoundingBox bounds = new BoundingBox(Bounds.Min * 2.0f, Bounds.Max * 2.0f);
            Bounds = bounds;


            Root = new OctreeNode(Bounds, this, 0, null);

            foreach(IBoundedObject loc in objectsInTree)
            {
                AddObjectRecursive(loc);
            }

            Lock.ExitWriteLock();
        }

        public bool NeedsUpdate(IBoundedObject component)
        {
            if(ObjectsToUpdate.ContainsKey(component))
            {
                return false;
            }

            return !ObjectsToNodes.ContainsKey(component) || ObjectsToNodes[component].Bounds.Intersects(component.GetBoundingBox());
        }

        public void Update(DwarfTime time)
        {
            UpdateTimer.Update(time);

            if(!UpdateTimer.HasTriggered)
            {
                return;
            }

            lock(ObjectsToUpdate)
            {
                foreach(KeyValuePair<IBoundedObject, bool> pair in ObjectsToUpdate)
                {
                    RemoveObject(pair.Key);
                    AddObjectRecursive(pair.Key);
                }

                ObjectsToUpdate.Clear();
            }
        }
    }

}