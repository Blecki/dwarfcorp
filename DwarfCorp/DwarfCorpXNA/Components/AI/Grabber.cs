// Grabber.cs
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
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This component represents the "Hands" of a creature. It's a generic way of attaching and detaching objects from each other.
    /// </summary>
    public class Grabber : Body, IUpdateableComponent
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

        public Grabber(string name, ComponentManager Manager, Matrix localTrans, Vector3 boundingboxExtents, Vector3 boundingBoxCenter) :
            base(Manager, name, localTrans, boundingboxExtents, boundingBoxCenter, false)
        {
            GrabbedComponents = new List<GrabbedItem>();
            MaxGrabs = 1;
        }

        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
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
            other.Active = true;
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