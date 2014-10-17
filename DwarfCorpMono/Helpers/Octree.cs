using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DwarfCorp
{

    public interface BoundedObject
    {
        BoundingBox GetBoundingBox();
        BoundingSphere GetBoundingSphere();
        uint GetID();
    }

    public class Octree
    {

        public int MaxDepth { get; set; }
        public int MaxObjectsPerNode { get; set; }
        public int MinObjectsPerNode { get; set; }
        public BoundingBox Bounds { get; set; }
        public OctreeNode Root { get; set; }
        public bool DebugDraw { get; set; }
        public ReaderWriterLockSlim Lock { get; set; }
        public Dictionary<BoundedObject, OctreeNode> ObjectsToNodes { get; set; }
        protected Dictionary<BoundedObject, bool> ObjectsToUpdate { get; set; }
        public Timer UpdateTimer { get; set; }

        public Octree(BoundingBox bounds, int maxDepth, int maxObjectsPerNode, int minObjectsPerNode)
        {
            Bounds = bounds;
            MaxDepth = maxDepth;
            MaxObjectsPerNode = maxObjectsPerNode;
            MinObjectsPerNode = minObjectsPerNode;
            Root = new OctreeNode(Bounds, this, 0, null);
            DebugDraw = false;
            Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            ObjectsToNodes = new Dictionary<BoundedObject, OctreeNode>();
            ObjectsToUpdate = new Dictionary<BoundedObject, bool>();
            UpdateTimer = new Timer(1.0f, false);
        }

        public void AddUpdate(BoundedObject obj)
        {
            lock (ObjectsToUpdate)
            {
                ObjectsToUpdate[obj] = true;
            }
        }

        public void AddObjectRecursive(BoundedObject obj)
        {
            if (ObjectsToNodes.ContainsKey(obj))
            {
                return;
            }
            else
            {
                Root.AddObjectRecursive(obj);
            }
        }

        public bool RemoveObject(BoundedObject obj)
        {
            if (!ObjectsToNodes.ContainsKey(obj))
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
            List<BoundedObject> objectsInTree = Root.MergeRecursive();

            BoundingBox bounds = new BoundingBox(Bounds.Min * 2.0f, Bounds.Max * 2.0f);
            Bounds = bounds;

            
            Root = new OctreeNode(Bounds, this, 0, null);

            foreach (BoundedObject loc in objectsInTree)
            {
                AddObjectRecursive(loc);
            }

            Lock.ExitWriteLock();
        }

        public bool NeedsUpdate(LocatableComponent component)
        {
            if (this.ObjectsToUpdate.ContainsKey(component))
            {
                return false;
            }

            if (!ObjectsToNodes.ContainsKey(component))
            {
                return true;
            }
            else
            {
                return ObjectsToNodes[component].Bounds.Intersects(component.GetBoundingBox());
            }
        }

        public void Update(GameTime time)
        {
            UpdateTimer.Update(time);

            if (UpdateTimer.HasTriggered)
            {
                lock (ObjectsToUpdate)
                {
                    foreach (KeyValuePair<BoundedObject, bool> pair in ObjectsToUpdate)
                    {
                        RemoveObject(pair.Key);
                        AddObjectRecursive(pair.Key);
                    }

                    ObjectsToUpdate.Clear();
                }
            }
        }
    }

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

        public OctreeNode[] Children = { null, null, null, null, null, null, null, null };

        private BoundingBox m_bounds;
        public BoundingBox Bounds 
        {    get
            {
                return m_bounds;
            }
            set 
            { 
                m_bounds = value;
                Vector3 ext = m_bounds.Max - m_bounds.Min;
                float m = Math.Max(Math.Max(ext.X, ext.Y), ext.Z) * 0.5f;
                BoundingSphere = new BoundingSphere((m_bounds.Max + m_bounds.Min) * 0.5f, (float)Math.Sqrt(3 * m * m));
            }
        }
        public BoundingSphere BoundingSphere { get; set; }

        public List<BoundedObject> Objects { get; set; }

        public Octree Tree { get; set; }

        public OctreeNode Parent { get; set; }

        public int Depth { get; set; }


        public bool HasChild(NodeType node)
        {
            bool toReturn =  Children[(int)node] != null;
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


        public OctreeNode(BoundingBox bounds, Octree tree, int depth, OctreeNode parent)
        {
            Bounds = bounds;
            Objects = new List<BoundedObject>();
            Tree = tree;
            Parent = parent;
            Depth = depth;
     
        }


        public T GetComponentIntersecting<T>(Vector3 vect) where T : BoundedObject
        {
            if (Bounds.Contains(vect) != ContainmentType.Disjoint)
            {
                for (int i = 0; i < Objects.Count; i++)
                {
                    BoundedObject o = Objects[i];
                    if (o is T && o.GetBoundingBox().Contains(vect) != ContainmentType.Disjoint)
                    {
                        return (T)o;
                    }
                }

                for (int i = 0; i < 8; i++)
                {
                    OctreeNode child = Children[i];
                    if (child != null)
                    {
                        T got = child.GetComponentIntersecting<T>(vect);

                        if (got != null)
                        {
                            
                            return got;
                        }
                    }
                }
            }
            return default(T);
        }

        public void GetComponentsIntersecting<T>(BoundingBox box, HashSet<T> set) where T : BoundedObject
        {
            Stack<OctreeNode> stack = new Stack<OctreeNode>();
            stack.Push(this);

            while (stack.Count > 0)
            {
                OctreeNode t = stack.Pop();
                if (t.Bounds.Intersects(box))
                {

                    for (int i = 0; i < t.Objects.Count; i++)
                    {
                        BoundedObject o = t.Objects[i];
                        if (o is T && o.GetBoundingBox().Intersects(box))
                        {
                            set.Add((T)o);
                        }
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

            }
            
        }


        public void GetComponentsIntersecting<T>(BoundingFrustum box, HashSet<T> set) where T : BoundedObject
        {
            Stack<OctreeNode> stack = new Stack<OctreeNode>();
            stack.Push(this);

            while (stack.Count > 0)
            {
                OctreeNode t = stack.Pop();
                if (t.Bounds.Intersects(box))
                {

                    for (int i = 0; i < t.Objects.Count; i++)
                    {
                        BoundedObject o = t.Objects[i];
                        if (o is T && o.GetBoundingBox().Intersects(box))
                        {
                            set.Add((T)o);
                        }
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

        
            }

            

        }

        public void GetComponentsIntersecting<T>(BoundingSphere box, HashSet<T> set) where T : BoundedObject
        {
            Stack<OctreeNode> stack = new Stack<OctreeNode>();
            stack.Push(this);

            while (stack.Count > 0)
            {
                OctreeNode t = stack.Peek();
                if (t.Bounds.Intersects(box))
                {
                    for (int i = 0; i < t.Objects.Count; i++)
                    {
                        BoundedObject o = t.Objects[i];
                        if (o is T && o.GetBoundingBox().Intersects(box))
                        {
                            set.Add((T)o);
                        }
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

                stack.Pop();
            }

            

        }

        public void GetComponentsIntersecting<T>(Ray box, HashSet<T> set) where T : BoundedObject
        {
            Stack<OctreeNode> stack = new Stack<OctreeNode>();
            stack.Push(this);

            while (stack.Count > 0)
            {
                OctreeNode t = stack.Pop();
                if (t.Bounds.Intersects(box) != null)
                {
                    for (int i = 0; i < t.Objects.Count; i++)
                    {
                        BoundedObject o = t.Objects[i];
                        if (o is T && o.GetBoundingBox().Intersects(box) != null)
                        {
                            set.Add((T)o);
                        }
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

            }
            
        }

        public bool ExistsInTreeRecursive(BoundedObject component)
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
                    if (node != null)
                    {
                        if (node.ExistsInTreeRecursive(component))
                        {
                            
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        public bool NeedsUpdateRecursive(BoundedObject component)
        {
            if (Objects.Contains(component))
            {
                if (!component.GetBoundingBox().Intersects(Bounds))
                {
                    
                    return true;
                }
                else
                {
                    
                    return false;
                }
            }
            else if (HasChildren())
            {
                bool shouldUpdate = false;
                for(int i = 0; i < 8; i++)
                {
                    OctreeNode node = Children[i];
                    if (node != null)
                    {
                        if (node.NeedsUpdateRecursive(component))
                        {
                            shouldUpdate = true;
                        }
                    }
                }

                
                return shouldUpdate;
            }
            else
            {
                
                return false;
            }
            
            
        }

        public void AddObjectRecursive(BoundedObject component)
        {
            if (Parent == null && !component.GetBoundingBox().Intersects(Bounds))
            {
                Tree.ExpandAndRebuild();
                return;
            }

            if (component.GetBoundingBox().Intersects(Bounds) && !HasChildren())
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

            else
            {
                for(int i = 0; i < 8; i++)
                {
                    OctreeNode node = Children[i];
                    if (node != null)
                    {
                        node.AddObjectRecursive(component);
                    }

                }
            }


        }

        public bool ContainsObjectRecursive(BoundedObject component)
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
                    if (node != null)
                    {
                        if (node.ContainsObjectRecursive(component))
                        {
                            
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        public bool RemoveObject(BoundedObject component)
        {
            if (CountObjectsRecursive() < Tree.MinObjectsPerNode && HasChildren())
            {
                MergeRecursive();
            }
            else if (Parent != null && !HasChildren() && CountObjectsRecursive() < Tree.MinObjectsPerNode)
            {
                Parent.MergeRecursive();
            }



            Objects.Remove(component);

            if (Tree.ObjectsToNodes.ContainsKey(component) && Tree.ObjectsToNodes[component] == this)
            {
                Tree.ObjectsToNodes.Remove(component);
            }

            return true;
        }

        public bool RemoveObjectRecursive(BoundedObject component)
        {
            if (Objects.Contains(component))
            {
                return RemoveObject(component);
            }
            else
            {
                bool toReturn = false;
                for(int i = 0; i < 8; i++)
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

        public List<BoundedObject> MergeRecursive()
        {
            List<BoundedObject> toReturn = new List<BoundedObject>();

            for (int i = 0; i < 8; i++)
            {
                OctreeNode node = Children[i];
                if (node != null)
                {
                    toReturn.AddRange(node.MergeRecursive());
                }
            }

            for (int i = 0; i < 8; i++)
            {
                if (Children[i] != null)
                {
                    Children[i].Objects.Clear();
                }
                Children[i] = null;
            }

            List<BoundedObject> toAdd = new List<BoundedObject>();
            toAdd.AddRange(toReturn);


            toReturn.AddRange(Objects);

            foreach(BoundedObject component in toAdd)
            {
                if (!Objects.Contains(component))
                {
                    Objects.Add(component);
                    Tree.ObjectsToNodes[component] = this;
                }
            }

            return toReturn;
        }

        public int CountObjectsRecursive()
        {
            
            int toReturn = Objects.Count;

            for (int i = 0; i < 8; i++)
            {
                OctreeNode node = Children[i];
                if(node != null)
                    toReturn += node.CountObjectsRecursive();
            }

            
            return toReturn;
        }

        public void Split()
        {
            Vector3 extents = Bounds.Max - Bounds.Min;
            Vector3 xExtents = new Vector3(extents.X, 0, 0) / 2.0f;
            Vector3 yExtents = new Vector3(0, extents.Y, 0) / 2.0f;
            Vector3 zExtents = new Vector3(0, 0, extents.Z) / 2.0f;
            Vector3 halfExtents = extents / 2.0f;
            Vector3 center = Bounds.Min + extents / 2.0f;
            for (int i = 0; i < 8; i++)
            {
                NodeType nodeType = (NodeType)i;

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



                for (int j = 0; j < Objects.Count; j++)
                {
                    BoundedObject o = Objects[j];
                    Children[i].AddObjectRecursive(o);
                }
                
            }

            Objects.Clear();
            

        }

        public void Draw()
        {
            if (Tree.DebugDraw)
            {
                if (Objects.Count > 0 && !HasChildren())
                {
                    SimpleDrawing.DrawBox(Bounds, Color.White, 0.01f);
                }

                for (int i = 0; i < 8; i++)
                {
                    OctreeNode child = Children[i];
                    if (child != null)
                    {
                        child.Draw();
                    }
                }
            }

        }
    }
}