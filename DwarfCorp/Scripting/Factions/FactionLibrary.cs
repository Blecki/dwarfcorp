using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class FactionLibrary
    {
        public Dictionary<string, Faction> Factions { get; set; }
        
        public Faction GenerateFaction(OverworldGenerationSettings Settings, int idx , int n)
        {
            var race = RaceLibrary.GetRandomIntelligentRace();

            var fact = new Faction(null)
            {
                Race = race,
                Name = TextGenerator.ToTitleCase(TextGenerator.GenerateRandom(Datastructures.SelectRandom(race.FactionNameTemplates).ToArray())),
                PrimaryColor = new HSLColor(idx * (255.0f / n), 255.0, MathFunctions.Rand(100.0f, 200.0f)),
                SecondaryColor = new HSLColor(MathFunctions.Rand(0, 255.0f), 255.0, MathFunctions.Rand(100.0f, 200.0f)),
                TradeMoney = (decimal)MathFunctions.Rand(250.0f, 20000.0f),
                Center = new Point(MathFunctions.RandInt(0, Settings.Overworld.Map.GetLength(0)), MathFunctions.RandInt(0, Settings.Overworld.Map.GetLength(1))),
                GoodWill = MathFunctions.Rand(-1, 1),
                DistanceToCapital = MathFunctions.Rand(100, 500),
                ClaimsColony = MathFunctions.RandEvent(0.1f)
            };
            fact.Economy = new Company(fact, fact.TradeMoney, new CompanyInformation() {LogoBackgroundColor = fact.SecondaryColor.ToVector4(), LogoSymbolColor = fact.PrimaryColor.ToVector4(), Name = fact.Name});
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
            
            Factions["Player"].Economy = new Company(Factions["Player"], 300.0m, CompanyInformation);
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
    }
}
