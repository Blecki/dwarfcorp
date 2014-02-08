using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    /// <summary>
    /// A creature has a set of "statuses" (such as health, energy, etc.) which get
    /// modified over its lifetime. A creature can be "satisfied" or "unsatisfied" depending on its status.
    /// </summary>
    public class CreatureStatus
    {
        /// <summary>
        /// A creature status is a named value which has minimum and maximum thresholds for satisfaction.
        /// </summary>
        public class Status
        {
            private float currentValue;

            public string Name { get; set; }

            public float CurrentValue
            {
                get { return currentValue; }
                set { SetValue(value); }
            }

            public float MinValue { get; set; }
            public float MaxValue { get; set; }
            public float UnhappyThreshold { get; set; }
            public float SatisfiedThreshold { get; set; }


            public bool IsSatisfied()
            {
                return CurrentValue >= SatisfiedThreshold;
            }

            public bool IsUnhappy()
            {
                return CurrentValue <= UnhappyThreshold;
            }

            public void SetValue(float v)
            {
                currentValue = Math.Max(Math.Min(v, MaxValue), MinValue);
            }
        }

        public Dictionary<string, Status> Statuses { get; set; }

        public bool IsAsleep { get; set; }

        public Status Hunger { get { return Statuses["Hunger"]; } set { Statuses["Hunger"] = value; } }
        public Status Energy { get { return Statuses["Energy"]; } set { Statuses["Energy"] = value; } }

        private float HungerDamageRate = 1.0f;
        private DateTime LastHungerDamageTime = DateTime.Now;

        public CreatureStatus()
        {
            IsAsleep = false;
            Statuses = new Dictionary<string, Status>();
            Hunger = new Status
            {
                MaxValue = 100.0f,
                MinValue = 0.0f,
                Name = "Hunger",
                SatisfiedThreshold = 80.0f,
                UnhappyThreshold = 15.0f,
                CurrentValue = 100.0f
            };

            Energy = new Status
            {
                MaxValue = 100.0f,
                MinValue = 0.0f,
                Name = "Energy",
                SatisfiedThreshold = 80.0f,
                UnhappyThreshold = 15.0f,
                CurrentValue = 100.0f
            };
        }

        public void Update(Creature creature, GameTime gameTime, ChunkManager chunks, Camera camera)
        { 
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Hunger.CurrentValue -= dt * creature.Stats.HungerGrowth;

            if(!IsAsleep)
            {
                Energy.CurrentValue -= dt * creature.Stats.Tiredness;
            }

            if(Energy.IsUnhappy())
            {
                creature.DrawIndicator(IndicatorManager.StandardIndicators.Sleepy);
            }

            if(Hunger.IsUnhappy())
            {
                creature.DrawIndicator(IndicatorManager.StandardIndicators.Hungry);

                if(Hunger.CurrentValue <= 1e-12 && (DateTime.Now - LastHungerDamageTime).TotalSeconds > HungerDamageRate)
                {
                    creature.Health.Damage(1.0f / (creature.Stats.HungerResistance) * HungerDamageRate);
                    LastHungerDamageTime = DateTime.Now;
                }
            }
        }
    }

}