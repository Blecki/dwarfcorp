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
        public Dictionary<string, Status> Statuses { get; set; }

        public bool IsAsleep { get; set; }
        public bool IsOnStrike { get; set; }

        [JsonIgnore]
        public Status Hunger { get { return Statuses["Hunger"]; } set { Statuses["Hunger"] = value; } }
        [JsonIgnore]
        public Status Energy { get { return Statuses["Energy"]; } set { Statuses["Energy"] = value; } }
        [JsonIgnore]
        public Status Happiness { get { return Statuses["Happiness"]; } set { Statuses["Happiness"] = value; } }
        [JsonIgnore]
        public Status Health { get { return Statuses["Health"]; } set { Statuses["Health"] = value; } }
        [JsonIgnore]
        public Status Boredom { get { return Statuses["Boredom"]; } set { Statuses["Boredom"] = value; } }
        public DwarfBux Money { get; set; }
        private float HungerDamageRate = 10.0f;
        private DateTime LastHungerDamageTime = DateTime.Now;

        public CreatureStatus()
        {
            Money = 0;
            IsAsleep = false;
            IsOnStrike = false;
            Statuses = new Dictionary<string, Status>();
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

        public void Update(Creature creature, DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            var statAdjustments = creature.Stats.FindAdjustment("status");
            if (statAdjustments == null)
            {
                statAdjustments = new StatAdjustment() { Name = "status" };
                creature.Stats.AddStatAdjustment(statAdjustments);
            }
            statAdjustments.Reset();

            if (!creature.IsAsleep)
                Hunger.CurrentValue -= (float)gameTime.ElapsedGameTime.TotalSeconds * creature.Stats.HungerGrowth;
            else
                creature.Hp += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.1f;

            Health.CurrentValue = (creature.Hp - creature.MinHealth) / (creature.MaxHealth - creature.MinHealth);

            // Todo: Why is energy just tied to time of day? Lets make them actually recover at night and spend it during the day.
            if(creature.Stats.CanSleep)
                Energy.CurrentValue = (float) (100*Math.Sin(creature.Manager.World.Time.GetTotalHours()*Math.PI / 24.0f));
            else
                Energy.CurrentValue = 100.0f;

            if (Energy.IsDissatisfied())
            {
                creature.DrawIndicator(IndicatorManager.StandardIndicators.Sleepy);
                statAdjustments.Strength = -2.0f;
                statAdjustments.Intelligence = -2.0f;
                statAdjustments.Dexterity = -2.0f;
            }

            if(creature.Stats.CanEat && Hunger.IsDissatisfied() && !creature.IsAsleep)
            {
                creature.DrawIndicator(IndicatorManager.StandardIndicators.Hungry);

                statAdjustments.Intelligence = -1.0f;
                statAdjustments.Dexterity = -1.0f;

                if(Hunger.CurrentValue <= 1e-12 && (DateTime.Now - LastHungerDamageTime).TotalSeconds > HungerDamageRate)
                {
                    creature.Damage(1.0f / (creature.Stats.HungerResistance) * HungerDamageRate);
                    LastHungerDamageTime = DateTime.Now;
                }
            }
        }
    }
}