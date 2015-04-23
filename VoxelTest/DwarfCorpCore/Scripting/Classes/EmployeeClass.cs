using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class EmployeeClassDef
    {
        public string Name { get; set; }
        public string Animations { get; set; }
        public List<EmployeeClass.Level> Levels { get; set; }
        public List<Attack> Attacks { get; set; }
        public List<string> Actions { get; set; } 
    }

    [JsonObject(IsReference = true)]
    public class EmployeeClass 
    {
        public class Level
        {
            public int Index;
            public string Name;
            public float Pay;
            public int XP;
            public CreatureStats.StatNums BaseStats;
        }

        public Texture2D SpriteSheet { get; set; }
        public List<Animation> Animations { get; set; }
        public List<Attack> Attacks { get; set; }
        public List<Level> Levels { get; set; }
        public string Name { get; set; }
        public List<GameMaster.ToolMode> Actions { get; set; } 

        [JsonIgnore]
        public static Dictionary<string, EmployeeClass> Classes { get; set; } 

        protected static bool staticClassInitialized = false;
        protected bool staticsInitiailized = false;

        public EmployeeClass()
        {
            
        }

        public EmployeeClass(EmployeeClassDef definition)
        {
            Name = definition.Name;
            Levels = definition.Levels;
            Actions = new List<GameMaster.ToolMode>();
            foreach (string s in definition.Actions)
            {
                GameMaster.ToolMode value = GameMaster.ToolMode.SelectUnits;
                if (Enum.TryParse(s, true, out value))
                {
                    Actions.Add(value);
                }
            }

            CompositeAnimation.Descriptor descriptor = FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(ContentPaths.GetFileAsString(definition.Animations));
            Animations = new List<Animation>();
            Animations.AddRange(descriptor.GenerateAnimations(Name));

            Attacks = definition.Attacks;
        }

        public static void AddClasses(string file)
        {
            if (!staticClassInitialized)
            {
                InitializeClassStatics();
            }
            List<EmployeeClassDef> defs = ContentPaths.LoadFromJson<List<EmployeeClassDef>>(file);

            foreach (EmployeeClassDef empClass in defs)
            {
                Classes[empClass.Name] = new EmployeeClass(empClass);
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

        public bool HasAction(GameMaster.ToolMode action)
        {
            return Actions != null && Actions.Contains(action);
        }

    }
}
