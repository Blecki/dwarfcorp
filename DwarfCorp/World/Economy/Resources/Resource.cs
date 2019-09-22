using System.Collections.Generic;
using System.Security.AccessControl;
using DwarfCorp.GameStates;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using System;

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
        public List<String> Tags;
        public float FoodContent;
        public List<CompositeLayer> CompositeLayers;
        public TrinketInfo TrinketData;
        public bool Generated = true;
        public string ShortName;
        public string PlantToGenerate;
        public Color Tint;
        public string AleName = "";
        public CraftItemInfo CraftInfo;
        public Potion PotionType = null;
        public string Category = "";

        public Resource()
        {

        }
    }
}