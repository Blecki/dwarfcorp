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
    public class ResourceSet
    {
        [JsonProperty] private Dictionary<String, int> Resources = new Dictionary<string, int>();

        public int Count = 0;

        public void Add(String Type, int Amount)
        {
            if (Resources.ContainsKey(Type))
                Resources[Type] += Amount;
            else
                Resources.Add(Type, Amount);

            Count = Resources.Values.Sum();
        }

        public void Add(ResourceAmount Resource)
        {
            Add(Resource.Type, Resource.Count);
        }

        public void Remove(String Type, int Amount)
        {
            if (Resources.ContainsKey(Type))
            {
                Resources[Type] -= Amount;
                if (Resources[Type] <= 0)
                    Resources.Remove(Type);
            }

            Count = Resources.Values.Sum();
        }

        public void Remove(ResourceAmount Resource)
        {
            Remove(Resource.Type, Resource.Count);
        }

        public IEnumerable<ResourceAmount> Enumerate()
        {
            foreach (var item in Resources)
                yield return new ResourceAmount(item.Key, item.Value);
        }
    }
}
