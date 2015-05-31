// OctreeNode.cs
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
using System.Security.Cryptography.X509Certificates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A particular octant in an octree.
    /// </summary>
    [JsonObject(IsReference =  true)]
    public class OctreeNode
    {
        public enum NodeType
        {
            UpperBackRight,
            UpperBackLeft,
            UpperFrontRight,
            UpperFrontLeft,
            LowerBackRight,
            LowerBackLeft,
            LowerFrontRight,
            LowerFrontLeft
        }

        public OctreeNode[] Children =
        {
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        };

        public BoundingBox Bounds { get; set; }

        public List<IBoundedObject> Objects { get; set; }

        public Octree Tree { get; set; }

        public OctreeNode Parent { get; set; }

        public int Depth { get; set; }

        public object ObjectLock = new object();

        public void ComputeObjectsToNodesRecursive(Dictionary<IBoundedObject, OctreeNode> nodeMap)
        {
            lock(ObjectLock)
            {
                foreach(IBoundedObject obj in Objects)
                {
                    nodeMap[obj] = this;
                }
            }

            foreach(OctreeNode node in Children.Where(node => node != null))
            {
                node.ComputeObjectsToNodesRecursive(nodeMap);
            }
        }

        public bool HasChild(NodeType node)
        {
            bool toReturn = Children[(int) node] != null;
            return toReturn;
        }

        public bool HasChild(int node)
        {
            return Children[node] != null;
        }

        public bool HasChildren()
        {
            return HasChild(0) ||
                   HasChild(1) ||
                   HasChild(2) ||
                   HasChild(3) ||
                   HasChild(4) ||
                   HasChild(5) ||
                   HasChild(6) ||
                   HasChild(7);
        }


        public OctreeNode()
        {
            
        }

        public OctreeNode(BoundingBox bounds, Octree tree, int depth, OctreeNode parent)
        {
            Bounds = bounds;
            Objects = new List<IBoundedObject>();
            Tree = tree;
            Parent = parent;
            Depth = depth;
        }


        public T GetComponentIntersecting<T>(Vector3 vect) where T : IBoundedObject
        {
            if(Bounds.Contains(vect) == ContainmentType.Disjoint)
            {
                return default(T);
            }

            lock(ObjectLock)
            {
                foreach(IBoundedObject o in Objects.Where(o => o is T && o.GetBoundingBox().Contains(vect) != ContainmentType.Disjoint))
                {
                    return (T) o;
                }
            }

            for(int i = 0; i < 8; i++)
            {
                OctreeNode child = Children[i];
                if(child == null)
                {
                    continue;
                }

                T got = child.GetComponentIntersecting<T>(vect);

                if(got != null)
                {
                    return got;
                }
            }
            return default(T);
        }

        public void GetComponentsIntersecting<T>(BoundingBox box, HashSet<T> set) where T : IBoundedObject
        {
            Stack<OctreeNode> stack = new Stack<OctreeNode>();
            stack.Push(this);

            while(stack.Count > 0)
            {
                OctreeNode t = stack.Pop();
                if(!t.Bounds.Intersects(box))
                {
                    continue;
                }

                lock(t.ObjectLock)
                {
                    foreach(IBoundedObject o in t.Objects.Where(o => o is T && o.GetBoundingBox().Intersects(box)))
                    {
                        set.Add((T) o);
                    }
                }


                for(int i = 0; i < 8; i++)
                {
                    OctreeNode child = t.Children[i];
                    if(child != null)
                    {
                        stack.Push(child);
                    }
                }
            }
        }


        public List<T> GetVisibleObjects<T>(BoundingFrustum frustrum) where T : IBoundedObject
        {
            List<T> toReturn = new List<T>();

            Stack<OctreeNode> stack = new Stack<OctreeNode>();
            stack.Push(this);

            while (stack.Count > 0)
            {
                OctreeNode t = stack.Pop();
                if (!t.Bounds.Intersects(frustrum))
                {
                    continue;
                }

                lock (t.ObjectLock)
                {
                    toReturn.AddRange(t.Objects.OfType<T>());
                }


                for (int i = 0; i < 8; i++)
                {
                    OctreeNode child = t.Children[i];
                    if (child != null)
                    {
                        stack.Push(child);
                    }
                }
            }

            return toReturn;
        }


        public void GetComponentsIntersecting<T>(BoundingFrustum box, HashSet<T> set) where T : IBoundedObject
        {
            Stack<OctreeNode> stack = new Stack<OctreeNode>();
            stack.Push(this);

            while(stack.Count > 0)
            {
                OctreeNode t = stack.Pop();
                if(!t.Bounds.Intersects(box))
                {
                    continue;
                }

                lock(t.ObjectLock)
                {
                    foreach(IBoundedObject o in t.Objects.Where(o => o is T && o.GetBoundingBox().Intersects(box)))
                    {
                        set.Add((T) o);
                    }
                }


                for(int i = 0; i < 8; i++)
                {
                    OctreeNode child = t.Children[i];
                    if(child != null)
                    {
                        stack.Push(child);
                    }
                }
            }
        }

        public void GetComponentsIntersecting<T>(BoundingSphere box, HashSet<T> set) where T : IBoundedObject
        {
            Stack<OctreeNode> stack = new Stack<OctreeNode>();
            stack.Push(this);

            while(stack.Count > 0)
            {
                OctreeNode t = stack.Peek();
                if(t.Bounds.Intersects(box))
                {
                    lock(t.ObjectLock)
                    {
                        foreach(IBoundedObject o in t.Objects.Where(o => o is T && o.GetBoundingBox().Intersects(box)))
                        {
                            set.Add((T) o);
                        }
                    }


                    for(int i = 0; i < 8; i++)
                    {
                        OctreeNode child = t.Children[i];
                        if(child != null)
                        {
                            stack.Push(child);
                        }
                    }
                }

                stack.Pop();
            }
        }

        public void GetComponentsIntersecting<T>(Ray box, HashSet<T> set) where T : IBoundedObject
        {
            Stack<OctreeNode> stack = new Stack<OctreeNode>();
            stack.Push(this);

            while(stack.Count > 0)
            {
                OctreeNode t = stack.Pop();
                if(t.Bounds.Intersects(box) == null)
                {
                    continue;
                }

                lock(t.ObjectLock)
                {
                    foreach(IBoundedObject o in t.Objects.Where(o => o is T && o.GetBoundingBox().Intersects(box) != null))
                    {
                        set.Add((T) o);
                    }
                }


                for(int i = 0; i < 8; i++)
                {
                    OctreeNode child = t.Children[i];
                    if(child != null)
                    {
                        stack.Push(child);
                    }
                }
            }
        }

        public bool ExistsInTreeRecursive(IBoundedObject component)
        {
            lock (ObjectLock)
            {
                if (Objects.Contains(component) || Tree.ObjectsToNodes.ContainsKey(component))
                {
                    return true;
                }
                else
                {
                    for (int i = 0; i < 8; i++)
                    {
                        OctreeNode node = Children[i];

                        if (node == null)
                        {
                            continue;
                        }

                        if (node.ExistsInTreeRecursive(component))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public bool NeedsUpdateRecursive(IBoundedObject component)
        {
            lock (ObjectLock)
            {
                if (Objects.Contains(component))
                {
                    return !component.GetBoundingBox().Intersects(Bounds);
                }
                else if (HasChildren())
                {
                    bool shouldUpdate = false;
                    for (int i = 0; i < 8; i++)
                    {
                        OctreeNode node = Children[i];
                        if (node == null)
                        {
                            continue;
                        }

                        if (node.NeedsUpdateRecursive(component))
                        {
                            shouldUpdate = true;
                        }
                    }


                    return shouldUpdate;
                }
                else
                {
                    return false;
                }
            }
        }

        public void AddObjectRecursive(IBoundedObject component)
        {
            if(Parent == null && !component.GetBoundingBox().Intersects(Bounds))
            {
                Tree.ExpandAndRebuild();
                return;
            }

            if(component.GetBoundingBox().Intersects(Bounds) && !HasChildren())
            {
                lock (ObjectLock)
                {
                    if (!Objects.Contains(component))
                    {
                        Objects.Add(component);
                    }

                    Tree.ObjectsToNodes[component] = this;

                    if (Objects.Count > Tree.MaxObjectsPerNode && Depth < Tree.MaxDepth)
                    {
                        Split();
                    }
                }
            }

            else
            {
                for(int i = 0; i < 8; i++)
                {
                    OctreeNode node = Children[i];
                    if(node != null)
                    {
                        node.AddObjectRecursive(component);
                    }
                }
            }
        }

        public bool ContainsObjectRecursive(IBoundedObject component)
        {
            lock (ObjectLock)
            {
                if (Objects.Contains(component))
                {
                    return true;
                }
                else
                {
                    for (int i = 0; i < 8; i++)
                    {
                        OctreeNode node = Children[i];
                        if (node == null)
                        {
                            continue;
                        }

                        if (node.ContainsObjectRecursive(component))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public bool RemoveObject(IBoundedObject component)
        {
            lock (ObjectLock)
            {
                Objects.Remove(component);

                if (Tree.ObjectsToNodes.ContainsKey(component) && Tree.ObjectsToNodes[component] == this)
                {
                    Tree.ObjectsToNodes.Remove(component);
                }

                if (CountObjectsRecursive() - 1 < Tree.MinObjectsPerNode && HasChildren())
                {
                    MergeRecursive();
                }
                else if (Parent != null && !HasChildren() && CountObjectsRecursive() < Tree.MinObjectsPerNode)
                {
                    Parent.MergeRecursive();
                }

                return true;
            }
        }

        public bool RemoveObjectRecursive(IBoundedObject component)
        {
            lock (ObjectLock)
            {
                if (Objects.Contains(component))
                {
                    return RemoveObject(component);
                }
                else
                {
                    bool toReturn = false;
                    for (int i = 0; i < 8; i++)
                    {
                        OctreeNode node = Children[i];
                        if (node != null)
                        {
                            toReturn = node.RemoveObjectRecursive(component) || toReturn;
                        }
                    }

                    Objects.Remove(component);
                    if (Tree.ObjectsToNodes.ContainsKey(component) && Tree.ObjectsToNodes[component] == this)
                    {
                        Tree.ObjectsToNodes.Remove(component);
                    }

                    return toReturn;
                }
            }
        }

        public List<IBoundedObject> MergeRecursive()
        {
            List<IBoundedObject> toReturn = new List<IBoundedObject>();

            for(int i = 0; i < 8; i++)
            {
                OctreeNode node = Children[i];
                if(node != null)
                {
                    toReturn.AddRange(node.MergeRecursive());
                }
            }

            for(int i = 0; i < 8; i++)
            {
                if(Children[i] != null)
                {
                    lock (Children[i].ObjectLock)
                    {
                        Children[i].Objects.Clear();
                    }
                }
                Children[i] = null;
            }

            List<IBoundedObject> toAdd = new List<IBoundedObject>();
            toAdd.AddRange(toReturn);

            lock (ObjectLock)
            {
                toReturn.AddRange(Objects);
            }

            foreach(IBoundedObject component in toAdd)
            {
                lock(ObjectLock)
                {
                    if(!Objects.Contains(component))
                    {
                        Objects.Add(component);
                        Tree.ObjectsToNodes[component] = this;
                    }
                }
            }

            return toReturn;
        }

        public int CountObjectsRecursive()
        {
            lock (ObjectLock)
            {
                int toReturn = Objects.Count;

                for (int i = 0; i < 8; i++)
                {
                    OctreeNode node = Children[i];
                    if (node != null)
                    {
                        toReturn += node.CountObjectsRecursive();
                    }
                }


                return toReturn;
            }
        }

        public void Split()
        {
            Vector3 extents = Bounds.Max - Bounds.Min;
            Vector3 xExtents = new Vector3(extents.X, 0, 0) / 2.0f;
            Vector3 yExtents = new Vector3(0, extents.Y, 0) / 2.0f;
            Vector3 zExtents = new Vector3(0, 0, extents.Z) / 2.0f;
            Vector3 halfExtents = extents / 2.0f;
            Vector3 center = Bounds.Min + extents / 2.0f;
            for(int i = 0; i < 8; i++)
            {
                NodeType nodeType = (NodeType) i;

                switch(nodeType)
                {
                    case NodeType.LowerBackLeft:
                        Children[i] = new OctreeNode(new BoundingBox(Bounds.Min, center), Tree, Depth + 1, this);
                        break;

                    case NodeType.LowerBackRight:
                        Children[i] = new OctreeNode(new BoundingBox(Bounds.Min + xExtents,
                            Bounds.Min + xExtents + halfExtents), Tree, Depth + 1, this);
                        break;

                    case NodeType.LowerFrontLeft:
                        Children[i] = new OctreeNode(new BoundingBox(Bounds.Min + zExtents,
                            Bounds.Min + halfExtents + zExtents), Tree, Depth + 1, this);
                        break;

                    case NodeType.LowerFrontRight:
                        Children[i] = new OctreeNode(new BoundingBox(Bounds.Min + zExtents + xExtents,
                            Bounds.Min + halfExtents + zExtents + xExtents), Tree, Depth + 1, this);
                        break;

                    case NodeType.UpperBackLeft:
                        Children[i] = new OctreeNode(new BoundingBox(Bounds.Min + yExtents,
                            Bounds.Min + yExtents + halfExtents), Tree, Depth + 1, this);
                        break;

                    case NodeType.UpperBackRight:
                        Children[i] = new OctreeNode(new BoundingBox(Bounds.Min + yExtents + xExtents,
                            Bounds.Min + yExtents + xExtents + halfExtents), Tree, Depth + 1, this);
                        break;

                    case NodeType.UpperFrontLeft:
                        Children[i] = new OctreeNode(new BoundingBox(Bounds.Min + yExtents + zExtents,
                            Bounds.Min + yExtents + zExtents + halfExtents), Tree, Depth + 1, this);
                        break;

                    case NodeType.UpperFrontRight:
                        Children[i] = new OctreeNode(new BoundingBox(Bounds.Min + yExtents + xExtents + zExtents,
                            Bounds.Min + yExtents + zExtents + xExtents + halfExtents), Tree, Depth + 1, this);
                        break;
                }

                lock(ObjectLock)
                {
                    foreach(IBoundedObject o in Objects)
                    {
                        Children[i].AddObjectRecursive(o);
                    }
                }
            }

            lock(ObjectLock)
            {
                Objects.Clear();
            }
        }

        public void Draw(Color color, float width)
        {
            if(Tree.DebugDraw)
            {
                lock (ObjectLock)
                {
                    if (Objects.Count > 0 && !HasChildren())
                    {
                        Drawer3D.DrawBox(Bounds, color, width);
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        OctreeNode child = Children[i];
                        if (child != null)
                        {
                            child.Draw(color, width*0.9f);
                        }
                    }
                }
            }
        }
    }

}