// Resource.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System.Collections.Generic;
using System.Security.AccessControl;
using DwarfCorp.GameStates;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A resource is a kind of item that can be bought or sold, and can be used
    /// to build things.
    /// </summary>
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

        public ResourceType Name;
        public DwarfBux MoneyValue;
        public string Description;
        public List<TileReference> GuiLayers;
        public List<ResourceTags> Tags;
        public float FoodContent;
        public List<CompositeLayer> CompositeLayers;
        public TrinketInfo TrinketData;

        public string ShortName;

        public string PlantToGenerate { get; set; }

        public bool CanCraft { get; set; }
        public List<Quantitiy<ResourceTags>> CraftPrerequisites { get; set; }

        public Color Tint { get; set; }
        public string AleName { get; set; }

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
            Jolly
        }

        public Resource()
        {
            AleName = "";
        }

        public Resource(Resource other)
        {
            Name = other.Name;
            ShortName = other.ShortName;
            MoneyValue = other.MoneyValue;
            Description = new string(other.Description.ToCharArray());
            GuiLayers = new List<TileReference>();
            GuiLayers.AddRange(other.GuiLayers);
            Tint = other.Tint;
            Tags = new List<ResourceTags>();
            Tags.AddRange(other.Tags);
            FoodContent = other.FoodContent;
            ShortName = other.ShortName;
            PlantToGenerate = other.PlantToGenerate;
            CanCraft = other.CanCraft;
            CraftPrerequisites = new List<Quantitiy<Resource.ResourceTags>>();
            CraftPrerequisites.AddRange(other.CraftPrerequisites);

            if (other.CompositeLayers != null)
            {
                CompositeLayers = new List<CompositeLayer>();
                CompositeLayers.AddRange(other.CompositeLayers);
            }
            else
            {
                CompositeLayers = null;
            }

            TrinketData = other.TrinketData;
            AleName = other.AleName;
        }

        public string GetTagDescription(string delimiter)
        {
            string s = "";

            for (int i = 0; i < Tags.Count; i++)
            {
                string tag = Tags[i].ToString();
                s += tag.ToString();

                if (i < Tags.Count - 1)
                {
                    s += delimiter;
                }
            }

            return s;
        }
    }

}