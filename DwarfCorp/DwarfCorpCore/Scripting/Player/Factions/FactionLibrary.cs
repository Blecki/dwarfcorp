// FactionLibrary.cs
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
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = false)]
    public class Embarkment
    {
        public static Dictionary<string, Embarkment> EmbarkmentLibrary { get; set; } 
        public List<string> Party;
        public Dictionary<ResourceLibrary.ResourceType, int> Resources;
        public float Money;

        public static void Initialize()
        {
            EmbarkmentLibrary = ContentPaths.LoadFromJson<Dictionary<string, Embarkment>>(ContentPaths.World.embarks);
            WorldManager.InitialEmbark = EmbarkmentLibrary["Normal"];
        }
    }

    /// <summary>
    /// A static collection of factions.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class FactionLibrary
    {
        public Dictionary<string, Faction> Factions { get; set; }
        public Dictionary<string, Race> Races { get; set; }
        
        public Faction GenerateFaction(int idx , int n)
        {
            Race race = Datastructures.SelectRandom(Races.Values.Where(r => r.IsIntelligent && r.IsNative));

            return new Faction()
            {
                Race = race,
                Name = TextGenerator.GenerateRandom(Datastructures.SelectRandom(race.FactionNameTemplates).ToArray()),
                PrimaryColor = new HSLColor(idx * (255.0f / n), 255.0, MathFunctions.Rand(100.0f, 200.0f)),
                SecondaryColor = new HSLColor(MathFunctions.Rand(0, 255.0f), 255.0, MathFunctions.Rand(100.0f, 200.0f)),
                TradeMoney = MathFunctions.Rand(250.0f, 20000.0f)
            };
        }

        public void InitializeRaces()
        {
            Races = new Dictionary<string, Race>();
            Races = ContentPaths.LoadFromJson<Dictionary<string, Race>>(ContentPaths.World.races);
        }

        public void Initialize(WorldManager state, string name, string motto, NamedImageFrame logo, Color color)
        {
            if (Races == null)
            {
                InitializeRaces();
            }

            if (Factions == null)
            {
                Factions = new Dictionary<string, Faction>();
                Factions["Player"] = new Faction
                {
                    Name = "Player",
                    Race = Races["Dwarf"]
                };

                Factions["Motherland"] = new Faction()
                {
                    Name = "Motherland",
                    Race = Races["Dwarf"],
                    IsRaceFaction = false,
                    TradeMoney = 10000
                };
            }


            Factions["Goblins"] = new Faction
            {
                Name = "Goblins",
                Race = Races["Goblins"],
                IsRaceFaction = true
            };

            Factions["Elf"] = new Faction
            {
                Name = "Elf",
                Race = Races["Elf"],
                IsRaceFaction = true
            };

            Factions["Undead"] = new Faction
            {
                Name = "Undead",
                Race = Races["Undead"],
                IsRaceFaction = true
            };

            Factions["Demon"] = new Faction
            {
                Name = "Demon",
                Race = Races["Demon"],
                IsRaceFaction = true
            };

            Factions["Herbivore"] = new Faction
            {
                Name = "Herbivore",
                Race = Races["Herbivore"],
                IsRaceFaction = true
            };

            Factions["Carnivore"] = new Faction
            {
                Name = "Carnivore",
                Race = Races["Carnivore"],
                IsRaceFaction = true
            };


            Factions["Molemen"] = new Faction
            {
                Name = "Molemen",
                Race = Races["Molemen"],
                IsRaceFaction = true
            };
            
            Factions["Player"].Economy = new Economy(Factions["Player"], 300.0f, state, name, motto, logo, color);
        }


        public FactionLibrary()
        {


        }

        public void Update(DwarfTime time)
        {
            foreach(var faction in Factions)
            {
                faction.Value.Update(time);
            }
        }

        public void AddFactions(List<Faction> factionsInSpawn)
        {
            if (Factions == null)
            {
                Factions = new Dictionary<string, Faction>();

                if (Races == null)
                {
                    InitializeRaces();
                }

                Factions["Player"] = new Faction
                {
                    Name = "Player",
                    Race = Races["Dwarf"]
                };

                Factions["Motherland"] = new Faction()
                {
                    Name = "Motherland",
                    Race = Races["Dwarf"],
                    IsRaceFaction = false,
                    TradeMoney = 10000
                };

                Factions["Herbivore"] = new Faction
                {
                    Name = "Herbivore",
                    Race = Races["Herbivore"],
                    IsRaceFaction = true
                };

                Factions["Carnivore"] = new Faction
                {
                    Name = "Carnivore",
                    Race = Races["Carnivore"],
                    IsRaceFaction = true
                };
            }
            foreach (Faction faction in factionsInSpawn)
            {
                Factions[faction.Name] = faction;
            }
        }
    }
}
