// Faction.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DwarfCorp
{
    public class Race
    {
        public class RaceSpeech
        {
            public List<string> Greetings { get; set; }
            public List<string> Farewells { get; set; }
            public List<string> GoodTrades { get; set; }
            public List<string> BadTrades { get; set; }
            public List<string> OffensiveTrades { get; set; }
            public List<string> WarDeclarations { get; set; }
            public List<string> PeaceDeclarations { get; set; }
            public List<string> DemandsForTribute { get; set; }
            public Language Language { get; set; }
        }


        public string Name { get; set; }
        public string Plural { get; set; }
        public List<string> CreatureTypes { get; set; }
        public List<List<string>> WarParties { get; set; }
        public List<List<string>> TradeEnvoys { get; set; } 
        public List<string> NaturalEnemies { get; set; } 
        public bool IsIntelligent { get; set; }
        public bool IsNative { get; set; }
        public string FactionNameFile { get; set; }
        public string NameFile { get; set; }
        public string DiplomacyConversation = "World/default.conv";
        public Animation.SimpleDescriptor TalkAnimation { get; set; }
        public RaceSpeech Speech  { get; set; }
        [JsonIgnore]
        public List<List<string>> FactionNameTemplates { get; set; }
        [JsonIgnore]
        public List<List<string>> NameTemplates { get; set; }

        public List<Resource.ResourceTags> LikedResources { get; set; }
        public List<Resource.ResourceTags> HatedResources { get; set; }
        public List<Resource.ResourceTags> CommonResources { get; set; }
        public List<Resource.ResourceTags> RareResources { get; set; } 

        public Dictionary<Resource.ResourceTags, int> TradeGoods { get; set; }
        public List<Resource.ResourceTags> Crafts { get; set; }
        public List<Resource.ResourceTags> Encrustings { get; set; }
        public string TradeMusic { get; set; }

        public Dictionary<String, String> Biomes = new Dictionary<string, string>();
        public int Icon { get; set; }
        public string Posessive = "";
        public int NumFurniture = 0;

        public bool EatsPlants { get; set; }
        public bool EatsMeat { get; set; }
        public string BecomeWhenEvil { get; set; }
        public string BecomeWhenNotEvil { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            FactionNameTemplates = TextGenerator.GetAtoms(FactionNameFile);
            NameTemplates = TextGenerator.GetAtoms(NameFile);
        }

        public List<ResourceAmount> GenerateResources(WorldManager world)
        {
            Dictionary<String, ResourceAmount> toReturn =
                new Dictionary<String, ResourceAmount>();
            Resource.ResourceTags[] blacklistTags = { Resource.ResourceTags.Money, Resource.ResourceTags.Corpse };
            foreach (var tags in TradeGoods)
            {
                int num = MathFunctions.RandInt(tags.Value - 5, tags.Value + 5);


                IEnumerable<Resource> resources = ResourceLibrary.FindResourcesWithTag(tags.Key);

                if (resources.Count() <= 0) continue;

                for (int i = 0; i < num; i++)
                {
                    Resource randResource = Datastructures.SelectRandom(resources);

                    if (randResource.Tags.Any(blacklistTags.Contains))
                        continue;

                    if (tags.Key == Resource.ResourceTags.Craft)
                    {
                        Resource.ResourceTags craftTag = Datastructures.SelectRandom(Crafts);
                        IEnumerable<Resource> availableCrafts = ResourceLibrary.FindResourcesWithTag(craftTag);

                        Resource trinket = ResourceLibrary.GenerateTrinket(
                            Datastructures.SelectRandom(availableCrafts).Name, MathFunctions.Rand(0.1f, 3.0f));

                        if (MathFunctions.RandEvent(0.3f) && Encrustings.Count > 0)
                        {
                            IEnumerable<Resource> availableGems =
                                ResourceLibrary.FindResourcesWithTag(Datastructures.SelectRandom(Encrustings));
                            randResource = ResourceLibrary.EncrustTrinket(trinket.Name,
                                Datastructures.SelectRandom(availableGems).Name);
                        }
                        else
                        {
                            randResource = trinket;
                        }
                    }

                    if (!toReturn.ContainsKey(randResource.Name))
                    {
                        toReturn[randResource.Name] = new ResourceAmount(randResource.Name, 1);
                    }
                    else
                    {
                        toReturn[randResource.Name].Count += 1;
                    }
                }
            }

            for (int i = 0; i < NumFurniture; i++)
            {
                var randomObject = Datastructures.SelectRandom(CraftLibrary.EnumerateCraftables().Where(type => type.Type == CraftItem.CraftType.Object && type.RequiredResources.All((tags) =>
                    TradeGoods.Any(good => good.Key == tags.Type))));
                if (randomObject == null)
                    continue;
                List<ResourceAmount> selectedResources = new List<ResourceAmount>();
                foreach(var requirement in randomObject.RequiredResources)
                {
                    IEnumerable<Resource> resources = ResourceLibrary.FindResourcesWithTag(requirement.Type);
                    selectedResources.Add(new ResourceAmount(Datastructures.SelectRandom(resources), requirement.Count));
                }
                var randResource = randomObject.ToResource(world, selectedResources, Posessive + " ");
                if (!toReturn.ContainsKey(randResource.Name))
                {
                    toReturn[randResource.Name] = new ResourceAmount(randResource.Name, 1);
                }
                else
                {
                    toReturn[randResource.Name].Count += 1;
                }
            }

            List<ResourceAmount> resList = toReturn.Select(amount => amount.Value).ToList();
            return resList;
        }
       
    }
}
