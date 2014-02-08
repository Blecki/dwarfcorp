using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public void Initialize()
        {
            Factions = new Dictionary<string, Faction>();
            Factions["Player"] = new Faction
            {
                Name = "Player"
            };
            Factions["Player"].Economy = new Economy(Factions["Player"], 100.0f, 1.0f, 1.0f);

            Factions["Goblins"] = new Faction
            {
                Name = "Goblins"
            };
            Factions["Goblins"].Economy = new Economy(Factions["Goblins"], 0.0f, 1.0f, 1.0f);
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
