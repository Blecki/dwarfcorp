using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This component represents the "Hands" of a creature. It's a generic way of attaching and detaching objects from each other.
    /// </summary>
    public class Grabber : Body
    {
        public struct GrabbedItem
        {
            public Body Component;
            public Matrix LocalTransform;
        }

        public List<GrabbedItem> GrabbedComponents { get; set; }
        public int MaxGrabs { get; set; }

        public Grabber()
        {
            
        }

        public Grabber(string name, GameComponent parent, Matrix localTrans, Vector3 boundingboxExtents, Vector3 boundingBoxCenter) :
            base(name, parent, localTrans, boundingboxExtents, boundingBoxCenter, false)
        {
            GrabbedComponents = new List<GrabbedItem>();
            MaxGrabs = 1;
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            foreach(GrabbedItem grabbed in GrabbedComponents)
            {
                grabbed.Component.GlobalTransform = grabbed.LocalTransform * GlobalTransform;
            }

            base.Update(gameTime, chunks, camera);
        }

        public bool IsFull()
        {
            return GrabbedComponents.Count >= MaxGrabs;
        }

        public override void Die()
        {
            foreach(GrabbedItem item in GrabbedComponents)
            {
                UnGrab(item.Component);
            }

            base.Die();
        }

        public bool IsGrabbed(Body component)
        {
            return GrabbedComponents.Any(item => item.Component == component);
        }

        public bool Grab(Body other)
        {
            if(!IsGrabbed(other) && GrabbedComponents.Count < MaxGrabs)
            {
                Matrix m = Matrix.Identity;
                m = GlobalTransform;
                m.Translation = GlobalTransform.Translation + new Vector3(0, 0.0f, 0.5f);
                other.GlobalTransform = m;

                GrabbedItem item = new GrabbedItem
                {
                    Component = other,
                    LocalTransform = Matrix.Invert(GlobalTransform) * other.GlobalTransform
                };

                GrabbedComponents.Add(item);

                other.LocalTransform = item.LocalTransform;
                AddChild(other);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void RemoveComponent(Body component)
        {
            int i = 0;
            int index = -1;
            foreach(GrabbedItem grabbed in GrabbedComponents)
            {
                if(grabbed.Component == component)
                {
                    index = i;
                    break;
                }
                i++;
            }

            if(index >= 0)
            {
                GrabbedComponents.RemoveAt(index);
            }
        }

        public bool UnGrab(Body other)
        {
            if(!IsGrabbed(other))
            {
                return false;
            }

            RemoveComponent(other);
            RemoveChild(other);
            other.IsActive = true;
            other.Parent = Manager.RootComponent;
            other.LocalTransform = other.GlobalTransform;
            return true;
        }

        public void UngrabFirst(Vector3 position)
        {
            Body grabbed = GetFirstGrab();
            UnGrab(grabbed);
            Matrix m = Matrix.Identity;
            m.Translation = position;
            grabbed.LocalTransform = m;
        }

        public Body GetFirstGrab()
        {
            return GrabbedComponents.Count <= 0 ? null : GrabbedComponents.First().Component;
        }
    }

}