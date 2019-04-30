using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public class EmployeeClass 
    {
        public class Level
        {
            public int Index; // Todo: Kill this.
            public string Name;
            public DwarfBux Pay;
            public int XP;
            public StatAdjustment BaseStats;
            public List<Attack> ExtraAttacks = new List<Attack>();
            public int HealingPower = 0;
        }

        [JsonIgnore]
        public List<Animation> Animations { get; set; }
        public String AnimationFilename;
        public string MinecartAnimations { get; set; }
        public List<Attack> Attacks { get; set; }
        public List<Level> Levels { get; set; }
        public string Name { get; set; }
        public Task.TaskCategory Actions = Task.TaskCategory.None;
        public CharacterMode AttackMode;

        // Todo: Should just include name of attack animation. Kinda what the AttackMode is.

        public bool IsTaskAllowed(Task.TaskCategory TaskCategory)
        {
            return (Actions & TaskCategory) == TaskCategory;
        }

        [JsonIgnore]
        public static Dictionary<string, EmployeeClass> Classes { get; set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (!String.IsNullOrEmpty(AnimationFilename))
                Animations = AnimationLibrary.LoadCompositeAnimationSet(AnimationFilename, Name);
        }

        protected static bool staticClassInitialized = false;
        protected bool staticsInitiailized = false;

        public EmployeeClass()
        {
            if (!staticClassInitialized)
            {
                InitializeClassStatics();
            }
        }

        protected virtual void InitializeStatics()
        {
            staticsInitiailized = true;
        }

        protected static void InitializeClassStatics()
        {
            if (!staticClassInitialized)
            {
                Classes = new Dictionary<string, EmployeeClass>();
                staticClassInitialized = true;
            }
        }
    }
}
