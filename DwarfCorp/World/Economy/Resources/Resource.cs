using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Resource
    {
        [JsonProperty] private String _Type;
        [JsonIgnore] public String Type => _Type;

        [JsonIgnore] private MaybeNull<ResourceType> _cachedResourceType = null;
        [JsonIgnore] public MaybeNull<ResourceType> ResourceType
        {
            get
            {
                if (!_cachedResourceType.HasValue())
                    _cachedResourceType = Library.GetResourceType(_Type);
                return _cachedResourceType;
            }
        }

        public Resource()
        {
            _Type = "invalid";
        }

        public Resource(String Type)
        {
            _Type = Type;
        }
    }
}
