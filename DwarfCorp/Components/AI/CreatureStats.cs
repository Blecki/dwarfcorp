using System;
using System.IO;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public partial class CreatureStats
    {
        #region Stat Stack

        [JsonProperty] private List<StatAdjustment> StatAdjustments = new List<StatAdjustment>();

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

        #endregion

        public float MaxSpeed => Dexterity;
        public float MaxAcceleration => MaxSpeed * 2.0f;
        public float StoppingForce => MaxAcceleration * 6.0f;
        public float BaseDigSpeed => Strength + Size;
        public float BaseChopSpeed => Strength * 3.0f + Dexterity * 1.0f;
        public float JumpForce => 1000.0f;
        public float MaxHealth => (Strength + Constitution + Size) * 10.0f;
        public float EatSpeed => Size + Strength;
        public float HungerGrowth => Size * 0.025f;
        public float BaseFarmSpeed => Intelligence + Strength;
        public float BuildSpeed => (Intelligence + Dexterity) / 10.0f;
        public float HungerResistance => Constitution;

        public bool CanEat = false;
        public int Age = 0;
        public int RandomSeed;
        public float VoicePitch = 1.0f;
        public Gender Gender = Gender.Male;
        public string FullName = "";
        public string Title = "";
        public int NumBlocksDestroyed = 0;
        public int NumItemsGathered = 0;
        public int NumRoomsBuilt = 0;
        public int NumThingsKilled = 0;
        public int NumBlocksPlaced = 0;
        public int XP = 0;

        public String ClassName = "";
        [JsonIgnore] public MaybeNull<CreatureClass> CurrentClass { get; private set; }

        public int GetCurrentLevel()
        {
            var baseStats = FindAdjustment("base stats");
            if (baseStats != null)
                return (int)(baseStats.Charisma + baseStats.Constitution + baseStats.Dexterity + baseStats.Intelligence + baseStats.Strength + baseStats.Wisdom);
            return 0;
        }

        public static int GetLevelUpCost(int CurrentLevel)
        {
            return CurrentLevel * GameSettings.Current.DwarfBaseLevelCost;
        }

        public String SpeciesName = "";
        [JsonIgnore] public MaybeNull<CreatureSpecies> Species { get; private set; }

        public DwarfBux DailyPay => GameSettings.Current.DwarfBasePay * GetCurrentLevel();

        public TaskCategory AllowedTasks = TaskCategory.Attack | TaskCategory.Gather | TaskCategory.Plant | TaskCategory.Harvest | TaskCategory.Chop | TaskCategory.Wrangle | TaskCategory.TillSoil;
        [JsonIgnore] public bool IsOverQualified => XP >= GetLevelUpCost(GetCurrentLevel());
        [JsonIgnore] public bool CanSpendXP => GetLevelUpCost(GetCurrentLevel()) <= XP;
        public bool IsAsleep = false;
        public float HungerDamageRate = 10.0f;
        public bool IsOnStrike = false;
        public DwarfBux Money = 0;
        public bool IsFleeing = false;

        public bool IsManager = false; // This creature is flagged as a manager.

        public bool IsTaskAllowed(TaskCategory TaskCategory)
        {
            if (IsManager && (TaskCategory & (TaskCategory.Attack | TaskCategory.Guard | TaskCategory.Gather | TaskCategory.BuildObject)) != TaskCategory)
                return false;
            return (AllowedTasks & TaskCategory) == TaskCategory;
        }

        public CreatureStats()
        {
            Age = (int)Math.Max(MathFunctions.RandNormalDist(35, 15), 15);
            RandomSeed = MathFunctions.RandInt(int.MinValue, int.MaxValue);

            if (FindAdjustment("base stats") == null)
                AddStatAdjustment(new StatAdjustment { Name = "base stats" });
        }

        [OnDeserialized]
        void OnDeserializing(StreamingContext context)
        {
            if (Library.GetClass(ClassName).HasValue(out var creatureClass))
                CurrentClass = creatureClass;
            Species = Library.GetSpecies(SpeciesName);
        }

        public CreatureStats(String SpeciesName, String ClassName, MaybeNull<Loadout> Loadout) : this()
        {
            this.ClassName = ClassName;
            CurrentClass = Library.GetClass(ClassName);

            this.SpeciesName = SpeciesName;
            Species = Library.GetSpecies(SpeciesName);

            XP = 0;

            if (Loadout.HasValue(out var loadout))
            {
                AllowedTasks = loadout.Actions;

                BaseCharisma = loadout.StartingStats.Charisma;
                BaseConstitution = loadout.StartingStats.Constitution;
                BaseDexterity = loadout.StartingStats.Dexterity;
                BaseIntelligence = loadout.StartingStats.Intelligence;
                BaseStrength = loadout.StartingStats.Strength;
                BaseWisdom = loadout.StartingStats.Wisdom;

                Title = loadout.Name;
                IsManager = loadout.StartAsManager;
            }
            else if (CurrentClass.HasValue(out var currentClass))
            { 
                AllowedTasks = currentClass.Actions;
                BaseCharisma = currentClass.Levels[0].BaseStats.Charisma;
                BaseConstitution = currentClass.Levels[0].BaseStats.Constitution;
                BaseDexterity = currentClass.Levels[0].BaseStats.Dexterity;
                BaseIntelligence = currentClass.Levels[0].BaseStats.Intelligence;
                BaseStrength = currentClass.Levels[0].BaseStats.Strength;
                BaseWisdom = currentClass.Levels[0].BaseStats.Wisdom;
                Title = currentClass.Levels[0].Name;
            }
            else
            {

            }
        }
    }
}
