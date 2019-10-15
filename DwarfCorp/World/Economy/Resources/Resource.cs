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

        public CreatureAI ReservedFor = null;

        public Blackboard MetaData = null;

        public Resource SetProperty<T>(String Name, T Value)
        {
            var prop = typeof(Resource).GetField(Name);
            if (prop == null || prop.FieldType != typeof(T))
                throw new InvalidProgramException("Type mismatch between base ResourceType class and overridden value.");

            if (MetaData == null)
                MetaData = new Blackboard();
            MetaData.SetData(Name, Value);

            return this;
        }

        public T GetProperty<T>(String Name, T Default)
        {
            if (MetaData != null && MetaData.Has(Name))
                return MetaData.GetData(Name, Default);
            else if (ResourceType.HasValue(out var res))
            {
                var prop = typeof(Resource).GetField(Name);
                if (prop.FieldType == typeof(T))
                    return (T)prop.GetValue(res);
                else
                    return Default;
            }
            else
                return Default;
        }

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
