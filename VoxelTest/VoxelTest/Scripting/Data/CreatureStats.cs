using System.IO;

namespace DwarfCorp
{

    /// <summary>
    /// A set of simple numbers which define how a creature is to behave.
    /// </summary>
    public class CreatureStats
    {
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
        public float BaseDigSpeed { get { return Strength * 4.0f; }}
        public float BaseChopSpeed { get { return Strength * 3.0f + Dexterity * 1.0f; } }
        public float JumpForce { get { return 1000.0f; } }
        public float MaxHealth { get { return (Strength + Constitution + Size) * 10.0f; }}

        public float EatSpeed { get { return Size + Strength; }}

        public float HungerGrowth { get { return Size * 0.0025f; } }

        public float Tiredness
        {
            get
            {
                if(CanSleep)
                {
                    return 0.25f / Constitution;
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

        public int XP { get; set; }

        public CreatureStats()
        {
            CanSleep = false;
            FirstName = "";
            LastName = "";
            CurrentClass = new WorkerClass();
            LevelIndex = 0;
            XP = 0;
        }
    }

}