// ResourceLibrary.cs
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
using System.Linq;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// A static collection of resource types (should eventually be replaced with a file)
    /// </summary>
    [JsonObject(IsReference = true)]
    public class ResourceLibrary
    {

        public struct ResourceType
        {
            private string _value;

            public static ResourceType Wood = "Wood";
            public static ResourceType Stone = "Stone";
            public static ResourceType Dirt = "Dirt";
            public static ResourceType Mana = "Mana";
            public static ResourceType Gold = "Gold";
            public static ResourceType Iron = "Iron";
            public static ResourceType Berry = "Berry";
            public static ResourceType Mushroom = "Mushroom";
            public static ResourceType Grain = "Grain";
            public static ResourceType Sand = "Sand";
            public static ResourceType Coal = "Coal";
            public static ResourceType Meat = "Meat";
            public static ResourceType Bones = "Bone";
            public static ResourceType Gem = "Gem";
            public static ResourceType Trinket = "Trinket";
            public static ResourceType GemTrinket = "Gem-set Trinket";

            public static implicit operator ResourceType(string value)
            {
                return new ResourceType { _value = new string(value.ToCharArray()) };
            }

            public static implicit operator string(ResourceType value)
            {
                return value._value;
            }

            public override string ToString()
            {
                return _value;
            }
        }

        public static Dictionary<ResourceType, Resource> Resources = new Dictionary<ResourceType, Resource>();


        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            InitializeStatics();
        }
       

        public static Resource GetResourceByName(string name)
        {
            return (from pair in Resources where pair.Value.ResourceName == name select pair.Value).FirstOrDefault();
        }


        private static Rectangle GetRect(int x, int y)
        {
            int tileSheetWidth = 32;
            int tileSheetHeight = 32;
            return new Rectangle(x * tileSheetWidth, y * tileSheetHeight, tileSheetWidth, tileSheetHeight);
        }

        public static void Add(Resource resource)
        {
            Resources[resource.ResourceName] = resource;

            EntityFactory.RegisterEntity(resource.ResourceName + " Resource", (position, data) => new ResourceEntity(resource.Type, position));
        }

        public void InitializeStatics()
        {
            string tileSheet = ContentPaths.Entities.Resources.resources;
            Resources = new Dictionary<ResourceType, Resource>();
            Add(new Resource(ResourceType.Wood, 1.0f, "Sometimes hard to come by! Comes from trees.", new NamedImageFrame(tileSheet, GetRect(3, 1)), Color.White, Resource.ResourceTags.Wood, Resource.ResourceTags.Material, Resource.ResourceTags.Flammable));
            Add(new Resource(ResourceType.Stone, 0.5f, "Dwarf's favorite material! Comes from the earth.", new NamedImageFrame(tileSheet, GetRect(3, 0)), Color.White, Resource.ResourceTags.Stone, Resource.ResourceTags.Material));
            Add(new Resource(ResourceType.Dirt, 0.1f, "Can't get rid of it! Comes from the earth.",
                new NamedImageFrame(tileSheet, GetRect(0, 1)), Color.White, Resource.ResourceTags.Soil,
                Resource.ResourceTags.Material));
            Add(new Resource(ResourceType.Sand,  0.2f, "Can't get rid of it! Comes from the earth.", new NamedImageFrame(tileSheet, GetRect(1, 1)), Color.White, Resource.ResourceTags.Soil, Resource.ResourceTags.Material));
            Add(new Resource(ResourceType.Mana, 40.0f, "Mysterious properties!",
                new NamedImageFrame(tileSheet, GetRect(1, 0)), Color.White, Resource.ResourceTags.Magical, Resource.ResourceTags.Precious, Resource.ResourceTags.SelfIlluminating));
            Add(new Resource(ResourceType.Gold, 50.0f, "Shiny!", new NamedImageFrame(tileSheet, GetRect(0, 0)), Color.White, Resource.ResourceTags.Material, Resource.ResourceTags.Metal, Resource.ResourceTags.Precious));
            Add(new Resource(ResourceType.Coal, 10.0f, "Used as fuel", new NamedImageFrame(tileSheet, GetRect(2, 2)), Color.White, Resource.ResourceTags.Fuel, Resource.ResourceTags.Flammable, Resource.ResourceTags.Material));
            Add(new Resource(ResourceType.Iron, 5.0f, "Needed to build things.", new NamedImageFrame(tileSheet, GetRect(2, 0)), Color.White, Resource.ResourceTags.Metal, Resource.ResourceTags.Material));
            Add(new Resource(ResourceType.Berry, 0.5f, "Dwarves can eat these.", new NamedImageFrame(tileSheet, GetRect(2, 1)), Color.White, Resource.ResourceTags.Food, Resource.ResourceTags.Flammable) { FoodContent = 50});
            Add(new Resource(ResourceType.Mushroom, 0.25f, "Dwarves can eat these.", new NamedImageFrame(tileSheet, GetRect(1, 2)), Color.White, Resource.ResourceTags.Food, Resource.ResourceTags.Fungus, Resource.ResourceTags.Flammable) { FoodContent = 50});
            Add(new Resource(ResourceType.Grain,  0.25f, "Dwarves can eat this.", new NamedImageFrame(tileSheet, GetRect(0, 2)), Color.White, Resource.ResourceTags.Food, Resource.ResourceTags.Grain,  Resource.ResourceTags.Flammable) { FoodContent = 100});
            Add(new Resource(ResourceType.Bones,  15.0f, "Came from an animal.", new NamedImageFrame(tileSheet, GetRect(3, 3)), Color.White, Resource.ResourceTags.Material, Resource.ResourceTags.AnimalProduct));
            Add(new Resource(ResourceType.Meat,  25.0f, "Came from an animal.",
                new NamedImageFrame(tileSheet, GetRect(3, 2)), Color.White, Resource.ResourceTags.Food,
                Resource.ResourceTags.AnimalProduct, Resource.ResourceTags.Meat) {FoodContent = 250});
            Add(new Resource(ResourceType.Gem, 35.0f, "Shiny!", new NamedImageFrame(tileSheet, GetRect(0, 3)), Color.White, Resource.ResourceTags.Precious, Resource.ResourceTags.Gem));
            Add(new Resource(Resources[ResourceType.Gem])
            {
                Type = "Ruby",
                ShortName = "Ruby",
                Image = new NamedImageFrame(tileSheet, GetRect(0, 3))
            });

            Add(new Resource(Resources[ResourceType.Gem])
            {
                Type = "Emerald",
                ShortName = "Emerald",
                Image = new NamedImageFrame(tileSheet, GetRect(0, 4))
            });

            Add(new Resource(Resources[ResourceType.Gem])
            {
                Type = "Amethyst",
                ShortName = "Amethyst",
                Image = new NamedImageFrame(tileSheet, GetRect(2, 4))
            });

            Add(new Resource(Resources[ResourceType.Gem])
            {
                Type = "Garnet",
                ShortName = "Garnet",
                Image = new NamedImageFrame(tileSheet, GetRect(1, 3))
            });

            Add(new Resource(Resources[ResourceType.Gem])
            {
                Type = "Citrine",
                ShortName = "Citrine",
                Image = new NamedImageFrame(tileSheet, GetRect(2, 3))
            });

            Add(new Resource(Resources[ResourceType.Gem])
            {
                Type = "Sapphire",
                ShortName = "Sapphire",
                Image = new NamedImageFrame(tileSheet, GetRect(1, 4))
            });

            Add((new Resource(ResourceType.Trinket, 100.0f, "A crafted item.",
                    new NamedImageFrame(ContentPaths.Entities.DwarfObjects.crafts, 32, 0, 0), Color.White, Resource.ResourceTags.Craft, Resource.ResourceTags.Encrustable)));
        }

        public static Resource EncrustTrinket(Resource resource, ResourceType gemType)
        {
            Resource toReturn = new Resource(resource);
            toReturn.Type = gemType + "-encrusted " + toReturn.ResourceName;
            toReturn.MoneyValue += Resources[gemType].MoneyValue * 2;
            toReturn.Tags = new List<Resource.ResourceTags>() {Resource.ResourceTags.Craft};
            toReturn.Image = new NamedImageFrame(toReturn.Image.AssetName, toReturn.Image.SourceRect.Width, toReturn.Image.SourceRect.X / toReturn.Image.SourceRect.Width + 1, toReturn.Image.SourceRect.Y / toReturn.Image.SourceRect.Height);
            Add(toReturn);
            return toReturn;
        }

        public static Resource GenerateTrinket(ResourceType baseMaterial, float quality)
        {
            Resource toReturn = new Resource(Resources[ResourceType.Trinket]);

            string[] names =
            {
                "Ring",
                "Pendant",
                "Earrings",
                "Crown",
                "Brace",
                "Figure",
                "Staff"
            };

            Point[] tiles =
            {
                new Point(0, 0),
                new Point(0, 1),
                new Point(0, 2),
                new Point(0, 3),
                new Point(2, 0),
                new Point(2, 1),
                new Point(2, 2)
            };

            float[] values =
            {
                1.5f,
                1.8f,
                1.6f,
                3.0f,
                2.0f,
                3.5f,
                4.0f
            };

            int item = PlayState.Random.Next(names.Count());

            string name = names[item];
            Point tile = tiles[item];
            toReturn.MoneyValue = values[item]*Resources[baseMaterial].MoneyValue * quality;

            string qualityType = "";

            if (quality < 0.5f)
            {
                qualityType = "Very poor";
            }
            else if (quality < 0.75)
            {
                qualityType = "Poor";
            }
            else if (quality < 1.0f)
            {
                qualityType = "Mediocre";
            }
            else if (quality < 1.25f)
            {
                qualityType = "Good";
            }
            else if (quality < 1.75f)
            {
                qualityType = "Excellent";
            }
            else if(quality < 2.0f)
            {
                qualityType = "Masterwork";
            }
            else
            {
                qualityType = "Legendary";
            }

            toReturn.Type = baseMaterial + " " + name + " (" + qualityType + ")";

            if (Resources.ContainsKey(toReturn.Type))
            {
                return Resources[toReturn.Type];
            }
            toReturn.Tint = Resources[baseMaterial].Tint;
            toReturn.Image = new NamedImageFrame(ContentPaths.Entities.DwarfObjects.crafts, 32, tile.X, tile.Y);
            Add(toReturn);
            toReturn.ShortName = baseMaterial + " " + names[item];
            return toReturn;
        }

        public ResourceLibrary()
        {
            InitializeStatics();
        }


    }

}