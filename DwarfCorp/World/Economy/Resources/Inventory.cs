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
        public class InventoryItem
        {
            public Resource Resource;
            public bool MarkedForRestock = false;
            public bool MarkedForUse = false;
        }

        public enum RestockType
        {
            RestockResource,
            None,
            Any
        }

        public List<InventoryItem> Resources = new List<InventoryItem>();

        public ResourceSet ContentsAsResourceSet()
        {
            var r = new ResourceSet();
            foreach (var item in Resources)
                r.Add(item.Resource);
            return r;
        }

        [JsonIgnore]
        public CreatureAI Attacker = null;

        public void SetLastAttacker(CreatureAI Creature)
        {
            Attacker = Creature;
        }

        public Inventory()
        {
        }

        public Inventory(ComponentManager Manager, string Name, Vector3 BoundingBoxExtents, Vector3 LocalBoundingBoxOffset) :
            base(Manager, Name, Matrix.Identity, BoundingBoxExtents, LocalBoundingBoxOffset)
        {
            CollisionType = CollisionType.None;
        }

        public Resource Find(String OfType, RestockType RestockType)
        {
            return Resources
                .Where(r =>
                {
                    switch (RestockType)
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
                .Where(r => r.Resource.TypeName == OfType)
                .Select(r => r.Resource)
                .FirstOrDefault();
        }

        public bool Remove(IEnumerable<Resource> Resources, RestockType RestockType)
        {
            foreach (var resource in Resources)
                if (!Remove(resource, RestockType))
                    return false;
            return true;
        }

        public bool Remove(Resource Resource, RestockType RestockType)
        {
            var index = Resources.FindIndex(r => Object.ReferenceEquals(r.Resource, Resource));
            if (index >= 0)
            {
                Resources.RemoveAt(index);
                return true;
            }
#if DEBUG
            throw new InvalidOperationException("Attempted to remove item from inventory, but it wasn't in inventory.");
#else
            return false;
#endif
        }
        
        public bool Pickup(GameComponent item, RestockType restockType)
        {
            if(item == null || item.IsDead)
            {
                return false;
            }

            if (item is ResourceEntity)
            {
                var entity = item as ResourceEntity;
                Resources.Add(new InventoryItem()
                {
                    MarkedForRestock = restockType == RestockType.RestockResource,
                    MarkedForUse = restockType != RestockType.RestockResource,
                    Resource = entity.Resource
                });
            }
            else
            {
                throw new InvalidOperationException();
            }

            item.SetFlag(Flag.Active, false);
            if (Parent.HasValue(out var parent))
            {
                var toss = new BodyTossMotion(0.5f + MathFunctions.Rand(0.05f, 0.08f), 1.0f, item.GlobalTransform, parent);
                item.AnimationQueue.Add(toss);
                toss.OnComplete += () => item.GetRoot().Delete();
            }
            else
                item.GetRoot().Delete();

            return true;
        }

        public bool RemoveAndCreateWithToss(Resource Resource, Vector3 pos, RestockType type) // Todo: Kill
        {
            var body = RemoveAndCreate(Resource, type);
            if (body != null)
            {
                var toss = new TossMotion(1.0f, 2.5f, body.LocalTransform, pos);

                if (body.GetRoot().GetComponent<Physics>().HasValue(out var physics))
                    physics.CollideMode = Physics.CollisionMode.None;

                body.AnimationQueue.Add(toss);
                toss.OnComplete += body.Delete;
                return true;
            }
            return false;
        }

        public GameComponent RemoveAndCreate(Resource Resource, RestockType RestockType) // todo: Kill
        {
            var pos = GetRoot().Position;

            if(!Remove(Resource, RestockType))
                return null;

            return Manager.RootComponent.AddChild(new ResourceEntity(Manager, Resource, pos + MathFunctions.RandVector3Cube() * 0.5f));
        }

        internal Dictionary<string, ResourceTypeAmount> Aggregate()
        {
            var toReturn = new Dictionary<string, ResourceTypeAmount>();
            foreach(var resource in Resources)
            {
                if (toReturn.ContainsKey(resource.Resource.TypeName))
                    toReturn[resource.Resource.TypeName].Count++;
                else
                    toReturn.Add(resource.Resource.TypeName, new ResourceTypeAmount(resource.Resource.TypeName, 1));
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
            //var resourceCounts = Aggregate();
            var parentBody = GetRoot();
            var myBox = GetBoundingBox();
            var box = parentBody == null ? GetBoundingBox() : new BoundingBox(myBox.Min - myBox.Center() + parentBody.Position, myBox.Max - myBox.Center() + parentBody.Position);
            //var aggregatedResources = resourceCounts.Select(c => new ResourceTypeAmount(c.Key, c.Value.Count));
            //var piles = EntityFactory.CreateResourcePiles(aggregatedResources, box).ToList();
            var piles = EntityFactory.CreateResourcePiles(Manager, Resources.Select(r => r.Resource), box).ToList();

            if (Attacker != null && !Attacker.IsDead)
                foreach (var item in piles)
                    Attacker.Creature.Gather(item, TaskPriority.Eventually);

            Resources.Clear();

            if (GetRoot().GetComponent<Flammable>().HasValue(out var flames) && flames.Heat >= flames.Flashpoint)
                foreach (var item in piles)
                    if (item.GetRoot().GetComponent<Flammable>().HasValue(out var itemFlames))
                        itemFlames.Heat = flames.Heat;
        }

        public bool Contains(Resource Resource)
        {
            return Resources.Any(r => Object.ReferenceEquals(r.Resource, Resource));
        }

        public bool HasResource(ResourceTypeAmount itemToStock)
        {
            return Resources.Count(resource => resource.Resource.TypeName == itemToStock.Type) >= itemToStock.Count;
        }

        public bool HasResource(ResourceTagAmount itemToStock)
        {
            var resourceCounts = new Dictionary<String, int>();

            foreach (var resource in Resources)
                if (resource.Resource.ResourceType.HasValue(out var res) && res.Tags.Contains(itemToStock.Tag))
                {
                    if (!resourceCounts.ContainsKey(resource.Resource.TypeName))
                        resourceCounts[resource.Resource.TypeName] = 0;
                    resourceCounts[resource.Resource.TypeName]++;
                }

            return resourceCounts.Count > 0 && resourceCounts.Max(r => r.Value >= itemToStock.Count);
        }

        public List<Resource> EnumerateResources(ResourceTagAmount quantitiy, RestockType type = RestockType.RestockResource)
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
                .Where(r => r.Resource.ResourceType.HasValue(out var res) && res.Tags.Contains(quantitiy.Tag))
                .Select(r => r.Resource)
                .ToList();
        }

        public List<InventoryItem> EnumerateInventory()
        {
            return Resources;
        }

        public List<Resource> FindResourcesOfType(ResourceTypeAmount amount)
        {
            var count = 0;
            var r = new List<Resource>();
            foreach (var res in Resources)
            {
                if (res.Resource.TypeName == amount.Type && count < amount.Count)
                {
                    r.Add(res.Resource);
                    count += 1;
                    if (count >= amount.Count)
                        break;
                }
            }
            return r;
        }

        public List<Resource> FindResourcesOfApparentType(ResourceApparentTypeAmount amount)
        {
            var count = 0;
            var r = new List<Resource>();
            foreach (var res in Resources)
            {
                if (res.Resource.DisplayName == amount.Type && count < amount.Count)
                {
                    r.Add(res.Resource);
                    count += 1;
                    if (count >= amount.Count)
                        break;
                }
            }
            return r;
        }

        public void AddResource(Resource tradeGood, RestockType type = RestockType.RestockResource)
        {
            Resources.Add(new InventoryItem()
            {
                Resource = tradeGood,
                MarkedForRestock = type == RestockType.RestockResource,
                MarkedForUse = type != RestockType.RestockResource
            });
        }
    }
}
