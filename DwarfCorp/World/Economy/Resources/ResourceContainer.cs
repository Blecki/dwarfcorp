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
        //public int MaxResources;
        public int CurrentResourceCount;

        [JsonProperty]
        public Dictionary<String, ResourceAmount> Resources = new Dictionary<string, ResourceAmount>();

        public ResourceContainer()
        {
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

        public bool RemoveResource(ResourceAmount resource)
        {
            if (resource == null)
                return false;

            if(resource.Count > Resources[resource.Type].Count)
                return false;

            if (!Resources.ContainsKey(resource.Type)) return false;

            Resources[resource.Type].Count -= resource.Count;
            CurrentResourceCount -= resource.Count;
            return true;
        }

        public bool AddResource(ResourceAmount resource)
        {
            if (!Resources.ContainsKey(resource.Type))
                Resources[resource.Type] = new ResourceAmount(resource.Type, 0);

            Resources[resource.Type].Count += resource.Count;
            CurrentResourceCount += resource.Count;
            return true;
        }

        public IEnumerable<ResourceAmount> Enumerate()
        {
            return Resources.Values;
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
