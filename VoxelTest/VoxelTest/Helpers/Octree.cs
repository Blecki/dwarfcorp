using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Threading;

namespace DwarfCorp
{

    public class Octree
    {
        public int MaxDepth { get; set; }
        public int MaxObjectsPerNode { get; set; }
        public int MinObjectsPerNode { get; set; }
        public BoundingBox Bounds { get; set; }
        public OctreeNode Root { get; set; }
        public bool DebugDraw { get; set; }
        public ReaderWriterLockSlim Lock { get; set; }
        public Dictionary<IBoundedObject, OctreeNode> ObjectsToNodes { get; set; }
        protected Dictionary<IBoundedObject, bool> ObjectsToUpdate { get; set; }
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
            ObjectsToNodes = new Dictionary<IBoundedObject, OctreeNode>();
            ObjectsToUpdate = new Dictionary<IBoundedObject, bool>();
            UpdateTimer = new Timer(1.0f, false);
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

        public void Update(GameTime time)
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