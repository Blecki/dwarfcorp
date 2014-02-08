using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public bool RemoveResource(ResourceAmount resource)
        {
            if(resource < Resources[resource.ResourceType.ResourceName])
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
    }
}
