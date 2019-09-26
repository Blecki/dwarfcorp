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

        public int TotalCount = 0;

        public bool Has(String Type, int Amount)
        {
            if (Resources.ContainsKey(Type))
                return Resources[Type] >= Amount;
            else
                return false;
        }

        public void Add(String Type, int Amount)
        {
            if (Resources.ContainsKey(Type))
                Resources[Type] += Amount;
            else
                Resources.Add(Type, Amount);

            TotalCount = Resources.Values.Sum();
        }

        public void Remove(String Type, int Amount)
        {
            if (Resources.ContainsKey(Type))
            {
                Resources[Type] -= Amount;
                if (Resources[Type] <= 0)
                    Resources.Remove(Type);
            }

            TotalCount = Resources.Values.Sum();
        }

        public IEnumerable<ResourceAmount> Enumerate()
        {
            foreach (var item in Resources)
                yield return new ResourceAmount(item.Key, item.Value);
        }

        public int Count(String Type)
        {
            if (Resources.ContainsKey(Type))
                return Resources[Type];
            else
                return 0;
        }
    }
}
