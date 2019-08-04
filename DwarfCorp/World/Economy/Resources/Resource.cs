using System.Collections.Generic;
using System.Security.AccessControl;
using DwarfCorp.GameStates;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Resource
    {
        public struct TrinketInfo
        {
            public string BaseAsset;
            public string EncrustingAsset;
            public int SpriteRow;
            public int SpriteColumn;
        }

        public struct CompositeLayer
        {
            public string Asset;
            public Point FrameSize;
            public Point Frame;
        }

        public struct CraftItemInfo
        {
            public string CraftItemType;
            public List<ResourceAmount> Resources;
        }

        public string Name;
        public DwarfBux MoneyValue;
        public string Description;
        public List<TileReference> GuiLayers; // Todo: Would like to combine the different graphics options
        public List<ResourceTags> Tags;
        public float FoodContent;
        public List<CompositeLayer> CompositeLayers;
        public TrinketInfo TrinketData;
        public bool Generated = true;
        public string ShortName;
        public float MaterialStrength = 5;
        public string PlantToGenerate;
        public List<Quantitiy<ResourceTags>> CraftPrerequisites;
        public Color Tint;
        public string AleName = "";
        public CraftItemInfo CraftInfo;
        public Potion PotionType = null;

        // Todo: Replace this with strings so mods can extend it.
        public enum ResourceTags
        {
            Edible,
            Material,
            HardMaterial,
            Precious,
            Flammable,
            SelfIlluminating,
            Wood,
            Metal,
            Stone,
            Sandstone,
            Obsidian,
            Granite,
            Slate,
            Marble,
            Fuel,
            Magical,
            Soil,
            Grain,
            Fungus,
            None,
            AnimalProduct,
            Meat,
            Gem,
            Craft,
            Encrustable,
            Alcohol,
            Brewable,
            Bakeable,
            RawFood,
            PreparedFood,
            Plantable,
            AboveGroundPlant,
            BelowGroundPlant,
            Bone,
            Corpse,
            Money,
            Sand,
            Glass,
            Fruit,
            Gourd,
            Evil,
            Jolly,
            Rail,
            Explosive,
            CraftItem,
            Mana,
            Potion,
            Seed,
            Slime,
            CopperOre,
            Copper,
            IronOre,
            Iron,
            Tool
        }

        public Resource()
        {

        }
    }
}