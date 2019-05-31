using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class FactionSet
    {
        public Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();
        
        public OverworldFaction GenerateOverworldFaction(OverworldGenerationSettings Settings, int idx, int n)
        {
            var race = Library.GetRandomIntelligentRace();

            var fact = new OverworldFaction()
            {
                Race = race.Name,
                Name = TextGenerator.ToTitleCase(TextGenerator.GenerateRandom(Datastructures.SelectRandom(race.FactionNameTemplates).ToArray())),
                PrimaryColor = new HSLColor(idx * (255.0f / n), 255.0, MathFunctions.Rand(100.0f, 200.0f)),
                SecondaryColor = new HSLColor(MathFunctions.Rand(0, 255.0f), 255.0, MathFunctions.Rand(100.0f, 200.0f)),
                CenterX = MathFunctions.RandInt(0, Settings.Overworld.Map.GetLength(0)),
                CenterY = MathFunctions.RandInt(0, Settings.Overworld.Map.GetLength(1)),
                GoodWill = MathFunctions.Rand(-1, 1),
                InteractiveFaction = true
            };

            return fact;
        }

        public void Initialize(WorldManager world, CompanyInformation CompanyInformation)
        {
            Factions["Player"] = new Faction(world, new OverworldFaction
            {
                Name = "Player",
                Race = "Dwarf"
            })
            {
                DistanceToCapital = 0,
                ClaimsColony = true,
            };

            Factions["The Motherland"] = new Faction(world, new OverworldFaction
            {
                Name = "The Motherland",
                Race = "Dwarf",
                InteractiveFaction = true,
                IsMotherland = true
            })
            {
                TradeMoney = 10000,
                TerritorySize = 9999,
                DistanceToCapital = 600,
                ClaimsColony = true
            };

            Factions["Herbivore"] = new Faction(world, new OverworldFaction
            {
                Name = "Herbivore",
                Race = "Herbivore"
            });

            Factions["Carnivore"] = new Faction(world, new OverworldFaction
            {
                Name = "Carnivore",
                Race = "Carnivore"
            });

            Factions["Evil"] = new Faction(world, new OverworldFaction
            {
                Name = "Evil",
                Race = "Evil"
            });


            Factions["Goblins"] = new Faction(world, new OverworldFaction
            {
                Name = "Goblins",
                Race = "Goblins"
            });

            Factions["Elf"] = new Faction(world, new OverworldFaction
            {
                Name = "Elf",
                Race = "Elf"
            });

            Factions["Undead"] = new Faction(world, new OverworldFaction
            {
                Name = "Undead",
                Race = "Undead"
            });

            Factions["Demon"] = new Faction(world, new OverworldFaction
            {
                Name = "Demon",
                Race = "Demon"
            });

            Factions["Molemen"] = new Faction(world, new OverworldFaction
            {
                Name = "Molemen",
                Race = "Molemen"
            });
        }

        public FactionSet()
        {


        }

        public void Update(DwarfTime time)
        {
            foreach(var faction in Factions)
                faction.Value.Update(time);
        }

        public void AddFaction(Faction Faction)
        {
            Factions[Faction.ParentFaction.Name] = Faction;
        }
    }
}
