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

        public void Initialize(PlayState state, string companyName, string companyMotto, NamedImageFrame companyLogo, Color companyColor)
        {
            Factions = new Dictionary<string, Faction>();
            Factions["Player"] = new Faction
            {
                Name = "Player",
                Alliance = "Dwarf",
                CreatureTypes = new List<string> { "Dwarf", "AxeDwarf"}
            };
            Factions["Player"].Economy = new Economy(Factions["Player"], 300.0f, state, companyName, companyMotto, companyLogo, companyColor);

            Factions["Goblins"] = new Faction
            {
                Name = "Goblins",
                Alliance = "Goblin",
                CreatureTypes = new List<string> { "Goblin" }
            };

            Factions["Undead"] = new Faction
            {
                Name = "Undead",
                Alliance = "Undead",
                CreatureTypes = new List<string> { "Skeleton", "Necromancer"}
            };

            //Factions["Goblins"].Economy = new Economy(Factions["Goblins"], 0.0f, 1.0f, 1.0f);


            Factions["Herbivore"] = new Faction
            {
                Name = "Herbivore",
                Alliance = "Herbivore"
            };
            //Factions["Herbivore"].Economy = new Economy(Factions["Herbivore"], 0.0f, 1.0f, 1.0f);
        }


        public FactionLibrary()
        {


        }

        public void Update(GameTime time)
        {
            foreach(var faction in Factions)
            {
                faction.Value.Update(time);
            }
        }
    }
}
