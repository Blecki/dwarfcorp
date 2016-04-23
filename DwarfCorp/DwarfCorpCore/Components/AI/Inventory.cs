// Inventory.cs
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
using System.ComponentModel;
using System.Linq;
using System.Management.Instrumentation;
using System.Security.AccessControl;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference=true)]
    public class Inventory : Body
    {
        public ResourceContainer Resources { get; set; }
        public float DropRate { get; set; }

        public delegate void DieDelegate(List<Body> items);

        public event DieDelegate OnDeath;

        protected virtual void OnOnDeath(List<Body> items)
        {
            DieDelegate handler = OnDeath;
            if (handler != null) handler(items);
        }

        public Inventory()
        {
            DropRate = 0.75f;
        }

        public Inventory(string name, Body parent) :
            base(name, parent, Matrix.Identity, parent.BoundingBox.Extents(), parent.BoundingBoxPos)
        {
            DropRate = 0.75f;
        }

        public bool Pickup(ResourceAmount resourceAmount)
        {
            return Resources.AddResource(resourceAmount);
        }

        public bool Remove(IEnumerable<Quantitiy<Resource.ResourceTags>> amount)
        {
            return amount.Aggregate(true, (current, resource) => current && Resources.RemoveResource(resource));
        }

        public bool Remove(IEnumerable<ResourceAmount> resourceAmount)
        {
            return resourceAmount.Aggregate(true, (current, resource) => current && Resources.RemoveResource(resource));
        }

        public bool Remove(ResourceAmount resourceAmount)
        {
            return Resources.RemoveResource(resourceAmount);
        }

        public bool Pickup(Body component)
        {
            return Pickup(Item.CreateItem(null, component));
        }

        public bool Pickup(Item item)
        {
            if(item == null || item.UserData == null || item.UserData.IsDead)
            {
                return false;
            }

            bool success =  Resources.AddResource
            (new ResourceAmount
            {
                NumResources = 1,
                ResourceType = ResourceLibrary.GetResourceByName(item.UserData.Tags[0])
            });

            if(!success)
            {
                return false;
            }

            if(item.IsInZone)
            {
                item.Zone = null;
            }
            item.UserData.GetRootComponent().Delete();

            return true;
        }

        public List<Body> RemoveAndCreate(ResourceAmount resources)
        {
            List<Body> toReturn = new List<Body>();

            if(!Resources.RemoveResource(resources.CloneResource()))
            {
                return toReturn;
            }

            for(int i = 0; i < resources.NumResources; i++)
            {
                Body newEntity = EntityFactory.CreateEntity<Body>(resources.ResourceType.ResourceName + " Resource",
                    GlobalTransform.Translation + MathFunctions.RandVector3Cube()*0.5f);
                toReturn.Add(newEntity);
            }

            return toReturn;
        }


 
        public override void Die()
        {
            List<Body> release = new List<Body>();
            foreach(var resource in Resources.Where(resource => resource.NumResources > 0))
            {
                for(int i = 0; i < resource.NumResources; i++)
                {
                    if (MathFunctions.RandEvent(DropRate))
                    {
                        Vector3 pos = MathFunctions.RandVector3Box(GetBoundingBox());
                        Physics item =
                            EntityFactory.CreateEntity<Physics>(resource.ResourceType.ResourceName + " Resource",
                                pos) as Physics;
                        if (item != null)
                        {
                            release.Add(item);
                            item.Velocity = pos - GetBoundingBox().Center();
                            item.Velocity.Normalize();
                            item.Velocity *= 5.0f;
                            item.IsSleeping = false;
                        }
                    }

                }
            }

            OnOnDeath(release);
            base.Die();
        }
    }
}
