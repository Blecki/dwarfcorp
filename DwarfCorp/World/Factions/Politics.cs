using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Politics
    {
        public OverworldFaction OwnerFaction;
        public OverworldFaction OtherFaction;

        [JsonProperty] private List<PoliticalEvent> RecentEvents = new List<PoliticalEvent>();

        public IEnumerable<PoliticalEvent> GetEvents() { return RecentEvents; }

        public bool HasMet = false;
        public bool IsAtWar = false;

        [JsonProperty] private float Feeling = 0.0f;

        public void AddEvent(PoliticalEvent E)
        {
            RecentEvents.Add(E);
            if (RecentEvents.Count > 8)
                RecentEvents.RemoveAt(0);
            Feeling += E.Change;
        }

        public Politics()
        {
        }

        public Relationship GetCurrentRelationship()
        {
            if (Feeling < -0.5f)
                return Relationship.Hateful;
            else if (Feeling < 0.5f)
                return Relationship.Indifferent;
            else
                return Relationship.Loving;
        }

        public bool HasEvent(string text)
        {
            return RecentEvents.Any(e => e.Description == text);
        }

        public float GetCurrentFeeling()
        {
            return Feeling;
        }
    }
}
