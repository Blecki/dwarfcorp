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
    /// <summary>
    /// A static collection of factions.
    /// </summary>
    [Saving.SaveableObject(0)]
    public class FactionLibrary : Saving.ISaveableObject
    {
        public Dictionary<string, Faction> Factions { get; set; }
        
        public Faction GenerateFaction(WorldManager world, int idx , int n)
        {
            var race = RaceLibrary.RandomIntelligentRace();

            var fact = new Faction(world)
            {
                Race = race,
                Name = TextGenerator.GenerateRandom(Datastructures.SelectRandom(race.FactionNameTemplates).ToArray()),
                PrimaryColor = new HSLColor(idx * (255.0f / n), 255.0, MathFunctions.Rand(100.0f, 200.0f)),
                SecondaryColor = new HSLColor(MathFunctions.Rand(0, 255.0f), 255.0, MathFunctions.Rand(100.0f, 200.0f)),
                TradeMoney = (decimal)MathFunctions.Rand(250.0f, 20000.0f),
                Center = new Point(MathFunctions.RandInt(0, Overworld.Map.GetLength(0)), MathFunctions.RandInt(0, Overworld.Map.GetLength(1))),
                GoodWill = MathFunctions.Rand(-1, 1),
                DistanceToCapital = MathFunctions.Rand(100, 500),
                ClaimsColony = MathFunctions.RandEvent(0.1f)
            };
            fact.Economy = new Economy(fact, fact.TradeMoney, world, new CompanyInformation() {LogoBackgroundColor = fact.SecondaryColor.ToVector4(), LogoSymbolColor = fact.PrimaryColor.ToVector4(), Name = fact.Name});
            return fact;
        }

        public void Initialize(WorldManager state, CompanyInformation CompanyInformation)
        {
            if (Factions == null)
            {
                Factions = new Dictionary<string, Faction>();
                Factions["Player"] = new Faction(state)
                {
                    Name = "Player",
                    Race = RaceLibrary.FindRace("Dwarf")
                };

                Factions["The Motherland"] = new Faction(state)
                {
                    Name = "The Motherland",
                    Race = RaceLibrary.FindRace("Dwarf"),
                    IsRaceFaction = false,
                    TradeMoney = 10000,
                    TerritorySize = 9999,
                    DistanceToCapital = 600,
                    IsMotherland = true,
                };
            }


            Factions["Goblins"] = new Faction(state)
            {
                Name = "Goblins",
                Race = RaceLibrary.FindRace("Goblins"),
                IsRaceFaction = true
            };

            Factions["Elf"] = new Faction(state)
            {
                Name = "Elf",
                Race = RaceLibrary.FindRace("Elf"),
                IsRaceFaction = true
            };

            Factions["Undead"] = new Faction(state)
            {
                Name = "Undead",
                Race = RaceLibrary.FindRace("Undead"),
                IsRaceFaction = true
            };

            Factions["Demon"] = new Faction(state)
            {
                Name = "Demon",
                Race = RaceLibrary.FindRace("Demon"),
                IsRaceFaction = true
            };

            Factions["Herbivore"] = new Faction(state)
            {
                Name = "Herbivore",
                Race = RaceLibrary.FindRace("Herbivore"),
                IsRaceFaction = true
            };

            Factions["Carnivore"] = new Faction(state)
            {
                Name = "Carnivore",
                Race = RaceLibrary.FindRace("Carnivore"),
                IsRaceFaction = true
            };

            Factions["Evil"] = new Faction(state)
            {
                Name = "Evil",
                Race = RaceLibrary.FindRace("Evil"),
                IsRaceFaction = true
            };


            Factions["Molemen"] = new Faction(state)
            {
                Name = "Molemen",
                Race = RaceLibrary.FindRace("Molemen"),
                IsRaceFaction = true
            };
            
            Factions["Player"].Economy = new Economy(Factions["Player"], 300.0m, state, CompanyInformation);
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

        public void AddFactions(WorldManager world, List<Faction> factionsInSpawn)
        {
            if (Factions == null)
            {
                Factions = new Dictionary<string, Faction>();

                Factions["Player"] = new Faction(world)
                {
                    Name = "Player",
                    Race = RaceLibrary.FindRace("Dwarf"),
                    DistanceToCapital = 0,
                    ClaimsColony = true
                };

                Factions["The Motherland"] = new Faction(world)
                {
                    Name = "The Motherland",
                    Race = RaceLibrary.FindRace("Dwarf"),
                    IsRaceFaction = false,
                    TradeMoney = 10000,
                    IsMotherland = true,
                    TerritorySize = 9999,
                    DistanceToCapital = 600,
                    ClaimsColony = true
                };

                Factions["Herbivore"] = new Faction(world)
                {
                    Name = "Herbivore",
                    Race = RaceLibrary.FindRace("Herbivore"),
                    IsRaceFaction = true
                };

                Factions["Carnivore"] = new Faction(world)
                {
                    Name = "Carnivore",
                    Race = RaceLibrary.FindRace("Carnivore"),
                    IsRaceFaction = true
                };

                Factions["Evil"] = new Faction(world)
                {
                    Name = "Evil",
                    Race = RaceLibrary.FindRace("Evil"),
                    IsRaceFaction = true
                };
            }
            foreach (Faction faction in factionsInSpawn)
            {
                Factions[faction.Name] = faction;
            }
        }

        public class SaveNugget : Saving.Nugget
        {
            public Dictionary<string, Saving.Nugget> Factions;
        }

        Saving.Nugget Saving.ISaveableObject.SaveToNugget(Saving.Saver SaveSystem)
        {
            var r = new SaveNugget();

            r.Factions = new Dictionary<string, Saving.Nugget>();
            foreach (var faction in Factions)
                r.Factions.Add(faction.Key, SaveSystem.SaveObject(faction.Value));

            return r;
        }

        void Saving.ISaveableObject.LoadFromNugget(Saving.Loader SaveSystem, Saving.Nugget From)
        {
            var n = From as SaveNugget;

            Factions = new Dictionary<string, Faction>();
            foreach (var savedFaction in n.Factions)
                Factions.Add(savedFaction.Key, SaveSystem.LoadObject(savedFaction.Value) as Faction);
        }
    }
}
