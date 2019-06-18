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
        public Faction Faction { get; set; }

        [JsonProperty]
        private List<PoliticalEvent> RecentEvents { get; set; }

        public IEnumerable<PoliticalEvent> GetEvents() { return RecentEvents; }

        public bool HasMet { get; set; }
        public bool IsAtWar { get; set; }
        public TimeSpan DistanceToCapital { get; set; }

        [JsonProperty]
        private float? _cachedFeeling = null;

        public void AddEvent(PoliticalEvent E)
        {
            RecentEvents.Add(E);
            _cachedFeeling = null;
        }

        public Politics()
        {
            RecentEvents = new List<PoliticalEvent>();
        }

        public Politics(DateTime currentDate, TimeSpan distanceToCapital)
        {
            DistanceToCapital = distanceToCapital;
            IsAtWar = false;
            HasMet = false;
            RecentEvents = new List<PoliticalEvent>();
        }

        public Relationship GetCurrentRelationship()
        {
            float feeling = GetCurrentFeeling();

            if (feeling < -0.5f)
                return Relationship.Hateful;
            else if (feeling < 0.5f)
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
            if (!_cachedFeeling.HasValue)
                _cachedFeeling = RecentEvents.Sum(e => e.Change);
            return _cachedFeeling.Value;
        }

        public void UpdateEvents(DateTime currentDate)
        {
            RecentEvents.RemoveAll((e) => currentDate - e.Time > e.Duration);
            _cachedFeeling = null;
        }
    }
}
