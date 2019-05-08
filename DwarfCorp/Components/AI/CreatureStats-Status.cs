using System;
using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Linq;

namespace DwarfCorp
{
    public partial class CreatureStats
    {
        public Status Hunger = new Status
        {
            MaxValue = 100.0f,
            MinValue = 0.0f,
            Name = "Hunger",
            Adjective = "Hungry",
            SatisfiedThreshold = 95.0f,
            DissatisfiedThreshold = 15.0f,
            CurrentValue = 100.0f
        };

        public Status Energy = new Status
        {
            MaxValue = 100.0f,
            MinValue = 0.0f,
            Name = "Energy",
            Adjective = "Tired",
            SatisfiedThreshold = 99.0f,
            DissatisfiedThreshold = 15.0f,
            CurrentValue = 100.0f
        };

        public Status Happiness = new Status
        {
            MaxValue = 100.0f,
            MinValue = 0.0f,
            Name = "Happiness",
            Adjective = "Unhappy",
            SatisfiedThreshold = 49.0f,
            DissatisfiedThreshold = 20.0f,
            CurrentValue = 50.0f
        };

        public Status Health = new Status
        {
            MaxValue = 1.0f,
            MinValue = 0.0f,
            Name = "Health",
            Adjective = "Unwell",
            SatisfiedThreshold = 0.8f,
            DissatisfiedThreshold = 0.15f,
            CurrentValue = 1.0f
        };
        
        public Status Boredom = new Status
        {
            MaxValue = 35.0f,
            MinValue = 0.0f,
            Name = "Boredom",
            Adjective = "Bored",
            SatisfiedThreshold = 20.0f,
            DissatisfiedThreshold = 15.0f,
            CurrentValue = 30.0f
        };
        
        private IEnumerable<Status> EnumerateStatuses()
        {
            yield return Hunger;
            yield return Energy;
            yield return Happiness;
            yield return Boredom;
            yield return Health;
        }

        public string GetStatusAdjective()
        {
            return EnumerateStatuses().Where(s => s.IsDissatisfied()).OrderBy(s => s.CurrentValue).FirstOrDefault()?.Adjective ?? "OK";
        }
    }
}