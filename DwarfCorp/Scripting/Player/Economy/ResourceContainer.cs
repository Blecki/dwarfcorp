// ResourceContainer.cs
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
        public Dictionary<String, ResourceAmount> Resources { get; set; }

        private void InitializeResources()
        {
            Resources = new Dictionary<String, ResourceAmount>();
            
            /*
            foreach(var pair in ResourceLibrary.Resources)
            {
                if (Resources == null)
                {
                    Resources = new Dictionary<ResourceLibrary.String, ResourceAmount>();
                }
                if (!Resources.ContainsKey(pair.Key) || Resources[pair.Key] == null)
                {
                    Resources[pair.Key] = new ResourceAmount
                    {
                        NumResources = 0,
                        String = pair.Key
                    };
                }
            }
             */
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            //InitializeResources();
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
                currentResourceCount -= toReturn;

                return toReturn;   
            }

            return 0;
        }

        public int CountResourcesWithTags(Resource.ResourceTags tag)
        {
            int toReturn = 0;
            foreach (ResourceAmount resource in Resources.Values)
            {
                if (ResourceLibrary.GetResourceByName(resource.Type).Tags.Contains(tag))
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

                if (ResourceLibrary.GetResourceByName(resource.Type).Tags.Contains(tags.Type))
                {
                    int rm = Math.Min(resource.Count, numLeft);
                    resource.Count -= rm;
                    numLeft -= rm;
                    currentResourceCount -= rm;
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
            currentResourceCount -= resource.Count;
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
            currentResourceCount += resource.Count;
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
                    currentResourceCount -= 1;
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

                if (ResourceLibrary.GetResourceByName(resourceAmount.Type).Tags.Contains(tags.Type))
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

        public int GetResourceCount(Resource.ResourceTags resourceType, bool allowHeterogenous = false)
        {
            if (allowHeterogenous)
                return Resources.Values.Where(resource => ResourceLibrary.GetResourceByName(resource.Type).Tags.Contains(resourceType)).Sum(resource => resource.Count);
            else
            {
                int count = 0;
                foreach(var resource in Resources.Values.Where(resource => ResourceLibrary.GetResourceByName(resource.Type).Tags.Contains(resourceType)))
                {
                    count = Math.Max(count, resource.Count);
                }
                return count;
            }
        }

        public bool HasResource(Resource.ResourceTags resourceType)
        {
            return Resources.Values.Any(resource => ResourceLibrary.GetResourceByName(resource.Type).Tags.Contains(resourceType));
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
