using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class Grabber : LocatableComponent
    {
        public Dictionary<LocatableComponent, Matrix> GrabbedComponents { get; set; }
        public int MaxGrabs { get; set; }

        public Grabber(ComponentManager manager, string name, GameComponent parent, Matrix localTrans, Vector3 boundingboxExtents, Vector3 boundingBoxCenter):
            base(manager, name, parent, localTrans, boundingboxExtents, boundingBoxCenter, false)
        {
            GrabbedComponents = new Dictionary<LocatableComponent, Matrix>();
            MaxGrabs = 1;
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            foreach (LocatableComponent grabbed in GrabbedComponents.Keys)
            {
                grabbed.GlobalTransform = GrabbedComponents[grabbed] * GlobalTransform;
            }

            base.Update(gameTime, chunks, camera);
        }

        public bool IsFull()
        {
            return GrabbedComponents.Count >= MaxGrabs;
        }

        public override void Die()
        {
            List<LocatableComponent> removals = new List<LocatableComponent>();
            removals.AddRange(GrabbedComponents.Keys);

            foreach (LocatableComponent r in removals)
            {
                UnGrab(r);
            }

            base.Die();
        }

        public bool Grab(LocatableComponent other)
        {
            if (!GrabbedComponents.ContainsKey(other) && GrabbedComponents.Count < MaxGrabs)
            {
                Matrix m = Matrix.Identity;
                m = GlobalTransform;
                m.Translation = GlobalTransform.Translation + new Vector3(0, 0.0f, 0.5f);
                other.GlobalTransform = m;
                GrabbedComponents[other] = Matrix.Invert(GlobalTransform) * other.GlobalTransform;
                other.LocalTransform = GrabbedComponents[other];
                AddChild(other);
                return true;
            }
            else return false;
        }

        public bool UnGrab(LocatableComponent other)
        {
            if (GrabbedComponents.ContainsKey(other))
            {
                GrabbedComponents.Remove(other);
                RemoveChild(other);
                other.IsActive = true;
                other.Parent = Manager.RootComponent;
                other.LocalTransform = other.GlobalTransform;
                return true;
            }

            return false;
        }

        public void UngrabFirst(Vector3 position)
        {
            LocatableComponent grabbed = GetFirstGrab();
            UnGrab(grabbed);
            Matrix m = Matrix.Identity;
            m.Translation = position;
            grabbed.LocalTransform = m;
        }

        public LocatableComponent GetFirstGrab()
        {
            if (GrabbedComponents.Count <= 0)
            {
                return null;
            }

            else return GrabbedComponents.Keys.ElementAt<LocatableComponent>(0);
        }

      


    }
}
