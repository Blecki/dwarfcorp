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

namespace DwarfCorp
{

    /// <summary>
    /// A set of simple numbers which define how a creature is to behave.
    /// </summary>
    public class CreatureStats
    {
        public class StatNums
        {
            public float Dexterity = 5;
            public float Constitution = 5;
            public float Strength = 5;
            public float Wisdom = 5;
            public float Charisma = 5;
            public float Intelligence = 5;
            public float Size = 5;

            public static StatNums operator +(StatNums a, StatNums b)
            {
                if (a == null || b == null) return null;
                return new StatNums()
                {
                    Charisma = a.Charisma + b.Charisma,
                    Constitution = a.Charisma + b.Charisma,
                    Dexterity = a.Dexterity + b.Dexterity,
                    Intelligence = a.Intelligence + b.Intelligence,
                    Size = a.Size + b.Size,
                    Strength = a.Strength + b.Strength,
                    Wisdom = a.Wisdom + b.Wisdom
                };
            }

            public static StatNums operator -(StatNums a, StatNums b)
            {
                if (a == null || b == null) return null;
                return new StatNums()
                {
                    Charisma = a.Charisma - b.Charisma,
                    Constitution = a.Charisma - b.Charisma,
                    Dexterity = a.Dexterity - b.Dexterity,
                    Intelligence = a.Intelligence - b.Intelligence,
                    Size = a.Size - b.Size,
                    Strength = a.Strength - b.Strength,
                    Wisdom = a.Wisdom - b.Wisdom
                };
            }
        }

        public StatNums StatBuffs { get; set; }


        public float Dexterity { get; set; }
        public float Constitution { get; set; }
        public float Strength { get; set; }
        public float Wisdom { get; set; }
        public float Charisma { get; set; }
        public float Intelligence { get; set; }
        public float Size { get; set; }

        public float BuffedDex { get { return Dexterity + StatBuffs.Dexterity; } }
        public float BuffedCon { get { return Constitution + StatBuffs.Constitution; } }
        public float BuffedStr { get { return Strength + StatBuffs.Strength; } }
        public float BuffedWis { get { return Wisdom + StatBuffs.Wisdom; }}
        public float BuffedChar { get { return Charisma + StatBuffs.Charisma; } }
        public float BuffedInt { get { return Intelligence + StatBuffs.Intelligence; }}
        public float BuffedSiz { get { return Size + StatBuffs.Size; } }

        public float MaxSpeed { get { return BuffedDex; } }
        public float MaxAcceleration { get { return MaxSpeed * 2.0f; }  }
        public float StoppingForce { get { return MaxAcceleration * 6.0f; } }
        public float BaseDigSpeed { get { return BuffedStr + BuffedSiz; }}
        public float BaseChopSpeed { get { return BuffedStr * 3.0f + BuffedDex * 1.0f; } }
        public float JumpForce { get { return 1000.0f; } }
        public float MaxHealth { get { return (BuffedStr + BuffedCon + BuffedSiz) * 10.0f; }}

        public float EatSpeed { get { return BuffedSiz + BuffedStr; }}

        public float HungerGrowth { get { return BuffedSiz * 0.025f; } }

        public float Tiredness
        {
            get
            {
                if(CanSleep)
                {
                    return 1.0f / BuffedCon;
                }
                else
                {
                    return 0.0f;
                }
            }
        } 

        public float HungerResistance { get { return BuffedCon; } }

        public bool CanSleep { get; set; }

        public string FullName { get; set; }
        public int NumBlocksDestroyed { get; set; }
        public int NumItemsGathered { get; set; }
        public int NumRoomsBuilt { get; set; }
        public int NumThingsKilled { get; set; }
        public int NumBlocksPlaced { get; set; }

        public int LevelIndex { get; set; }
        public EmployeeClass CurrentClass { get; set; }
        public EmployeeClass.Level CurrentLevel { get { return CurrentClass.Levels[LevelIndex]; } }

        private int xp = 0;
        private bool announced = false;
        public int XP
        {
            get { return xp; }
            set
            {
                xp = value;
                if (IsOverQualified)
                {
                    if (!announced)
                    {
                        announced = true;
                        PlayState.AnnouncementManager.Announce(FullName + " (" + CurrentLevel.Name + ")" + " wants a promotion!",
                            FullName + " can now be promoted to " +
                            CurrentClass.Levels[LevelIndex + 1].Name, GameState.Game.StateManager.GetState<PlayState>("PlayState").moneyButton_OnClicked);
                    }
                }
            }
        }

        public bool IsOverQualified {
            get { return CurrentClass.Levels.Count > 1 && XP > CurrentClass.Levels[LevelIndex + 1].XP; }}

        public float BaseFarmSpeed { get { return BuffedInt/100.0f + BuffedStr/100.0f; }}
        public bool CanEat { get; set; }
        public float BuildSpeed { get { return (BuffedInt + BuffedDex)/10.0f; }}

        public CreatureStats()
        {
            CanSleep = false;
            CanEat = false;
            FullName = "";
            CurrentClass = new WorkerClass();
            LevelIndex = 0;
            XP = 0;
            StatBuffs = new StatNums()
            {
                Charisma = 0,
                Constitution = 0,
                Dexterity = 0,
                Intelligence = 0,
                Size = 0,
                Strength = 0,
                Wisdom = 0
            };
        }

        public CreatureStats(EmployeeClass creatureClass, int level)
        {
            CanSleep = false;
            CanEat = false;
            FullName = "";
            CurrentClass = creatureClass;
            LevelIndex = level;
            XP = creatureClass.Levels[level].XP;
            Dexterity = Math.Max(Dexterity, CurrentLevel.BaseStats.Dexterity);
            Constitution = Math.Max(Constitution, CurrentLevel.BaseStats.Constitution);
            Strength = Math.Max(Strength, CurrentLevel.BaseStats.Strength);
            Wisdom = Math.Max(Wisdom, CurrentLevel.BaseStats.Wisdom);
            Charisma = Math.Max(Charisma, CurrentLevel.BaseStats.Charisma);
            Intelligence = Math.Max(Intelligence, CurrentLevel.BaseStats.Intelligence);
            StatBuffs = new StatNums()
            {
                Charisma = 0,
                Constitution = 0,
                Dexterity = 0,
                Intelligence = 0,
                Size = 0,
                Strength = 0,
                Wisdom = 0
            };
        }

        public void ResetBuffs()
        {
            StatBuffs.Charisma = 0;
            StatBuffs.Constitution = 0;
            StatBuffs.Dexterity = 0;
            StatBuffs.Intelligence = 0;
            StatBuffs.Size = 0;
            StatBuffs.Strength = 0;
            StatBuffs.Wisdom = 0;
        }

        public void LevelUp()
        {
            LevelIndex = Math.Min(LevelIndex + 1, CurrentClass.Levels.Count - 1);

            Dexterity = Math.Max(Dexterity, CurrentLevel.BaseStats.Dexterity);
            Constitution = Math.Max(Constitution, CurrentLevel.BaseStats.Constitution);
            Strength = Math.Max(Strength, CurrentLevel.BaseStats.Strength);
            Wisdom = Math.Max(Wisdom, CurrentLevel.BaseStats.Wisdom);
            Charisma = Math.Max(Charisma, CurrentLevel.BaseStats.Charisma);
            Intelligence = Math.Max(Intelligence, CurrentLevel.BaseStats.Intelligence);
            announced = false;
        }
    }

}