using System;
using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A creature has a set of "statuses" (such as health, energy, etc.) which get
    /// modified over its lifetime. A creature can be "satisfied" or "unsatisfied" depending on its status.
    /// </summary>
    public class CreatureStatus
    {
        public Status Hunger;
        public Status Energy;
        public Status Happiness;
        public Status Health;
        public Status Boredom;

        public CreatureStatus()
        {
            Hunger = new Status
            {
                MaxValue = 100.0f,
                MinValue = 0.0f,
                Name = "Hunger",
                SatisfiedThreshold = 95.0f,
                DissatisfiedThreshold = 15.0f,
                CurrentValue = 100.0f
            };

            Energy = new Status
            {
                MaxValue = 100.0f,
                MinValue = 0.0f,
                Name = "Energy",
                SatisfiedThreshold = 99.0f,
                DissatisfiedThreshold = 15.0f,
                CurrentValue = 100.0f
            };

            Happiness = new Status
            {
                MaxValue = 100.0f,
                MinValue = 0.0f,
                Name = "Happiness",
                SatisfiedThreshold = 49.0f,
                DissatisfiedThreshold = 20.0f,
                CurrentValue = 50.0f
            };

            Boredom = new Status
            {
                MaxValue = 35.0f,
                MinValue = 0.0f,
                Name = "Boredom",
                SatisfiedThreshold = 20.0f,
                DissatisfiedThreshold = 15.0f,
                CurrentValue = 30.0f
            };

            Health = new Status
            {
                MaxValue = 1.0f,
                MinValue = 0.0f,
                Name = "Health",
                SatisfiedThreshold = 0.8f,
                DissatisfiedThreshold = 0.15f,
                CurrentValue = 1.0f
            };
        }

        private IEnumerable<Status> EnumerateStatuses()
        {
            yield return Hunger;
            yield return Energy;
            yield return Happiness;
            yield return Boredom;
            yield return Health;
        }

        public string get_status()
        {
            Status minStatus = null;
            float minValue = float.MaxValue;
            foreach (var status in EnumerateStatuses())
            {
                if (status.IsDissatisfied() && status.CurrentValue < minValue)
                {
                    minStatus = status;
                    minValue = status.CurrentValue;
                }
            }
            if (minStatus == null)
            {
                return "OK";
            }
            else if (minStatus.Name == "Energy")
            {
                return "Tired";
            }
            else if (minStatus.Name == "Hunger")
            {
                return "Hungry";
            }
            else if (minStatus.Name == "Boredom")
            {
                return "Bored";
            }
            else if (minStatus.Name == "Health")
            {
                return "Injured";
            }
            else if (minStatus.Name == "Happiness")
            {
                return "Unhappy";
            }
            else
            {
                return "Weird";
            }
        }
    }
}