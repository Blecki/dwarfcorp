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

        public void Clear()
        {
            Resources.Clear();
        }

        public bool Has(ResourceTypeAmount ResourceType)
        {
            return Resources.Count(r => r.TypeName == ResourceType.Type) >= ResourceType.Count;
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
            return Resources.Count(r => r.TypeName == Type);
        }

        public int ApparentCount(String ApparentType)
        {
            return Resources.Count(r => r.DisplayName == ApparentType);
        }

        public int CountWithTag(String Tag)
        {
            return Resources.Count(r => r.ResourceType.HasValue(out var res) && res.Tags.Contains(Tag));
        }
        
        public List<Resource> GetByType(List<ResourceTypeAmount> Types)
        {
            var needed = new Dictionary<String, int>();
            foreach (var type in Types)
                needed[type.Type] = type.Count;

            var r = new List<Resource>();
            foreach (var res in Enumerate())
                if (needed.ContainsKey(res.TypeName) && needed[res.TypeName] > 0)
                {
                    needed[res.TypeName] -= 1;
                    r.Add(res);
                }

            return r;
        }

        public struct ResourceGroup
        {
            public String ApparentType;
            public int Count;
            public List<Resource> Resources;
            public MaybeNull<Resource> Prototype { get => Resources.Count > 0 ? Resources[0] : null; }

            public ResourceGroup Append(Resource Res)
            {
                Resources.Add(Res);

                return new ResourceGroup
                {
                    ApparentType = ApparentType,
                    Count = Count + 1,
                    Resources = Resources
                };
            }
        }

        /// <summary>
        /// Returns resources grouped by their type as it appears to the player, which may be different from their underlying type.
        /// </summary>
        /// <param name="Resources"></param>
        /// <returns></returns>
        public static IEnumerable<ResourceGroup> GroupByApparentType(List<Resource> Resources)
        {
            var r = new Dictionary<String, ResourceGroup>();
            foreach (var res in Resources)
                if (r.ContainsKey(res.DisplayName))
                    r[res.DisplayName] = r[res.DisplayName].Append(res);
                else
                    r.Add(res.DisplayName, new ResourceGroup { ApparentType = res.DisplayName, Count = 1, Resources = new List<Resource> { res } });
            return r.Values;
        }

        public static IEnumerable<ResourceGroup> GroupByRealType(List<Resource> Resources)
        {
            var r = new Dictionary<String, ResourceGroup>();
            foreach (var res in Resources)
                if (r.ContainsKey(res.TypeName))
                    r[res.DisplayName] = r[res.TypeName].Append(res);
                else
                    r.Add(res.DisplayName, new ResourceGroup { ApparentType = res.TypeName, Count = 1, Resources = new List<Resource> { res } });
            return r.Values;
        }
    }
}
