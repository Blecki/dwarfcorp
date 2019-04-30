using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class SnowGolemClass : EmployeeClass
    {
        public SnowGolemClass()
        {
            if (!staticClassInitialized)
            {
                InitializeClassStatics();
            }
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }

        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Snow Golem",
                    Pay = 25,
                    XP = 0,
                    BaseStats = new StatAdjustment(5)
                    {
                        Constitution = 20.0f
                    }
                }
            };
        }

        void InitializeActions()
        {
            Actions =
                Task.TaskCategory.Gather |
                Task.TaskCategory.Guard |
                Task.TaskCategory.Attack;
        }

        public void InitializeWeapons()
        {
            Attacks = new List<Attack>()
            {
                new Attack("Snowball", 0.1f, 1.0f, 50.0f, ContentPaths.Audio.demon_attack, ContentPaths.Effects.hit)
                {
                    Mode = Attack.AttackMode.Ranged,
                    LaunchSpeed = 10.0f,
                    ProjectileType = "Snowball",
                    TriggerMode = Attack.AttackTrigger.Timer
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "Snow Golem";
            InitializeLevels();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }
    }
}
