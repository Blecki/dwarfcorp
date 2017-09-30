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
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference=true)]
    public class Inventory : Body
    {
        //public ResourceContainer Resources { get; set; }
        public class InventoryItem
        {
            public ResourceLibrary.ResourceType Resource;
            public bool MarkedForRestock;
        }

        public enum RestockType
        {
            RestockResource,
            None
        }
        public List<InventoryItem> Resources { get; set; } 
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
            DropRate = 1.0f;
        }

        public Inventory(ComponentManager Manager, string name, Vector3 BoundingBoxExtents, Vector3 BoundingBoxPos) :
            base(Manager, name, Matrix.Identity, BoundingBoxExtents, BoundingBoxPos)
        {
            DropRate = 1.0f;
            Resources = new List<InventoryItem>();
        }

        public bool Pickup(ResourceAmount resourceAmount, RestockType restock)
        {
            for (int i = 0; i < resourceAmount.NumResources; i++)
            {
                Resources.Add(new InventoryItem()
                {
                    Resource = resourceAmount.ResourceType,
                    MarkedForRestock = restock == RestockType.RestockResource
                });
            }
            return true;
        }

        public bool Remove(IEnumerable<Quantitiy<Resource.ResourceTags>> amount)
        {
            foreach (var quantity in amount)
            {
                for (int i = 0; i < quantity.NumResources; i++)
                {
                    int kRemove = -1;
                    for (int k = 0; k < Resources.Count; k++)
                    {
                        if (!ResourceLibrary.GetResourceByName(Resources[k].Resource)
                            .Tags.Contains(quantity.ResourceType)) continue;
                        kRemove = k;
                        break;
                    }
                    if (kRemove < 0)
                    {
                        return false;
                    }
                    Resources.RemoveAt(kRemove);
                }
            }
            return true;
        }

        public bool Remove(IEnumerable<ResourceAmount> resourceAmount)
        {
            foreach (var quantity in resourceAmount)
            {
                for (int i = 0; i < quantity.NumResources; i++)
                {
                    int kRemove = -1;
                    for (int k = 0; k < Resources.Count; k++)
                    {
                        if (Resources[k].Resource != quantity.ResourceType) continue;
                        kRemove = k;
                        break;
                    }
                    if (kRemove < 0)
                    {
                        return false;
                    }
                    Resources.RemoveAt(kRemove);
                }
            }
            return true;
        }

        public bool Remove(ResourceAmount resourceAmount)
        {
            return Remove(new List<ResourceAmount>() {resourceAmount});
        }

        public bool Pickup(Body component, RestockType restockType)
        {
            return Pickup(Item.CreateItem(null, component), restockType);
        }

        public bool Pickup(Item item, RestockType restockType)
        {
            if(item == null || item.UserData == null || item.UserData.IsDead)
            {
                return false;
            }

            Resources.Add(new InventoryItem()
            {
                MarkedForRestock = restockType == RestockType.RestockResource,
                Resource = item.UserData.Tags[0]
            });
            if(item.IsInZone)
            {
                item.Zone = null;
            }
         
            
            TossMotion toss = new TossMotion(0.5f + MathFunctions.Rand(0.05f, 0.08f),
                1.0f, item.UserData.GlobalTransform, Position);
            item.UserData.AnimationQueue.Add(toss);
            toss.OnComplete += () => item.UserData.GetRoot().Delete();

            return true;
        }

        public List<Body> RemoveAndCreate(ResourceAmount resources)
        {
            List<Body> toReturn = new List<Body>();

            if(!Remove(resources.CloneResource()))
            {
                return toReturn;
            }

            for(int i = 0; i < resources.NumResources; i++)
            {
                Body newEntity = EntityFactory.CreateEntity<Body>(resources.ResourceType + " Resource",
                    GlobalTransform.Translation + MathFunctions.RandVector3Cube()*0.5f);
                toReturn.Add(newEntity);
            }

            return toReturn;
        }


 
        public override void Die()
        {
            List<Body> release = new List<Body>();
            foreach(var resource in Resources)
            {
                if (MathFunctions.RandEvent(DropRate))
                {
                    const int maxIters = 10;

                    for (int i = 0; i < maxIters; i++)
                    {
                        Vector3 pos = MathFunctions.RandVector3Box(GetBoundingBox());
                        var voxel = new VoxelHandle(World.ChunkManager.ChunkData,
                        GlobalVoxelCoordinate.FromVector3(pos));
                        if ((!voxel.IsValid) || !voxel.IsEmpty)
                        {
                            continue;
                        }
                        Physics item =
                            EntityFactory.CreateEntity<Physics>(resource.Resource + " Resource",
                                pos) as Physics;
                        if (item != null)
                        {
                            release.Add(item);
                            item.Velocity = pos - GetBoundingBox().Center();
                            item.Velocity.Normalize();
                            item.Velocity *= 5.0f;
                            item.IsSleeping = false;
                        }
                        break;
                    }
                }
            }

            OnOnDeath(release);
            base.Die();
        }

        public bool HasResource(ResourceAmount itemToStock)
        {
            return Resources.Count(resource => resource.Resource == itemToStock.ResourceType) >= itemToStock.NumResources;
        }

        public bool HasResource(Quantitiy<Resource.ResourceTags> itemToStock, bool allowHeterogenous = false)
        {
            if (allowHeterogenous)
                return Resources.Count(resource => ResourceLibrary.GetResourceByName(resource.Resource).Tags.Contains(itemToStock.ResourceType)) >= itemToStock.NumResources;
            else
            {
                Dictionary<ResourceLibrary.ResourceType, int> resourceCounts = new Dictionary<ResourceLibrary.ResourceType, int>();
                foreach (var resource in Resources)
                {
                    if (ResourceLibrary.GetResourceByName(resource.Resource).Tags.Contains(itemToStock.ResourceType))
                    {
                        if (!resourceCounts.ContainsKey(resource.Resource))
                        {
                            resourceCounts[resource.Resource] = 0;
                        }
                        resourceCounts[resource.Resource]++;
                    }
                }

                return resourceCounts.Count > 0 && resourceCounts.Max(r => r.Value >= itemToStock.NumResources);
            }
        }

        public List<ResourceAmount> GetResources(Quantitiy<Resource.ResourceTags> quantitiy)
        {
            return (from resource in Resources where ResourceLibrary.GetResourceByName(resource.Resource).Tags.Contains(quantitiy.ResourceType) select new ResourceAmount(resource.Resource)).ToList();
        }

        public void AddResource(ResourceAmount tradeGood)
        {
            Pickup(tradeGood, RestockType.RestockResource);
        }
    }
}
