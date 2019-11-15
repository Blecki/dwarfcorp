using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Resource
    {
        [JsonProperty] private String _TypeName;
        [JsonIgnore] public String TypeName => _TypeName;

        public CreatureAI ReservedFor = null;

        public Blackboard MetaData = null;

        private Resource SetProperty<T>(String Name, T Value)
        {
            var prop = typeof(ResourceType).GetField(Name);
            if (prop != null && prop.FieldType != typeof(T))
                throw new InvalidProgramException("Type mismatch between base ResourceType class and overridden value.");

            if (MetaData == null)
                MetaData = new Blackboard();
            MetaData.SetData<T>(Name, Value);

            return this;
        }

        private T GetProperty<T>(String Name, T Default)
        {
            if (MetaData != null && MetaData.Has(Name))
                return MetaData.GetData<T>(Name, Default);
            else if (ResourceType.HasValue(out var res))
            {
                var prop = typeof(ResourceType).GetField(Name);
                if (prop != null && prop.FieldType == typeof(T))
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
                    _cachedResourceType = Library.GetResourceType(_TypeName);
                return _cachedResourceType;
            }
        }

        public Resource()
        {
            _TypeName = "invalid";
        }

        public Resource(String Type)
        {
            _TypeName = Type;
        }

        public Resource(ResourceType Type)
        {
            _TypeName = Type.TypeName;
            _cachedResourceType = Type;
        }

        #region Property Accessors.
        [JsonIgnore] public String DisplayName { get { return GetProperty<String>("DisplayName", TypeName); } set { SetProperty<String>("DisplayName", value); } }
        [JsonIgnore] public bool Aggregate { get { return GetProperty<bool>("Aggregate", true); } set { SetProperty<bool>("Aggregate", value); } }
        [JsonIgnore] public String Category { get { return GetProperty<String>("Category", ""); } set { SetProperty<String>("Category", value); } }
        [JsonIgnore] public List<Gui.TileReference> GuiLayers { get => GetProperty<List<Gui.TileReference>>("GuiLayers", null); set => SetProperty<List<Gui.TileReference>>("GuiLayers", value); }
        [JsonIgnore] public List<ResourceType.CompositeLayer> CompositeLayers { get => GetProperty<List<ResourceType.CompositeLayer>>("CompositeLayers", new List<ResourceType.CompositeLayer>()); set => SetProperty<List<ResourceType.CompositeLayer>>("CompositeLayers", value); }
        [JsonIgnore] public String Description { get => GetProperty<String>("Description", ""); set => SetProperty<String>("Description", value); }
        [JsonIgnore] public float FoodContent { get => GetProperty<float>("FoodContent", 0.0f); set => SetProperty<float>("FoodContent", value); }
        [JsonIgnore] public DwarfBux MoneyValue { get => GetProperty<DwarfBux>("MoneyValue", 0u); set => SetProperty<DwarfBux>("MoneyValue", value); }
        [JsonIgnore] public ResourceType.TrinketInfo TrinketData { get => GetProperty<ResourceType.TrinketInfo>("TrinketData", new ResourceType.TrinketInfo()); set => SetProperty<ResourceType.TrinketInfo>("TrinketData", value); }
        [JsonIgnore] public Color Tint { get => GetProperty<Color>("Tint", new Color(1.0f, 1.0f, 1.0f, 1.0f)); set => SetProperty<Color>("Tint", value); }
        #endregion
    }
}
