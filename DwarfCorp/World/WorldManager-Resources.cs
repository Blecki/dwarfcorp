using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DwarfCorp
{
    public partial class PersistentWorldData
    {
        public Dictionary<Resource.ResourceTags, int> CachedResourceTagCounts = new Dictionary<Resource.ResourceTags, int>();
        public Dictionary<string, bool> CachedCanBuildVoxel = new Dictionary<string, bool>();
    }

    public partial class WorldManager
    {
        public bool RemoveResourcesWithToss(ResourceAmount resources, Vector3 position, Zone Zone) // Todo: Kill this one.
        {
            if (!Zone.Resources.HasResource(resources))
                return false;
            if (!(Zone is Stockpile))
                return false;

            var stock = Zone as Stockpile;

            // Todo: Stockpile deals with it's own boxes.
            var resourceType = Library.GetResourceType(resources.Type);
            var num = stock.Resources.RemoveMaxResources(resources, resources.Count);

            stock.HandleBoxes();

            foreach (var tag in resourceType.Tags)
                if (PersistentData.CachedResourceTagCounts.ContainsKey(tag)) // Move cache into worldmanager...
                {
                    PersistentData.CachedResourceTagCounts[tag] -= num;
                    Trace.Assert(PersistentData.CachedResourceTagCounts[tag] >= 0);
                }

            for (int i = 0; i < num; i++)
            {
                // Make a toss from the last crate to the agent.
                var startPosition = stock.Voxels.Count > 0 ? stock.Voxels.First().Center + new Vector3(0.0f, 1.0f, 0.0f) : Vector3.Zero;
                if (stock.Boxes.Count > 0)
                    startPosition = stock.Boxes.Last().Position + MathFunctions.RandVector3Cube() * 0.5f;

                GameComponent newEntity = EntityFactory.CreateEntity<GameComponent>(resources.Type + " Resource", startPosition);

                TossMotion toss = new TossMotion(1.0f + MathFunctions.Rand(0.1f, 0.2f), 2.5f + MathFunctions.Rand(-0.5f, 0.5f), newEntity.LocalTransform, position);
                newEntity.GetRoot().GetComponent<Physics>().CollideMode = Physics.CollisionMode.None;
                newEntity.AnimationQueue.Add(toss);
                toss.OnComplete += () => newEntity.Die();
            }

            RecomputeCachedVoxelstate();
            return true;
        }

        public bool RemoveResources(List<ResourceAmount> resources)
        {
            var amounts = new Dictionary<String, ResourceAmount>();

            foreach (ResourceAmount resource in resources)
            {
                if (!amounts.ContainsKey(resource.Type))
                    amounts.Add(resource.Type, new ResourceAmount(resource));
                else
                    amounts[resource.Type].Count += resource.Count;
            }

            if (!HasResources(amounts.Values))
                return false;

            foreach (var resource in resources)
            {
                int count = 0;
                var resourceType = Library.GetResourceType(resource.Type);
                foreach (var stock in EnumerateZones().Where(s => resources.All(r => s is Stockpile && (s as Stockpile).IsAllowed(r.Type))))
                {
                    int num = stock.Resources.RemoveMaxResources(resource, resource.Count - count);
                    (stock as Stockpile).HandleBoxes();
                    foreach (var tag in resourceType.Tags)
                    {
                        if (PersistentData.CachedResourceTagCounts.ContainsKey(tag))
                        {
                            PersistentData.CachedResourceTagCounts[tag] -= num;
                            Trace.Assert(PersistentData.CachedResourceTagCounts[tag] >= 0);
                        }
                    }

                    count += num;

                    if (count >= resource.Count)
                        break;
                }
            }

            RecomputeCachedVoxelstate();
            return true;
        }

        public List<ResourceAmount> GetResourcesWithTags(List<Quantitiy<Resource.ResourceTags>> tags) // Todo: This is only ever called with a list of 1.
        {
            var tagsRequired = new Dictionary<Resource.ResourceTags, int>();
            var tagsGot = new Dictionary<Resource.ResourceTags, int>();
            var amounts = new Dictionary<String, ResourceAmount>();

            foreach (Quantitiy<Resource.ResourceTags> quantity in tags)
            {
                tagsRequired[quantity.Type] = quantity.Count;
                tagsGot[quantity.Type] = 0;
            }

            var r = new Random();

            foreach (var stockpile in EnumerateZones())
                foreach (var resource in stockpile.Resources.Enumerate().OrderBy(x => r.Next()))
                    foreach (var requirement in tagsRequired)
                    {
                        var got = tagsGot[requirement.Key];

                        if (requirement.Value <= got) continue;

                        if (!Library.GetResourceType(resource.Type).Tags.Contains(requirement.Key)) continue;

                        int amountToRemove = global::System.Math.Min(resource.Count, requirement.Value - got);

                        if (amountToRemove <= 0) continue;

                        tagsGot[requirement.Key] += amountToRemove;

                        if (amounts.ContainsKey(resource.Type))
                        {
                            amounts[resource.Type].Count += amountToRemove;
                        }
                        else
                        {
                            amounts[resource.Type] = new ResourceAmount(resource.Type, amountToRemove);
                        }
                    }

            var toReturn = new List<ResourceAmount>();

            foreach (var requirement in tagsRequired)
            {
                ResourceAmount maxAmount = null;
                foreach (var pair in amounts)
                {
                    if (!Library.GetResourceType(pair.Key).Tags.Contains(requirement.Key)) continue;
                    if (maxAmount == null || pair.Value.Count > maxAmount.Count)
                    {
                        maxAmount = pair.Value;
                    }
                }
                if (maxAmount != null)
                {
                    toReturn.Add(maxAmount);
                }
            }
            return toReturn;
        }

        public bool HasResources(IEnumerable<Quantitiy<Resource.ResourceTags>> resources)
        {
            foreach (Quantitiy<Resource.ResourceTags> resource in resources)
            {
                int count = EnumerateZones().Sum(stock => stock.Resources.GetResourceCount(resource.Type));

                if (count < resource.Count)
                    return false;
            }

            return true;
        }

        public bool HasResources(IEnumerable<ResourceAmount> resources)
        {
            foreach (ResourceAmount resource in resources)
            {
                int count = EnumerateZones().Sum(stock => stock.Resources.GetResourceCount(resource.Type));

                if (count < resources.Where(r => r.Type == resource.Type).Sum(r => r.Count))
                    return false;
            }

            return true;
        }

        public bool HasResources(String resource)
        {
            return HasResources(new List<ResourceAmount>() { new ResourceAmount(resource) });
        }

        public int CountResourcesWithTag(Resource.ResourceTags tag)
        {
            List<ResourceAmount> resources = ListResourcesWithTag(tag);
            int amounts = 0;

            foreach (ResourceAmount amount in resources)
            {
                amounts += amount.Count;
            }

            return amounts;
        }

        public List<ResourceAmount> ListResourcesWithTag(Resource.ResourceTags tag, bool allowHeterogenous = true)
        {
            var resources = ListResources();

            if (allowHeterogenous)
            {
                return (from pair in resources
                        where Library.GetResourceType(pair.Value.Type).Tags.Contains(tag)
                        select pair.Value).ToList();
            }

            ResourceAmount maxAmount = null;
            foreach (var pair in resources)
            {
                var resource = Library.GetResourceType(pair.Value.Type);
                if (!resource.Tags.Contains(tag)) continue;
                if (maxAmount == null || pair.Value.Count > maxAmount.Count)
                {
                    maxAmount = pair.Value;
                }
            }
            return maxAmount != null ? new List<ResourceAmount>() { maxAmount } : new List<ResourceAmount>();
        }

        public Dictionary<string, ResourceAmount> ListResources()
        {
            var toReturn = new Dictionary<string, ResourceAmount>();

            foreach (var stockpile in EnumerateZones())
            {
                foreach (var resource in stockpile.Resources.Enumerate())
                {
                    if (resource.Count == 0)
                        continue;

                    if (toReturn.ContainsKey(resource.Type))
                        toReturn[resource.Type].Count += resource.Count;
                    else
                        toReturn[resource.Type] = new ResourceAmount(resource);
                }
            }

            return toReturn;
        }

        public bool AddResources(ResourceAmount resources)
        {
            var amount = new ResourceAmount(resources.Type, resources.Count);
            var resource = Library.GetResourceType(amount.Type);

            foreach (Stockpile stockpile in EnumerateZones().Where(s => s is Stockpile && (s as Stockpile).IsAllowed(resources.Type)))
            {
                int space = stockpile.Resources.MaxResources - stockpile.Resources.CurrentResourceCount;

                if (space >= amount.Count)
                {
                    stockpile.Resources.AddResource(amount);
                    stockpile.HandleBoxes();
                    foreach (var tag in resource.Tags)
                    {
                        if (!PersistentData.CachedResourceTagCounts.ContainsKey(tag))
                            PersistentData.CachedResourceTagCounts[tag] = 0;
                        PersistentData.CachedResourceTagCounts[tag] += amount.Count;
                    }

                    RecomputeCachedVoxelstate();
                    return true;
                }
                else
                {
                    var amountToMove = space;
                    stockpile.Resources.AddResource(new ResourceAmount(resources.Type, amountToMove));
                    amount.Count -= amountToMove;

                    stockpile.HandleBoxes();
                    foreach (var tag in resource.Tags)
                    {
                        if (!PersistentData.CachedResourceTagCounts.ContainsKey(tag))
                            PersistentData.CachedResourceTagCounts[tag] = 0;
                        PersistentData.CachedResourceTagCounts[tag] += amountToMove;
                    }

                    RecomputeCachedVoxelstate();

                    if (amount.Count == 0)
                        return true;
                }
            }

            return false;
        }

        public Dictionary<string, Pair<ResourceAmount>> ListResourcesInStockpilesPlusMinions()
        {
            var stocks = ListResources();
            var toReturn = new Dictionary<string, Pair<ResourceAmount>>();

            foreach (var pair in stocks)
                toReturn[pair.Key] = new Pair<ResourceAmount>(pair.Value, new ResourceAmount(pair.Value.Type, 0));

            foreach (var creature in PlayerFaction.Minions)
            {
                var inventory = creature.Creature.Inventory;
                foreach (var i in inventory.Resources)
                {
                    var resource = i.Resource;
                    if (toReturn.ContainsKey(resource))
                        toReturn[resource].Second.Count += 1;
                    else
                        toReturn[resource] = new Pair<ResourceAmount>(new ResourceAmount(resource, 0), new ResourceAmount(resource));
                }
            }

            return toReturn;
        }

        public void RecomputeCachedVoxelstate()
        {
            foreach (var type in Library.EnumerateVoxelTypes())
            {
                bool nospecialRequried = type.BuildRequirements.Count == 0;
                PersistentData.CachedCanBuildVoxel[type.Name] = type.IsBuildable && ((nospecialRequried && HasResources(type.ResourceToRelease)) || (!nospecialRequried && HasResourcesCached(type.BuildRequirements)));
            }
        }

        public bool HasResourcesCached(IEnumerable<Resource.ResourceTags> resources)
        {
            foreach (var resource in resources)
            {
                if (!PersistentData.CachedResourceTagCounts.ContainsKey(resource))
                    return false;

                if (PersistentData.CachedResourceTagCounts[resource] == 0)
                    return false;
            }

            return true;
        }

        public void RecomputeCachedResourceState()
        {
            PersistentData.CachedResourceTagCounts.Clear();
            foreach (var resource in ListResources())
            {
                var type = Library.GetResourceType(resource.Key);

                foreach (var tag in type.Tags)
                {
                    Trace.Assert(type.Tags.Count(t => t == tag) == 1);
                    if (!PersistentData.CachedResourceTagCounts.ContainsKey(tag))
                        PersistentData.CachedResourceTagCounts[tag] = resource.Value.Count;
                    else
                        PersistentData.CachedResourceTagCounts[tag] += resource.Value.Count;
                }
            }

            RecomputeCachedVoxelstate();
        }

        public bool CanBuildVoxel(VoxelType type)
        {
            if (PersistentData.CachedCanBuildVoxel.ContainsKey(type.Name))
                return PersistentData.CachedCanBuildVoxel[type.Name];
            return false;
        }
    }
}
