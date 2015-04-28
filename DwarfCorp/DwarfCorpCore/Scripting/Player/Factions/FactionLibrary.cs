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
                SecondaryColor = new HSLColor(MathFunctions.Rand(0, 255.0f), 255.0, MathFunctions.Rand(100.0f, 200.0f))
            };
        }

        public void Initialize(PlayState state, string name, string motto, NamedImageFrame logo, Color color)
        {
            Races = new Dictionary<string, Race>();
            /*
            Races["Dwarf"] = new Race()
            {
                Name = "Dwarf",
                CreatureTypes = new List<string> {"Dwarf", "AxeDwarf"},
                IsIntelligent = true,
                IsNative = false,
                FactionNameFile = ContentPaths.Text.Templates.nations_dwarf,
                NameFile = ContentPaths.Text.Templates.names_dwarf,
                FactionNameTemplates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.nations_dwarf)
            };

            Races["Goblins"] = new Race()
            {
                Name = "Goblins",
                CreatureTypes = new List<string> { "Goblin"},
                IsIntelligent = true,
                IsNative = true,
                FactionNameFile = ContentPaths.Text.Templates.nations_dwarf,
                NameFile = ContentPaths.Text.Templates.names_goblin,
                FactionNameTemplates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.nations_goblin)
            };

            Races["Molemen"] = new Race()
            {
                Name = "Molemen",
                CreatureTypes = new List<string> { "Moleman" },
                IsIntelligent = true,
                IsNative = true,
                FactionNameFile = ContentPaths.Text.Templates.nations_dwarf,
                NameFile = ContentPaths.Text.Templates.names_dwarf,
                FactionNameTemplates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.nations_goblin)
            };

            Races["Elf"] = new Race()
            {
                Name = "Elf",
                CreatureTypes = new List<string> { "Elf" },
                IsIntelligent = true,
                IsNative = true,
                FactionNameFile = ContentPaths.Text.Templates.nations_elf,
                NameFile = ContentPaths.Text.Templates.names_elf,
                FactionNameTemplates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.nations_elf)
            };

            Races["Undead"] = new Race()
            {
                Name = "Undead",
                CreatureTypes = new List<string> { "Necromancer", "Skeleton" },
                IsIntelligent = true,
                IsNative = true,
                FactionNameFile = ContentPaths.Text.Templates.nations_undead,
                NameFile = ContentPaths.Text.Templates.names_undead,
                FactionNameTemplates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.nations_undead)
            };


            Races["Herbivore"] = new Race()
            {
                Name = "Herbivore",
                CreatureTypes = new List<string> { "Bird", "Deer" },
                IsIntelligent = false,
                IsNative = true,
                FactionNameFile = ContentPaths.Text.Templates.nations_dwarf,
                NameFile = ContentPaths.Text.Templates.names_dwarf,
            };
             */
            Races = ContentPaths.LoadFromJson<Dictionary<string, Race>>(ContentPaths.World.races);

            Factions = new Dictionary<string, Faction>();
            Factions["Player"] = new Faction
            {
                Name = "Player",
                Race = Races["Dwarf"]
            };
            Factions["Player"].Economy = new Economy(Factions["Player"], 300.0f, state, name, motto, logo, color);

            Factions["Goblins"] = new Faction
            {
                Name = "Goblins",
                Race = Races["Goblins"]
            };

            Factions["Elf"] = new Faction
            {
                Name = "Elf",
                Race = Races["Elf"]
            };

            Factions["Undead"] = new Faction
            {
                Name = "Undead",
                Race = Races["Undead"]
            };

            Factions["Herbivore"] = new Faction
            {
                Name = "Herbivore",
                Race = Races["Herbivore"]
            };


            Factions["Molemen"] = new Faction
            {
                Name = "Molemen",
                Race = Races["Molemen"]
            };
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
    }
}
