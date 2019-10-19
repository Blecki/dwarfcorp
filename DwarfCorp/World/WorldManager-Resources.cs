using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DwarfCorp
{
    public partial class PersistentWorldData
    {
        public Dictionary<String, int> CachedResourceTagCounts = new Dictionary<String, int>();
        public Dictionary<string, bool> CachedCanBuildVoxel = new Dictionary<string, bool>();
    }

    public partial class WorldManager
    {
        public bool RemoveResourcesFromSpecificZone(Resource resource, Zone Zone)
        {
            if (Zone is Stockpile stock)
            {
                if (stock.Resources.Remove(resource))
                {
                    RemoveFromResourceTagCounts(resource);
                    RecomputeCachedVoxelstate();
                    return true;
                }
            }

            return false;
        }

        public bool RemoveResources(List<Resource> resources)
        {
            foreach (var resource in resources)
            {
                foreach (var stock in EnumerateZones().OfType<Stockpile>())
                    if (RemoveResourcesFromSpecificZone(resource, stock))
                        break;
            }

            return true;
        }

        private void RemoveFromResourceTagCounts(Resource resource)
        {
            if (resource.ResourceType.HasValue(out var resourceType))
                foreach (var tag in resourceType.Tags)
                    if (PersistentData.CachedResourceTagCounts.ContainsKey(tag))
                        PersistentData.CachedResourceTagCounts[tag] -= 1;
        }

        public List<Resource> GetResourcesWithTag(String Tag)
        {
            return EnumerateZones().OfType<Stockpile>().SelectMany(zone => zone.Resources.Enumerate().Where(r => r.ResourceType.HasValue(out var res) && res.Tags.Contains(Tag))).ToList();
        }

        public List<ResourceTypeAmount> GetResourcesWithTagAggregatedByType(String Tag)
        {
            return ResourceSet.AggregateByType(GetResourcesWithTag(Tag));
        }

        public bool HasResourcesWithTags(IEnumerable<ResourceTagAmount> resources)
        {
            foreach (var resource in resources)
            {
                int count = EnumerateZones().OfType<Stockpile>().Sum(stock => stock.Resources.Count(resource.Tag));

                if (count < resource.Count)
                    return false;
            }

            return true;
        }

        public bool HasResources(IEnumerable<ResourceTypeAmount> resources)
        {
            foreach (var resource in resources)
            {
                int count = EnumerateZones().OfType<Stockpile>().Sum(stock => stock.Resources.Count(resource.Type));

                if (count < resources.Where(r => r.Type == resource.Type).Sum(r => r.Count))
                    return false;
            }

            return true;
        }

        public bool HasResource(String resource)
        {
            return HasResources(new List<ResourceTypeAmount>() { new ResourceTypeAmount(resource, 1) });
        }

        public int CountResourcesWithTag(String Tag)
        {
            return GetResourcesWithTag(Tag).Count();
        }

        public Dictionary<string, ResourceTypeAmount> ListResources()
        {
            var toReturn = new Dictionary<string, ResourceTypeAmount>();

            foreach (var stockpile in EnumerateZones().OfType<Stockpile>().Where(pile => !(pile is Graveyard)))
            {
                foreach (var resource in stockpile.Resources.Enumerate())
                {

                    if (toReturn.ContainsKey(resource.Type))
                        toReturn[resource.Type].Count += 1;
                    else
                        toReturn[resource.Type] = new ResourceTypeAmount(resource.Type, 1);
                }
            }

            return toReturn;
        }

        public ResourceSet GetTradeableResources()
        {
            var r = new ResourceSet();
            foreach (var stockpile in EnumerateZones().OfType<Stockpile>().Where(pile => !(pile is Graveyard)))
                foreach (var resource in stockpile.Resources.Enumerate())
                    r.Add(resource);
            return r;
        }

        public MaybeNull<Tuple<Stockpile, Resource>> FindResource(String Type)
        {
            foreach (var stockpile in EnumerateZones().OfType<Stockpile>())
                foreach (var resource in stockpile.Resources.Enumerate())
                    if (resource.Type == Type)
                        return Tuple.Create(stockpile, resource);
            return null;
        }

        public MaybeNull<Tuple<Stockpile, Resource>> FindUnreservedResource(String Type)
        {
            foreach (var stockpile in EnumerateZones().OfType<Stockpile>())
                foreach (var resource in stockpile.Resources.Enumerate())
                    if (resource.Type == Type && resource.ReservedFor == null)
                        return Tuple.Create(stockpile, resource);
            return null;
        }

        public bool AddResources(Resource Resource)
        {
            bool added = false;

            foreach (Stockpile stockpile in EnumerateZones().Where(s => s is Stockpile && (s as Stockpile).IsAllowed(Resource.Type)))
                if (!stockpile.IsFull())
                    added = stockpile.AddResource(Resource);

            if (added)
            {
                if (Resource.ResourceType.HasValue(out var res))
                    foreach (var tag in res.Tags)
                    {
                        if (!PersistentData.CachedResourceTagCounts.ContainsKey(tag))
                            PersistentData.CachedResourceTagCounts[tag] = 0;
                        PersistentData.CachedResourceTagCounts[tag] += 1;
                    }

                RecomputeCachedVoxelstate();
            }

            return added;
        }

        public Dictionary<string, Pair<ResourceTypeAmount>> ListResourcesInStockpilesPlusMinions()
        {
            var stocks = ListResources();
            var toReturn = new Dictionary<string, Pair<ResourceTypeAmount>>();

            foreach (var pair in stocks)
                toReturn[pair.Key] = new Pair<ResourceTypeAmount>(pair.Value, new ResourceTypeAmount(pair.Value.Type, 0));

            foreach (var creature in PlayerFaction.Minions)
            {
                var inventory = creature.Creature.Inventory;
                foreach (var i in inventory.Resources)
                {
                    if (toReturn.ContainsKey(i.Resource.Type))
                        toReturn[i.Resource.Type].Second.Count += 1;
                    else
                        toReturn[i.Resource.Type] = new Pair<ResourceTypeAmount>(new ResourceTypeAmount(i.Resource.Type, 0), new ResourceTypeAmount(i.Resource.Type, 1));
                }
            }

            return toReturn;
        }

        public IEnumerable<Resource> EnumerateResourcesIncludingMinions()
        {
            foreach (var stockpile in EnumerateZones().OfType<Stockpile>())
                foreach (var res in stockpile.Resources.Enumerate())
                    yield return res;

            foreach (var creature in PlayerFaction.Minions)
                foreach (var i in creature.Creature.Inventory.Resources)
                    yield return i.Resource;
        }

        public void RecomputeCachedVoxelstate()
        {
            foreach (var type in Library.EnumerateVoxelTypes())
            {
                bool nospecialRequried = type.BuildRequirements.Count == 0;
                PersistentData.CachedCanBuildVoxel[type.Name] = type.IsBuildable && ((nospecialRequried && HasResource(type.ResourceToRelease)) || (!nospecialRequried && HasResourcesCached(type.BuildRequirements)));
            }
        }

        public bool HasResourcesCached(IEnumerable<String> resources)
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
                if (Library.GetResourceType(resource.Key).HasValue(out var type))
                    foreach (var tag in type.Tags)
                    {
                        Trace.Assert(type.Tags.Count(t => t == tag) == 1);
                        if (!PersistentData.CachedResourceTagCounts.ContainsKey(tag))
                            PersistentData.CachedResourceTagCounts[tag] = resource.Value.Count;
                        else
                            PersistentData.CachedResourceTagCounts[tag] += resource.Value.Count;
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
