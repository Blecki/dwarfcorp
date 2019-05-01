// CreatureStats.cs
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
using System.IO;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public class CreatureStats
    {
        [JsonProperty]
        private List<StatAdjustment> StatAdjustments = new List<StatAdjustment>();

        public void AddStatAdjustment(StatAdjustment Adjustment)
        {
            StatAdjustments.Add(Adjustment);
        }

        public void RemoveStatAdjustment(String Name)
        {
            StatAdjustments.RemoveAll(a => a.Name == Name);
        }

        public StatAdjustment FindAdjustment(String Name)
        {
            return StatAdjustments.FirstOrDefault(a => a.Name == Name);
        }

        public IEnumerable<StatAdjustment> EnumerateStatAdjustments()
        {
            return StatAdjustments;
        }

        public float BaseDexterity { set { FindAdjustment("base stats").Dexterity = value; } }
        public float BaseConstitution { set { FindAdjustment("base stats").Constitution = value; } }
        public float BaseStrength { set { FindAdjustment("base stats").Strength = value; } }
        public float BaseWisdom { set { FindAdjustment("base stats").Wisdom = value; } }
        public float BaseCharisma { set { FindAdjustment("base stats").Charisma = value; } }
        public float BaseIntelligence { set { FindAdjustment("base stats").Intelligence = value; } }
        public float BaseSize { set { FindAdjustment("base stats").Size = value; } }

        public float Dexterity { get { return Math.Max(1, StatAdjustments.Sum(a => a.Dexterity)); } }
        public float Constitution { get { return Math.Max(1, StatAdjustments.Sum(a => a.Constitution)); } }
        public float Strength { get { return Math.Max(1, StatAdjustments.Sum(a => a.Strength)); } }
        public float Wisdom { get { return Math.Max(1, StatAdjustments.Sum(a => a.Wisdom)); } }
        public float Charisma { get { return Math.Max(1, StatAdjustments.Sum(a => a.Charisma)); } }
        public float Intelligence { get { return Math.Max(1, StatAdjustments.Sum(a => a.Intelligence)); } }
        public float Size { get { return Math.Max(1, StatAdjustments.Sum(a => a.Size)); } }

        public float MaxSpeed { get { return Dexterity; } }
        public float MaxAcceleration { get { return MaxSpeed * 2.0f; }  }
        public float StoppingForce { get { return MaxAcceleration * 6.0f; } }
        public float BaseDigSpeed { get { return Strength + Size; }}
        public float BaseChopSpeed { get { return Strength * 3.0f + Dexterity * 1.0f; } }
        public float JumpForce { get { return 1000.0f; } }
        public float MaxHealth { get { return (Strength + Constitution + Size) * 10.0f; }}

        public float EatSpeed { get { return Size + Strength; }}

        public float HungerGrowth { get { return Size * 0.025f; } }

        public float BaseFarmSpeed { get { return Intelligence + Strength; } }
        public bool CanEat { get; set; }
        public float BuildSpeed { get { return (Intelligence + Dexterity) / 10.0f; } }

        public int Age { get; set; }

        public int RandomSeed;
        public float VoicePitch { get; set; }
        public Gender Gender { get; set; }
        
        public float Tiredness
        {
            get
            {
                if(CanSleep)
                {
                    return 1.0f / Constitution;
                }
                else
                {
                    return 0.0f;
                }
            }
        } 

        public float HungerResistance { get { return Constitution; } }

        public bool CanSleep { get; set; }
        public bool CanGetBored { get; set; }
        public string FullName { get; set; }
        public string Title { get; set; }
        public int NumBlocksDestroyed { get; set; }
        public int NumItemsGathered { get; set; }
        public int NumRoomsBuilt { get; set; }
        public int NumThingsKilled { get; set; }
        public int NumBlocksPlaced { get; set; }

        public int LevelIndex { get; set; }

        [JsonIgnore]
        public CreatureClass CurrentClass { get; set; }
        public Task.TaskCategory AllowedTasks = Task.TaskCategory.None;

        public bool IsMigratory { get; set; }

        public bool IsTaskAllowed(Task.TaskCategory TaskCategory)
        {
            return (AllowedTasks & TaskCategory) == TaskCategory;
        }

        [JsonIgnore]
        public CreatureClass.Level CurrentLevel { get { return CurrentClass.Levels[LevelIndex]; } }

        private int xp = 0;
        public int XP
        {
            get { return xp; }
            set
            {
                xp = value;
            }
        }

        [JsonIgnore]
        public bool IsOverQualified
        {
            get
            {
                return CurrentClass != null ? CurrentClass.Levels.Count > LevelIndex + 1 && XP > CurrentClass.Levels[LevelIndex + 1].XP : false;
            }
        }


        /// <summary>
        /// If true, the creature will occasionally lay eggs.
        /// </summary>
        public bool LaysEggs { get; set; }

        public CreatureStats()
        {
            CanSleep = false;
            CanEat = false;
            CanGetBored = false;
            FullName = "";
            CurrentClass = null;
            AllowedTasks = Task.TaskCategory.Attack |  Task.TaskCategory.Gather | Task.TaskCategory.Plant | Task.TaskCategory.Harvest | Task.TaskCategory.Chop | Task.TaskCategory.Wrangle | Task.TaskCategory.TillSoil;
            LevelIndex = 0;
            XP = 0;
            IsMigratory = false;
            Age = (int)Math.Max(MathFunctions.RandNormalDist(30, 15), 10);
            RandomSeed = MathFunctions.RandInt(int.MinValue, int.MaxValue);
            VoicePitch = 1.0f;

            AddStatAdjustment(new StatAdjustment { Name = "base stats" });
        }

        public CreatureStats(CreatureClass creatureClass, int level)
        {
            CanSleep = false;
            CanEat = false;
            CanGetBored = false;
            FullName = "";
            CurrentClass = creatureClass;
            AllowedTasks = CurrentClass.Actions;
            LevelIndex = level;
            XP = creatureClass.Levels[level].XP;

            StatAdjustments.Add(new StatAdjustment
            {
                Name = "base stats",
                Dexterity = CurrentLevel.BaseStats.Dexterity,
                Constitution = CurrentLevel.BaseStats.Constitution,
                Strength = CurrentLevel.BaseStats.Strength,
                Wisdom = CurrentLevel.BaseStats.Wisdom,
                Charisma = CurrentLevel.BaseStats.Charisma,
                Intelligence = CurrentLevel.BaseStats.Intelligence,
                Size = 0
            });

            Age = (int)Math.Max(MathFunctions.RandNormalDist(30, 15), 10);
            RandomSeed = MathFunctions.RandInt(int.MinValue, int.MaxValue);
        }

        public void ResetBuffs()
        {
            StatAdjustments.RemoveAll(a => a.Name != "base stats");
        }

        public void LevelUp()
        {
            LevelIndex = Math.Min(LevelIndex + 1, CurrentClass.Levels.Count - 1);

            StatAdjustments.RemoveAll(a => a.Name == "base stats");

            StatAdjustments.Add(new StatAdjustment
            {
                Name = "base stats",
                Dexterity = CurrentLevel.BaseStats.Dexterity,
                Constitution = CurrentLevel.BaseStats.Constitution,
                Strength = CurrentLevel.BaseStats.Strength,
                Wisdom = CurrentLevel.BaseStats.Wisdom,
                Charisma = CurrentLevel.BaseStats.Charisma,
                Intelligence = CurrentLevel.BaseStats.Intelligence
            });
        }

        public static float GetRandomVoicePitch(Gender gender)
        {
            switch (gender)
            {
                case Gender.Female:
                    return MathFunctions.Rand(0.2f, 1.0f);
                case Gender.Male:
                    return MathFunctions.Rand(-1.0f, 0.3f);
                case Gender.Nonbinary:
                    return MathFunctions.Rand(-1.0f, 1.0f);
            }
            return 1.0f;
        }
    }

}
