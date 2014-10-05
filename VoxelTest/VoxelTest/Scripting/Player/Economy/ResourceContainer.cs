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
    [JsonObject(IsReference = true)]
    public class ResourceContainer : IEnumerable<ResourceAmount>
    {
        public int MaxResources { get; set; }
        [JsonProperty]
        private int currentResourceCount = 0;
        public  int CurrentResourceCount { get { return currentResourceCount; } }
        [JsonProperty]
        public Dictionary<ResourceLibrary.ResourceType, ResourceAmount> Resources { get; set; }

        private void InitializeResources()
        {
            foreach(var pair in ResourceLibrary.Resources)
            {
                if (Resources == null)
                {
                    Resources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();
                }
                if (!Resources.ContainsKey(pair.Key) || Resources[pair.Key] == null)
                {
                    Resources[pair.Key] = new ResourceAmount
                    {
                        NumResources = 0,
                        ResourceType = ResourceLibrary.Resources[pair.Key]
                    };
                }
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            InitializeResources();
        }
       

        public ResourceContainer()
        {
            InitializeResources();
        }

        public void Clear()
        {
            InitializeResources();
            currentResourceCount = 0;
        }

        public int RemoveMaxResources(ResourceAmount resource, int count)
        {
            if (resource != null && Resources.ContainsKey(resource.ResourceType.Type))
            {
                int toReturn = Math.Min(count, Resources[resource.ResourceType.Type].NumResources);

                Resources[resource.ResourceType.Type] -= toReturn;
                currentResourceCount -= toReturn;

                return toReturn;   
            }

            return 0;
        }

        public bool RemoveResource(ResourceAmount resource)
        {
            if (resource == null)
            {
                return false;
            }

            if(resource.NumResources > Resources[resource.ResourceType.Type].NumResources)
            {
                return false;
            }

            if (!Resources.ContainsKey(resource.ResourceType.Type)) return false;

            Resources[resource.ResourceType.Type] -= resource;
            currentResourceCount -= resource.NumResources;
            return true;
        }

        public bool AddResource(ResourceAmount resource)
        {
            if (resource == null)
            {
                return false;
            }

            if(resource.NumResources + CurrentResourceCount > MaxResources)
            {
                return false;
            }

            if (!Resources.ContainsKey(resource.ResourceType.Type)) return false;

            Resources[resource.ResourceType.Type] += resource;
            currentResourceCount += resource.NumResources;
            return true;
        }

        public IEnumerator<ResourceAmount> GetEnumerator()
        {
            return Resources.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsFull()
        {
            return CurrentResourceCount >= MaxResources;
        }

        public bool AddItem(Body component)
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
            foreach(KeyValuePair<ResourceLibrary.ResourceType, ResourceAmount> resource in Resources)
            {
                if(resource.Value.NumResources > 0)
                {
                    resource.Value.NumResources = Math.Max(resource.Value.NumResources - 1, 0);
                    currentResourceCount -= 1;
                    return;
                }
            }
        }

        public int GetResourceCount(Resource resourceType)
        {
            return !Resources.ContainsKey(resourceType.Type) ? 0 : Resources[resourceType.Type].NumResources;
        }

        public bool HasResource(ResourceAmount resourceType)
        {
            return GetResourceCount(resourceType.ResourceType) >= resourceType.NumResources;
        }
    }
}
