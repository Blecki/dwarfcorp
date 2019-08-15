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
    public class Inventory : GameComponent
    {
        //public ResourceContainer Resources { get; set; }
        public class InventoryItem
        {
            public String Resource;
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

        public ResourceContainer ContentsAsResourceContainer()
        {
            var r = new ResourceContainer { MaxResources = int.MaxValue };
            foreach (var item in Resources)
                r.AddResource(new ResourceAmount(item.Resource, 1));
            return r;
        }

        [JsonIgnore]
        private CreatureAI Attacker = null;

        public void SetLastAttacker(CreatureAI Creature)
        {
            Attacker = Creature;
        }

        public Inventory()
        {
        }

        public Inventory(ComponentManager Manager, string name, Vector3 BoundingBoxExtents, Vector3 LocalBoundingBoxOffset) :
            base(Manager, name, Matrix.Identity, BoundingBoxExtents, LocalBoundingBoxOffset)
        {
            DropRate = 1.0f;
            Resources = new List<InventoryItem>();
            CollisionType = CollisionType.None;
        }

        public bool Remove(IEnumerable<Quantitiy<Resource.ResourceTags>> amount, RestockType type)
        {
            foreach (var quantity in amount)
                for (int i = 0; i < quantity.Count; i++)
                {
                    int kRemove = -1;
                    for (int k = 0; k < Resources.Count; k++)
                    {
                        if (type == RestockType.None && Resources[k].MarkedForRestock)
                            continue;
                        else if (type == RestockType.RestockResource && !Resources[k].MarkedForRestock)
                            continue;

                        if (!Library.GetResourceType(Resources[k].Resource).HasValue(out var res) || !res.Tags.Contains(quantity.Type)) continue;
                        kRemove = k;
                        break;
                    }

                    if (kRemove < 0)
                        return false;

                    Resources.RemoveAt(kRemove);
                }

            return true;
        }

        public bool Remove(IEnumerable<ResourceAmount> resourceAmount, RestockType type)
        {
            foreach (var quantity in resourceAmount)
                if (!Remove(quantity, type))
                    return false;
            return true;
        }

        public bool Remove(ResourceAmount resourceAmount, RestockType type)
        {
            for (int i = 0; i < resourceAmount.Count; i++)
            {
                var kRemove = -1;
                for (int k = 0; k < Resources.Count; k++)
                {
                    if (type == RestockType.None && Resources[k].MarkedForRestock)
                        continue;
                    else if (type == RestockType.RestockResource && !Resources[k].MarkedForRestock)
                        continue;
                    if (Resources[k].Resource != resourceAmount.Type) continue;
                    kRemove = k;
                    break;
                }

                if (kRemove < 0)
                    return false;

                Resources.RemoveAt(kRemove);
            }

            return true;
        }


        public bool Pickup(GameComponent item, RestockType restockType)
        {
            if(item == null || item.IsDead)
            {
                return false;
            }

            if (item is ResourceEntity)
            {
                ResourceEntity entity = item as ResourceEntity;
                for (int i = 0; i < entity.Resource.Count; i++)
                {
                    Resources.Add(new InventoryItem()
                    {
                        MarkedForRestock = restockType == RestockType.RestockResource,
                        MarkedForUse = restockType != RestockType.RestockResource,
                        Resource = entity.Resource.Type
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
            BodyTossMotion toss = new BodyTossMotion(0.5f + MathFunctions.Rand(0.05f, 0.08f),
                1.0f, item.GlobalTransform, Parent as GameComponent);
            item.AnimationQueue.Add(toss);
            toss.OnComplete += () => item.GetRoot().Delete();

            return true;
        }

        public bool RemoveAndCreateWithToss(List<ResourceAmount> resources, Vector3 pos, RestockType type)
        {
            bool createdAny = false;
            foreach (var resource in resources)
            {
                List<GameComponent> things = RemoveAndCreate(resource, type);
                foreach (var body in things)
                {
                    TossMotion toss = new TossMotion(1.0f, 2.5f, body.LocalTransform, pos);

                    if (body.GetRoot().GetComponent<Physics>().HasValue(out var physics))
                        physics.CollideMode = Physics.CollisionMode.None;

                    body.AnimationQueue.Add(toss);
                    toss.OnComplete += body.Delete;
                    createdAny = true;
                }
            }
            return createdAny;
        }

        public List<GameComponent> RemoveAndCreate(ResourceAmount resources, RestockType type)
        {
            var parentBody = GetRoot() as GameComponent;
            var pos = parentBody == null ? GlobalTransform.Translation : parentBody.Position;
            List<GameComponent> toReturn = new List<GameComponent>();

            if(!Remove(resources.CloneResource(), type))
                return toReturn;

            for(int i = 0; i < resources.Count; i++)
            {
                GameComponent newEntity = EntityFactory.CreateEntity<GameComponent>(resources.Type + " Resource",
                    pos + MathFunctions.RandVector3Cube()*0.5f);
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
                    toReturn[resource.Resource].Count++;
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
                DropAll();
            }

            base.Die();
        }

        public void DropAll()
        {
            var resourceCounts = new Dictionary<String, int>();
            foreach (var resource in Resources)
            {
                if (!resourceCounts.ContainsKey(resource.Resource))
                {
                    resourceCounts[resource.Resource] = 0;
                }
                resourceCounts[resource.Resource]++;
            }
            var parentBody = GetRoot() as GameComponent;
            var myBox = GetBoundingBox();
            var box = parentBody == null ? GetBoundingBox() : new BoundingBox(myBox.Min - myBox.Center() + parentBody.Position, myBox.Max - myBox.Center() + parentBody.Position);
            var aggregatedResources = resourceCounts.Select(c => new ResourceAmount(c.Key, c.Value));
            var piles = EntityFactory.CreateResourcePiles(aggregatedResources, box).ToList();

            if (Attacker != null && !Attacker.IsDead)
                foreach (var item in piles)
                    Attacker.Creature.Gather(item);
            //else
            //    foreach (var item in piles)
            //        World.Master.TaskManager.AddTask(new GatherItemTask(item));
            Resources.Clear();

            if (GetRoot().GetComponent<Flammable>().HasValue(out var flames) && flames.Heat >= flames.Flashpoint)
                foreach (var item in piles)
                    if (item.GetRoot().GetComponent<Flammable>().HasValue(out var itemFlames))
                        itemFlames.Heat = flames.Heat;
        }

        public bool HasResource(ResourceAmount itemToStock)
        {
            return Resources.Count(resource => resource.Resource == itemToStock.Type) >= itemToStock.Count;
        }

        public bool HasResource(Quantitiy<Resource.ResourceTags> itemToStock)
        {
            var resourceCounts = new Dictionary<String, int>();

            foreach (var resource in Resources)
                if (Library.GetResourceType(resource.Resource).HasValue(out var res) && res.Tags.Contains(itemToStock.Type))
                {
                    if (!resourceCounts.ContainsKey(resource.Resource))
                        resourceCounts[resource.Resource] = 0;
                    resourceCounts[resource.Resource]++;
                }

            return resourceCounts.Count > 0 && resourceCounts.Max(r => r.Value >= itemToStock.Count);
        }

        public List<ResourceAmount> GetResources(Quantitiy<Resource.ResourceTags> quantitiy, RestockType type = RestockType.RestockResource)
        {
            return Resources
                .Where(r =>
                {
                    switch (type)
                    {
                        case RestockType.Any:
                            return true;
                        case RestockType.None:
                            return !r.MarkedForRestock;
                        case RestockType.RestockResource:
                            return r.MarkedForRestock;
                    }

                    return false;
                })
                .Where(r => Library.GetResourceType(r.Resource).HasValue(out var res) && res.Tags.Contains(quantitiy.Type))
                .Select(r => new ResourceAmount(r.Resource))
                .ToList();
        }

        public void AddResource(ResourceAmount tradeGood, RestockType type = RestockType.RestockResource)
        {
            for (int i = 0; i < tradeGood.Count; i++)
            {
                Resources.Add(new InventoryItem()
                {
                    Resource = tradeGood.Type,
                    MarkedForRestock = type == RestockType.RestockResource,
                    MarkedForUse = type != RestockType.RestockResource
                });
            }
        }
    }
}
