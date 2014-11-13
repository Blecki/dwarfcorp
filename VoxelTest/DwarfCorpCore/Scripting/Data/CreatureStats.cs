using System;
using System.IO;
using DwarfCorp.GameStates;
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
        }

        public float Dexterity { get; set; }
        public float Constitution { get; set; }
        public float Strength { get; set; }
        public float Wisdom { get; set; }
        public float Charisma { get; set; }
        public float Intelligence { get; set; }

        public float Size { get; set; }

        public float MaxSpeed { get { return Dexterity; } }
        public float MaxAcceleration { get { return MaxSpeed * 2.0f; }  }
        public float StoppingForce { get { return MaxAcceleration * 6.0f; } }
        public float BaseDigSpeed { get { return Strength + Size; }}
        public float BaseChopSpeed { get { return Strength * 3.0f + Dexterity * 1.0f; } }
        public float JumpForce { get { return 1000.0f; } }
        public float MaxHealth { get { return (Strength + Constitution + Size) * 10.0f; }}

        public float EatSpeed { get { return Size + Strength; }}

        public float HungerGrowth { get { return Size * 0.025f; } }

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

        public string FirstName { get; set; }
        public string LastName { get; set; }
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
                        PlayState.AnnouncementManager.Announce(FirstName + " " + LastName + " wants a promotion!",
                            FirstName + " " + LastName + " can now be promoted to " +
                            CurrentClass.Levels[LevelIndex + 1].Name);
                    }
                }
            }
        }

        public bool IsOverQualified {
            get { return XP > CurrentClass.Levels[LevelIndex + 1].XP; }}

        public float BaseFarmSpeed { get { return Intelligence/100.0f + Strength/100.0f; }}
        public bool CanEat { get; set; }

        public CreatureStats()
        {
            CanSleep = false;
            CanEat = false;
            FirstName = "";
            LastName = "";
            CurrentClass = new WorkerClass();
            LevelIndex = 0;
            XP = 0;
        }

        public CreatureStats(EmployeeClass creatureClass, int level)
        {
            CanSleep = false;
            CanEat = false;
            FirstName = "";
            LastName = "";
            CurrentClass = creatureClass;
            LevelIndex = level;
            XP = creatureClass.Levels[level].XP;
            Dexterity = Math.Max(Dexterity, CurrentLevel.BaseStats.Dexterity);
            Constitution = Math.Max(Constitution, CurrentLevel.BaseStats.Constitution);
            Strength = Math.Max(Strength, CurrentLevel.BaseStats.Strength);
            Wisdom = Math.Max(Wisdom, CurrentLevel.BaseStats.Wisdom);
            Charisma = Math.Max(Charisma, CurrentLevel.BaseStats.Charisma);
            Intelligence = Math.Max(Intelligence, CurrentLevel.BaseStats.Intelligence);
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