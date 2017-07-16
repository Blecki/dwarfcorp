// CreatureStatus.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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

        [JsonIgnore]
        public Status Hunger { get { return Statuses["Hunger"]; } set { Statuses["Hunger"] = value; } }
        [JsonIgnore]
        public Status Energy { get { return Statuses["Energy"]; } set { Statuses["Energy"] = value; } }
        [JsonIgnore]
        public Status Happiness { get { return Statuses["Happiness"]; } set { Statuses["Happiness"] = value; } }
        [JsonIgnore]
        public Status Health { get { return Statuses["Health"]; } set { Statuses["Health"] = value; } }
        public DwarfBux Money { get; set; }
        private float HungerDamageRate = 10.0f;
        private DateTime LastHungerDamageTime = DateTime.Now;

        public CreatureStatus()
        {
            Money = 0;
            IsAsleep = false;
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
                SatisfiedThreshold = 80.0f,
                DissatisfiedThreshold = 20.0f,
                CurrentValue = 50.0f
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
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (!creature.IsAsleep)
            {
                Hunger.CurrentValue -= dt*creature.Stats.HungerGrowth * 100;
            }
            else
            {
                creature.Hp += dt*0.1f;
            }

            Health.CurrentValue = (creature.Hp - creature.MinHealth) / (creature.MaxHealth - creature.MinHealth);

            if(creature.Stats.CanSleep)
                Energy.CurrentValue = (float) (100*Math.Sin(creature.Manager.World.Time.GetTotalHours()*Math.PI / 24.0f));
            else
            {
                Energy.CurrentValue = 100.0f;
            }

            if(Energy.IsDissatisfied())
            {
                creature.DrawIndicator(IndicatorManager.StandardIndicators.Sleepy);
            }

            if(creature.Stats.CanEat && Hunger.IsDissatisfied() && !creature.IsAsleep)
            {
                creature.DrawIndicator(IndicatorManager.StandardIndicators.Hungry);

                if(Hunger.CurrentValue <= 1e-12 && (DateTime.Now - LastHungerDamageTime).TotalSeconds > HungerDamageRate)
                {
                    creature.Damage(1.0f / (creature.Stats.HungerResistance) * HungerDamageRate);
                    LastHungerDamageTime = DateTime.Now;
                }
            }
        }
    }

}