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

        [JsonIgnore] public float Tool_Durability { get => GetProperty<float>("Tool_Durability", 1.0f); set => SetProperty<float>("Tool_Durability", value); }
        [JsonIgnore] public float Tool_Wear { get => GetProperty<float>("Tool_Wear", 0.0f); set => SetProperty<float>("Tool_Wear", value); }
        [JsonIgnore] public bool Tool_Breakable { get => GetProperty<bool>("Tool_Breakable", false); set => SetProperty<bool>("Tool_Breakable", value); }
        [JsonIgnore] public CharacterMode Tool_AttackAnimation { get => GetProperty<CharacterMode>("Tool_AttackAnimation", CharacterMode.Attacking); set => SetProperty<CharacterMode>("Tool_AttackAnimation", value); }
        [JsonIgnore] public int Tool_AttackTriggerFrame { get => GetProperty<int>("Tool_AttackTriggerFrame", 1); set => SetProperty<int>("Tool_AttackTriggerFrame", value); }
        [JsonIgnore] public float Tool_AttackDamage { get => GetProperty<float>("Tool_AttackDamage", 1); set => SetProperty<float>("Tool_AttackDamage", value); }
        [JsonIgnore] public String Tool_AttackHitParticles { get => GetProperty<String>("Tool_AttackHitParticles", ""); set => SetProperty<String>("Tool_AttackHitParticles", value); }
        [JsonIgnore] public String Tool_AttackHitEffect { get => GetProperty<String>("Tool_AttackHitEffect", ""); set => SetProperty<String>("Tool_AttackHitEffect", value); }
        [JsonIgnore] public Color Tool_AttackHitColor { get => GetProperty<Color>("Tool_AttackHitColor", Color.White); set => SetProperty<Color>("Tool_AttackHitColor", value); }

        [JsonIgnore] public String Equipment_LayerName { get => GetProperty<String>("Equipment_LayerName", ""); set => SetProperty<String>("Equipment_LayerName", value); }
        [JsonIgnore] public LayerType Equipment_LayerType { get => GetProperty<LayerType>("Equipment_LayerType", LayerType.Tool); set => SetProperty<LayerType>("Equipment_LayerType", value); }
        [JsonIgnore] public EquipmentSlot Equipment_Slot { get => GetProperty<EquipmentSlot>("Equipment_Slot", EquipmentSlot.None); set => SetProperty<EquipmentSlot>("Equipment_Slot", value); }

        #endregion
    }
}
