using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Dynamic = 4
        }

        public Dictionary<CollisionType, Octree> Octrees { get; set; }

        public CollisionManager()
        {
            
        }

        public CollisionManager(BoundingBox bounds)
        {
            Octrees = new Dictionary<CollisionType, Octree>();
            Octrees[CollisionType.Static] = new Octree(bounds, 16, 20, 3);
            Octrees[CollisionType.Dynamic] = new Octree(bounds, 16, 5, 1);
        }

        public void AddObject(IBoundedObject bounded, CollisionType type)
        {
            if(type == CollisionType.None)
            {
                return;
            }

            Octrees[type].AddObjectRecursive(bounded);
        }

        public void RemoveObject(IBoundedObject bounded, CollisionType type)
        {
            if(type == CollisionType.None)
            {
                return;
            }

            Octrees[type].RemoveObject(bounded);
        }

        public void UpdateObject(IBoundedObject bounded, CollisionType type)
        {
            if(type == CollisionType.None)
            {
                return;
            }

            Octrees[type].AddUpdate(bounded);
        }

        public bool NeedsUpdate(IBoundedObject bounded, CollisionType type)
        {
            return type != CollisionType.None && Octrees[type].NeedsUpdate(bounded);
        }

        public void Update(GameTime t)
        {
            foreach(var octree in Octrees)
            {
                octree.Value.Update(t);
            }
        }


        public void GetObjectsIntersecting<TObject>(BoundingBox box, HashSet<TObject> set, CollisionType queryType) where TObject : IBoundedObject
        {
            switch((int) queryType)
            {
                case (int) CollisionType.Static:
                case (int) CollisionType.Dynamic:
                    Octrees[queryType].Root.GetComponentsIntersecting(box, set);
                    break;
                case ((int) CollisionType.Static | (int) CollisionType.Dynamic):
                    Octrees[CollisionType.Static].Root.GetComponentsIntersecting(box, set);
                    Octrees[CollisionType.Dynamic].Root.GetComponentsIntersecting(box, set);
                    break;
            }
        }

        public void GetObjectsIntersecting<TObject>(BoundingFrustum box, HashSet<TObject> set, CollisionType queryType) where TObject : IBoundedObject
        {
            switch((int) queryType)
            {
                case (int) CollisionType.Static:
                case (int) CollisionType.Dynamic:
                    Octrees[queryType].Root.GetComponentsIntersecting(box, set);
                    break;
                case ((int) CollisionType.Static | (int) CollisionType.Dynamic):
                    Octrees[CollisionType.Static].Root.GetComponentsIntersecting(box, set);
                    Octrees[CollisionType.Dynamic].Root.GetComponentsIntersecting(box, set);
                    break;
            }
        }

        public void GetObjectsIntersecting<TObject>(Ray box, HashSet<TObject> set, CollisionType queryType) where TObject : IBoundedObject
        {
            switch((int) queryType)
            {
                case (int) CollisionType.Static:
                case (int) CollisionType.Dynamic:
                    Octrees[queryType].Root.GetComponentsIntersecting(box, set);
                    break;
                case ((int) CollisionType.Static | (int) CollisionType.Dynamic):
                    Octrees[CollisionType.Static].Root.GetComponentsIntersecting(box, set);
                    Octrees[CollisionType.Dynamic].Root.GetComponentsIntersecting(box, set);
                    break;
            }
        }

        public void GetObjectsIntersecting<TObject>(BoundingSphere box, HashSet<TObject> set, CollisionType queryType) where TObject : IBoundedObject
        {
            switch((int) queryType)
            {
                case (int) CollisionType.Static:
                case (int) CollisionType.Dynamic:
                    Octrees[queryType].Root.GetComponentsIntersecting(box, set);
                    break;
                case ((int) CollisionType.Static | (int) CollisionType.Dynamic):
                    Octrees[CollisionType.Static].Root.GetComponentsIntersecting(box, set);
                    Octrees[CollisionType.Dynamic].Root.GetComponentsIntersecting(box, set);
                    break;
            }
        }

        public void DebugDraw()
        {
            Color[] colors =
            {
                Color.Red,
                Color.Green
            };

            int i = 0;
            foreach(var octree in Octrees)
            {
                octree.Value.DebugDraw = true;
                octree.Value.Root.Draw(colors[i], 0.1f);
                i++;
            }
        }
    }

}