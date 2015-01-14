using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
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
        public Attack MeleeAttack { get; set; }
        public List<Level> Levels { get; set; }
        public string Name { get; set; }
        public List<GameMaster.ToolMode> Actions { get; set; } 

        protected bool staticsInitiailized = false;

        protected virtual void InitializeStatics()
        {
            staticsInitiailized = true;
        }

        public bool HasAction(GameMaster.ToolMode action)
        {
            return Actions.Contains(action);
        }

    }
}
