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
            public Language Language { get; set; }
        }


        public string Name { get; set; }
        public List<string> CreatureTypes { get; set; }
        public List<List<string>> WarParties { get; set; }
        public List<List<string>> TradeEnvoys { get; set; } 
        public List<string> NaturalEnemies { get; set; } 
        public bool IsIntelligent { get; set; }
        public bool IsNative { get; set; }
        public string FactionNameFile { get; set; }
        public string NameFile { get; set; }
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

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            FactionNameTemplates = TextGenerator.GetAtoms(FactionNameFile);
            NameTemplates = TextGenerator.GetAtoms(NameFile);
        }

        public List<ResourceAmount> GenerateResources()
        {
            Dictionary<ResourceLibrary.ResourceType, ResourceAmount> toReturn =
                new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();
            Resource.ResourceTags[] blacklistTags = { Resource.ResourceTags.Money, Resource.ResourceTags.Corpse };
            foreach (var tags in TradeGoods)
            {
                int num = MathFunctions.RandInt(tags.Value - 5, tags.Value + 5);


                IEnumerable<Resource> resources = ResourceLibrary.GetResourcesByTag(tags.Key);

                if (resources.Count() <= 0) continue;

                for (int i = 0; i < num; i++)
                {
                    Resource randResource = Datastructures.SelectRandom(resources);

                    if (randResource.Tags.Any(blacklistTags.Contains))
                        continue;

                    if (tags.Key == Resource.ResourceTags.Craft)
                    {
                        Resource.ResourceTags craftTag = Datastructures.SelectRandom(Crafts);
                        IEnumerable<Resource> availableCrafts = ResourceLibrary.GetResourcesByTag(craftTag);

                        Resource trinket = ResourceLibrary.GenerateTrinket(
                            Datastructures.SelectRandom(availableCrafts).Type, MathFunctions.Rand(0.1f, 3.0f));

                        if (MathFunctions.RandEvent(0.3f) && Encrustings.Count > 0)
                        {
                            IEnumerable<Resource> availableGems =
                                ResourceLibrary.GetResourcesByTag(Datastructures.SelectRandom(Encrustings));
                            randResource = ResourceLibrary.EncrustTrinket(trinket.Type,
                                Datastructures.SelectRandom(availableGems).Type);
                        }
                        else
                        {
                            randResource = trinket;
                        }
                    }

                    if (!toReturn.ContainsKey(randResource.Type))
                    {
                        toReturn[randResource.Type] = new ResourceAmount(randResource.Type, 1);
                    }
                    else
                    {
                        toReturn[randResource.Type].NumResources += 1;
                    }
                }
            }

            List<ResourceAmount> resList = toReturn.Select(amount => amount.Value).ToList();
            return resList;
        }
       
    }
}
