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
        protected Dictionary<string, ResourceAmount> Resources = new Dictionary<string, ResourceAmount>();

        private void InitializeResources()
        {
            foreach(var pair in ResourceLibrary.Resources)
            {
                Resources[pair.Key] = new ResourceAmount
                {
                    NumResources = 0,
                    ResourceType = ResourceLibrary.Resources[pair.Key]
                };
            }
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
            int toReturn = Math.Min(count, Resources[resource.ResourceType.ResourceName].NumResources);

            Resources[resource.ResourceType.ResourceName] -= toReturn;
            currentResourceCount -= toReturn;

            return toReturn;
        }

        public bool RemoveResource(ResourceAmount resource)
        {
            if(resource.NumResources > Resources[resource.ResourceType.ResourceName].NumResources)
            {
                return false;
            }

            Resources[resource.ResourceType.ResourceName] -= resource;
            currentResourceCount -= resource.NumResources;
            return true;
        }

        public bool AddResource(ResourceAmount resource)
        {
            if(resource.NumResources + CurrentResourceCount > MaxResources)
            {
                return false;
            }

            Resources[resource.ResourceType.ResourceName] += resource;
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
            foreach(KeyValuePair<string, ResourceAmount> resource in Resources)
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
            if(!Resources.ContainsKey(resourceType.ResourceName))
            {
                return 0;
            }
            else
            {
                return Resources[resourceType.ResourceName].NumResources;
            }
        }

        public bool HasResource(ResourceAmount resourceType)
        {
            return GetResourceCount(resourceType.ResourceType) >= resourceType.NumResources;
        }
    }
}
