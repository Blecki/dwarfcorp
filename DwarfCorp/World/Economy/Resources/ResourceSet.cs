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
        [JsonProperty] private List<Resource> Resources = new List<Resource>();

        [JsonIgnore] public int TotalCount => Resources.Count;

        public bool Has(ResourceTypeAmount ResourceType)
        {
            return Resources.Count(r => r.Type == ResourceType.Type) >= ResourceType.Count;
        }

        public bool Contains(Resource Resource)
        {
            return Resources.Contains(Resource);
        }

        public void Add(Resource Resource)
        {
            Resources.Add(Resource);
        }

        public bool Remove(Resource Resource)
        {
            return Resources.Remove(Resource);
        }

        public IEnumerable<Resource> Enumerate()
        {
            return Resources;
        }

        public int Count(String Type)
        {
            return Resources.Count(r => r.Type == Type);
        }

        public int CountWithTag(String Tag)
        {
            return Resources.Count(r => r.ResourceType.HasValue(out var res) && res.Tags.Contains(Tag));
        }

        public List<ResourceTypeAmount> AggregateByType()
        {
            var r = new Dictionary<String, int>();
            foreach (var res in Resources)
                if (r.ContainsKey(res.Type))
                    r[res.Type] += 1;
                else
                    r.Add(res.Type, 1);
            return r.Select(p => new ResourceTypeAmount(p.Key, p.Value)).ToList();
        }

        public List<Resource> RemoveByType(List<ResourceTypeAmount> Types)
        {
            var needed = new Dictionary<String, int>();
            foreach (var type in Types)
                needed[type.Type] = type.Count;

            var r = new List<Resource>();
            foreach (var res in Enumerate())
                if (needed.ContainsKey(res.Type) && needed[res.Type] > 0)
                {
                    needed[res.Type] -= 1;
                    r.Add(res);
                }

            foreach (var res in r)
                Remove(res);

            return r;
        }

        public List<Resource> GetByType(List<ResourceTypeAmount> Types)
        {
            var needed = new Dictionary<String, int>();
            foreach (var type in Types)
                needed[type.Type] = type.Count;

            var r = new List<Resource>();
            foreach (var res in Enumerate())
                if (needed.ContainsKey(res.Type) && needed[res.Type] > 0)
                {
                    needed[res.Type] -= 1;
                    r.Add(res);
                }

            return r;
        }

        public List<Resource> RemoveByType(Dictionary<String, int> NeedToRemove)
        {
            var r = new List<Resource>();
            foreach (var res in Enumerate())
                if (NeedToRemove.ContainsKey(res.Type) && NeedToRemove[res.Type] > 0)
                {
                    NeedToRemove[res.Type] -= 1;
                    r.Add(res);
                }

            foreach (var res in r)
                Remove(res);

            return r;
        }

        public static List<ResourceTypeAmount> AggregateByType(List<Resource> Resources)
        {
            var r = new Dictionary<String, int>();
            foreach (var res in Resources)
                if (r.ContainsKey(res.Type))
                    r[res.Type] += 1;
                else
                    r.Add(res.Type, 1);
            return r.Select(p => new ResourceTypeAmount(p.Key, p.Value)).ToList();
        }
    }
}
