using System;
using System.Collections.Generic;
using DwarfCorp.GameStates;
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

            public string GetDescription()
            {
                if (CurrentValue >= MaxValue)
                {
                    return "VERY HAPPY";
                }
                else if (CurrentValue <= MinValue)
                {
                    return "LIVID";
                }
                else if (IsSatisfied())
                {
                    return "SATISFIED";
                }
                else if (IsUnhappy())
                {
                    return "UNHAPPY";
                }
                else
                {
                    return "OK";
                }
                
            }

        }

        public Dictionary<string, Status> Statuses { get; set; }

        public bool IsAsleep { get; set; }

        public Status Hunger { get { return Statuses["Hunger"]; } set { Statuses["Hunger"] = value; } }
        public Status Energy { get { return Statuses["Energy"]; } set { Statuses["Energy"] = value; } }
        public Status Happiness { get { return Statuses["Happiness"]; } set { Statuses["Happiness"] = value; } }
        public Status Health { get { return Statuses["Health"]; } set { Statuses["Health"] = value; } }

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
                SatisfiedThreshold = 95.0f,
                UnhappyThreshold = 15.0f,
                CurrentValue = 100.0f
            };

            Energy = new Status
            {
                MaxValue = 100.0f,
                MinValue = 0.0f,
                Name = "Energy",
                SatisfiedThreshold = 99.0f,
                UnhappyThreshold = 15.0f,
                CurrentValue = 100.0f
            };

            Happiness = new Status
            {
                MaxValue = 100.0f,
                MinValue = 0.0f,
                Name = "Happiness",
                SatisfiedThreshold = 80.0f,
                UnhappyThreshold = 20.0f,
                CurrentValue = 50.0f
            };

            Health = new Status
            {
                MaxValue = 1.0f,
                MinValue = 0.0f,
                Name = "Health",
                SatisfiedThreshold = 0.8f,
                UnhappyThreshold = 0.15f,
                CurrentValue = 1.0f
            };
        }

        public void Update(Creature creature, GameTime gameTime, ChunkManager chunks, Camera camera)
        { 
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Hunger.CurrentValue -= dt * creature.Stats.HungerGrowth;

            Health.CurrentValue = (creature.Health.Hp - creature.Health.MinHealth) / (creature.Health.MaxHealth - creature.Health.MinHealth);

            Energy.CurrentValue = (float) (100*Math.Sin(PlayState.Time.GetTotalHours()*Math.PI / 24.0f));
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