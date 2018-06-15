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
            public ResourceType Resource;
            public bool MarkedForRestock = false;
            public bool MarkedForUse = false;
        }

        public enum RestockType
        {
            RestockResource,
            None,
            Any
        }
        public List<InventoryItem> Resources { get; set; } 
        public float DropRate { get; set; }

        [JsonIgnore]
        private CreatureAI Attacker = null;

        public void SetLastAttacker(CreatureAI Creature)
        {
            Attacker = Creature;
        }

        public Inventory()
        {
            DropRate = 1.0f;
        }

        public Inventory(ComponentManager Manager, string name, Vector3 BoundingBoxExtents, Vector3 LocalBoundingBoxOffset) :
            base(Manager, name, Matrix.Identity, BoundingBoxExtents, LocalBoundingBoxOffset)
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
                    MarkedForRestock = restock == RestockType.RestockResource,
                    MarkedForUse = restock != RestockType.RestockResource
                });
            }
            return true;
        }

        public bool Remove(IEnumerable<Quantitiy<Resource.ResourceTags>> amount, RestockType type)
        {
            foreach (var quantity in amount)
            {
                for (int i = 0; i < quantity.NumResources; i++)
                {
                    int kRemove = -1;
                    for (int k = 0; k < Resources.Count; k++)
                    {
                        if (type == RestockType.None && Resources[k].MarkedForRestock)
                            continue;
                        else if (type == RestockType.RestockResource && !Resources[k].MarkedForRestock)
                            continue;

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

        public bool Remove(IEnumerable<ResourceAmount> resourceAmount, RestockType type)
        {
            foreach (var quantity in resourceAmount)
            {
                for (int i = 0; i < quantity.NumResources; i++)
                {
                    int kRemove = -1;
                    for (int k = 0; k < Resources.Count; k++)
                    {
                        if (type == RestockType.None && Resources[k].MarkedForRestock)
                            continue;
                        else if (type == RestockType.RestockResource && !Resources[k].MarkedForRestock)
                            continue;
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

        public bool Remove(ResourceAmount resourceAmount, RestockType type)
        {
            return Remove(new List<ResourceAmount>() {resourceAmount}, type);
        }


        public bool Pickup(Body item, RestockType restockType)
        {
            if(item == null || item.IsDead)
            {
                return false;
            }

            if (item is ResourceEntity)
            {
                ResourceEntity entity = item as ResourceEntity;
                for (int i = 0; i < entity.Resource.NumResources; i++)
                {
                    Resources.Add(new InventoryItem()
                    {
                        MarkedForRestock = restockType == RestockType.RestockResource,
                        MarkedForUse = restockType != RestockType.RestockResource,
                        Resource = entity.Resource.ResourceType
                    });
                }
            }
            else
            {
                Resources.Add(new InventoryItem()
                {
                    MarkedForRestock = restockType == RestockType.RestockResource,
                    MarkedForUse = restockType != RestockType.RestockResource,
                    Resource = item.Tags[0]
                });
            }

            item.SetFlag(Flag.Active, false);
            TossMotion toss = new TossMotion(0.5f + MathFunctions.Rand(0.05f, 0.08f),
                1.0f, item.GlobalTransform, Position);
            item.AnimationQueue.Add(toss);
            toss.OnComplete += () => item.GetRoot().Delete();

            return true;
        }

        public bool RemoveAndCreateWithToss(List<ResourceAmount> resources, Vector3 pos, RestockType type)
        {
            bool createdAny = false;
            foreach (var resource in resources)
            {
                List<Body> things = RemoveAndCreate(resource, type);
                foreach (var body in things)
                {
                    TossMotion toss = new TossMotion(1.0f, 2.5f, body.LocalTransform, pos);
                    body.GetRoot().GetComponent<Physics>().CollideMode = Physics.CollisionMode.None;
                    body.AnimationQueue.Add(toss);
                    toss.OnComplete += body.Delete;
                    createdAny = true;
                }
            }
            return createdAny;
        }

        public List<Body> RemoveAndCreate(ResourceAmount resources, RestockType type)
        {
            List<Body> toReturn = new List<Body>();

            if(!Remove(resources.CloneResource(), type))
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

        internal Dictionary<string, ResourceAmount> Aggregate()
        {
            Dictionary<string, ResourceAmount> toReturn = new Dictionary<string, ResourceAmount>();
            foreach(var resource in Resources)
            {
                if (toReturn.ContainsKey(resource.Resource))
                {
                    toReturn[resource.Resource].NumResources++;
                }
                else
                {
                    toReturn.Add(resource.Resource, new ResourceAmount(resource.Resource));
                }
            }
            return toReturn;
        }

        public override void Die()
        {
            if (Active)
            {
                var resourceCounts = new Dictionary<ResourceType, int>();
                foreach (var resource in Resources)
                {
                    if (!resourceCounts.ContainsKey(resource.Resource))
                    {
                        resourceCounts[resource.Resource] = 0;
                    }
                    resourceCounts[resource.Resource]++;
                }

                var aggregatedResources = resourceCounts.Select(c => new ResourceAmount(c.Key, c.Value));
                var piles = EntityFactory.CreateResourcePiles(aggregatedResources, GetBoundingBox()).ToList();

                if (Attacker != null && !Attacker.IsDead)
                    foreach (var item in piles)
                        Attacker.Creature.Gather(item);
                //else
                //    foreach (var item in piles)
                //        World.Master.TaskManager.AddTask(new GatherItemTask(item));
            }

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
                Dictionary<ResourceType, int> resourceCounts = new Dictionary<ResourceType, int>();
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

        public List<ResourceAmount> GetResources(Quantitiy<Resource.ResourceTags> quantitiy, RestockType type = RestockType.RestockResource)
        {
            return (from resource in Resources where
                    ResourceLibrary.GetResourceByName(resource.Resource).Tags.Contains(quantitiy.ResourceType) && ((type == RestockType.RestockResource 
                    && resource.MarkedForRestock) || (type == RestockType.None && !resource.MarkedForRestock) || (type == RestockType.Any))
                    select new ResourceAmount(resource.Resource)).ToList();
        }

        public void AddResource(ResourceAmount tradeGood, RestockType type = RestockType.RestockResource)
        {
            Pickup(tradeGood, type);
        }
    }
}
