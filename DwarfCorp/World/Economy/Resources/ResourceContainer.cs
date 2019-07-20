using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Text;
using DwarfCorp.GameStates;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class ResourceContainer
    {
        public int MaxResources;
        public int CurrentResourceCount;

        [JsonProperty]
        public Dictionary<String, ResourceAmount> Resources = new Dictionary<string, ResourceAmount>();

        public ResourceContainer()
        {
        }

        public void Clear()
        {
            CurrentResourceCount = 0;
        }

        public bool HasResources(ResourceAmount resource)
        {
            if (resource != null && Resources.ContainsKey(resource.Type))
            {
                int toReturn = Resources[resource.Type].Count;
                return toReturn >= resource.Count; ;
            }

            return false;
        }

        public int RemoveMaxResources(ResourceAmount resource, int count)
        {
            if (resource != null && Resources.ContainsKey(resource.Type))
            {
                int toReturn = Math.Min(count, Resources[resource.Type].Count);

                Resources[resource.Type].Count -= toReturn;
                CurrentResourceCount -= toReturn;

                return toReturn;   
            }

            return 0;
        }

        public int CountResourcesWithTags(Resource.ResourceTags tag)
        {
            int toReturn = 0;
            foreach (ResourceAmount resource in Resources.Values)
            {
                if (Library.GetResourceType(resource.Type).Tags.Contains(tag))
                {
                    toReturn += resource.Count;
                }
            }

            return toReturn;
        }

        public bool RemoveResourceImmediate(Quantitiy<Resource.ResourceTags> tags )
        {
            int numLeft = tags.Count;

            foreach (ResourceAmount resource in Resources.Values)
            {
                if (numLeft == 0) return true;

                if (Library.GetResourceType(resource.Type).Tags.Contains(tags.Type))
                {
                    int rm = Math.Min(resource.Count, numLeft);
                    resource.Count -= rm;
                    numLeft -= rm;
                    CurrentResourceCount -= rm;
                }
            }

            return numLeft == 0;
        }

        public bool RemoveResource(Quantitiy<Resource.ResourceTags> resource)
        {
            if (resource == null)
            {
                return false;
            }

            if (resource.Count > CountResourcesWithTags(resource.Type))
            {
                return false;
            }

            return RemoveResourceImmediate(resource);
        }

        public bool RemoveResource(ResourceAmount resource)
        {
            if (resource == null)
            {
                return false;
            }

            if(resource.Count > Resources[resource.Type].Count)
            {
                return false;
            }

            if (!Resources.ContainsKey(resource.Type)) return false;

            Resources[resource.Type].Count -= resource.Count;
            CurrentResourceCount -= resource.Count;
            return true;
        }

        public bool AddResource(ResourceAmount resource)
        {
            if (resource == null)
            {
                return false;
            }

            if(resource.Count + CurrentResourceCount > MaxResources)
            {
                return false;
            }

            if (!Resources.ContainsKey(resource.Type))
            {
                Resources[resource.Type] = new ResourceAmount(resource.Type, 0);
            }

            Resources[resource.Type].Count += resource.Count;
            CurrentResourceCount += resource.Count;
            return true;
        }

        public IEnumerable<ResourceAmount> Enumerate()
        {
            return Resources.Values;
        }

        public bool IsFull()
        {
            return CurrentResourceCount >= MaxResources;
        }

        public bool AddItem(GameComponent component)
        {
            if(IsFull())
            {
                return false;
            }
            else
            {
                AddResource(new ResourceAmount(component));

                return true;
            }
        }

        public void RemoveAnyResource()
        {
            foreach(KeyValuePair<String, ResourceAmount> resource in Resources)
            {
                if(resource.Value.Count > 0)
                {
                    resource.Value.Count = Math.Max(resource.Value.Count - 1, 0);
                    CurrentResourceCount -= 1;
                    return;
                }
            }
        }

        public List<ResourceAmount> GetResources(Quantitiy<Resource.ResourceTags> tags)
        {
            List<ResourceAmount> toReturn = new List<ResourceAmount>();
            int amountLeft = tags.Count;
            foreach (ResourceAmount resourceAmount in Resources.Values)
            {
                if (amountLeft <= 0)
                {
                    break;
                }

                if (resourceAmount.Count == 0)
                {
                    continue;
                }

                if (Library.GetResourceType(resourceAmount.Type).Tags.Contains(tags.Type))
                {
                    int amountToRemove = Math.Min(tags.Count, amountLeft);

                    if (amountToRemove > 0)
                    {
                        toReturn.Add(new ResourceAmount(resourceAmount.Type, amountToRemove));
                        amountLeft -= amountToRemove;
                    }
                }
            }

            return toReturn;
        }

        public int GetResourceCount(Resource.ResourceTags resourceType)
        {
            int count = 0;
            foreach (var resource in Resources.Values.Where(resource => Library.GetResourceType(resource.Type).Tags.Contains(resourceType)))
                count = Math.Max(count, resource.Count);
            return count;
        }

        public bool HasResource(Resource.ResourceTags resourceType)
        {
            return Resources.Values.Any(resource => Library.GetResourceType(resource.Type).Tags.Contains(resourceType));
        }

        public bool HasResource(Quantitiy<Resource.ResourceTags > resourceType)
        {
            return GetResourceCount(resourceType.Type) >= resourceType.Count;
        }

        public int GetResourceCount(Resource resourceType)
        {
            return GetResourceCount(resourceType.Name);
        }

        public int GetResourceCount(String resourceType)
        {
            return !Resources.ContainsKey(resourceType) ? 0 : Resources[resourceType].Count;
        }

        public bool HasResource(ResourceAmount resourceType)
        {
            return GetResourceCount(resourceType.Type) >= resourceType.Count;
        }
    }
}
