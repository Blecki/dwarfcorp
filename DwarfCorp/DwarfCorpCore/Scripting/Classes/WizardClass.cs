using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class WizardClass : EmployeeClass
    {
        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Bachelor of Magical Studies",
                    Pay = 25,
                    XP = 0,
                    BaseStats = new CreatureStats.StatNums()
                },
                new Level
                {
                    Index = 1,
                    Name = "Master of Magical Studies",
                    Pay = 50,
                    XP = 100,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Intelligence = 6,
                        Constitution = 6
                    }
                },
                new Level
                {
                    Index = 2,
                    Name = "PhM Candidate",
                    Pay = 100,
                    XP = 250,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Intelligence = 7,
                        Constitution = 6,
                        Charisma = 6
                    }
                },
                new Level
                {
                    Index = 3,
                    Name = "Adjunct Wizard",
                    Pay = 200,
                    XP = 500,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Intelligence = 7,
                        Constitution = 7,
                        Charisma = 6,
                        Dexterity = 6
                    }
                },
                new Level
                {
                    Index = 4,
                    Name = "Associate Wizard",
                    Pay = 500,
                    XP = 1000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Intelligence = 8,
                        Constitution = 7,
                        Charisma = 6,
                        Dexterity = 6
                    }
                },
                new Level
                {
                    Index = 5,
                    Name = "Tenured Wizard",
                    Pay = 1000,
                    XP = 5000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Intelligence = 9,
                        Constitution = 8,
                        Charisma = 7,
                        Dexterity = 7
                    }
                },
                new Level
                {
                    Index = 6,
                    Name = "Wizarding Fellow",
                    Pay = 5000,
                    XP = 10000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Intelligence = 10,
                        Constitution = 8,
                        Charisma = 8,
                        Dexterity = 8
                    }
                },
                new Level
                {
                    Index = 7,
                    Name = "Dean of Wizarding",
                    Pay = 10000,
                    XP = 20000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Intelligence = 10,
                        Constitution = 9,
                        Charisma = 9,
                        Dexterity = 9,
                        Strength = 6
                    }

                },
                new Level
                {
                    Index = 8,
                    Name = "Chair of Wizarding",
                    Pay = 50000,
                    XP = 1000000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Intelligence = 10,
                        Constitution = 10,
                        Charisma = 10,
                        Dexterity = 10,
                        Strength = 6
                    }
                },
                new Level
                {
                    Index = 9,
                    Name = "Magical Provost",
                    Pay = 100000,
                    XP = 2000000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Intelligence = 10,
                        Constitution = 10,
                        Charisma = 10,
                        Dexterity = 10,
                        Strength = 10
                    }
                },
                new Level
                {
                    Index = 10,
                    Name = "Wizard Emeritus",
                    Pay = 100000,
                    XP = 5000000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Intelligence = 10,
                        Constitution = 10,
                        Charisma = 10,
                        Dexterity = 10,
                        Strength = 10
                    }
                }
            };
        }

        void InitializeActions()
        {
            Actions = new List<GameMaster.ToolMode>()
            {
                GameMaster.ToolMode.Gather,
                GameMaster.ToolMode.Magic,
                GameMaster.ToolMode.Attack
            };
        }

        void InitializeAnimations()
        {
            CompositeAnimation.Descriptor descriptor =
    FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(
        ContentPaths.GetFileAsString(ContentPaths.Entities.Dwarf.Sprites.wizard_animation));
            Animations = new List<Animation>();
            Animations.AddRange(descriptor.GenerateAnimations(CompositeLibrary.Dwarf));
        }

        public void InitializeWeapons()
        {
            Attacks = new List<Attack>()
            {
                new Attack("Magic Missile", 0.1f, 1.0f, 1.0f, ContentPaths.Audio.tinkle, ContentPaths.Effects.rings)
                {
                    Knockback = 0.5f,
                    HitParticles = "star_particle"
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "Wizard";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }
        public WizardClass()
        {
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }
    }
}
