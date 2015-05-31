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

            Resources[resource.ResourceType.Type].NumResources -= resource.NumResources;
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

            Resources[resource.ResourceType.Type].NumResources += resource.NumResources;
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
