﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class CraftDwarfClass : EmployeeClass
    {
        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Craft Apprentice",
                    Pay = 25,
                    XP = 0,
                    BaseStats = new CreatureStats.StatNums()
                },
                new Level
                {
                    Index = 1,
                    Name = "Assistant Craftsdwarf",
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
                    Name = "Craftsdwarf",
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
                    Name = "Craft Engineer",
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
                    Name = "Craft Specialist",
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
                    Name = "Principal Crafter",
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
                    Name = "VP of Crafting",
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
                    Name = "President of Crafting",
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
                    Name = "Craft Analyst",
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
                    Name = "Craft Scientist",
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
                    Name = "Craft Overlord",
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
                GameMaster.ToolMode.Build,
                GameMaster.ToolMode.Attack,
                GameMaster.ToolMode.Gather,
                GameMaster.ToolMode.Farm,
                GameMaster.ToolMode.Craft
            };
        }

        void InitializeAnimations()
        {
            CompositeAnimation.Descriptor descriptor =
                FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(
                    ContentPaths.GetFileAsString(ContentPaths.Entities.Dwarf.Sprites.crafter_animation));
            Animations = new List<Animation>();
            Animations.AddRange(descriptor.GenerateAnimations(CompositeLibrary.Composites[CompositeLibrary.Dwarf]));
        }

        public void InitializeWeapons()
        {
            MeleeAttack = new Attack("Hammer", 1.0f, 1.0f, 1.0f, ContentPaths.Audio.hammer, "Dwarf")
            {
                Knockback = 2.5f,
                HitAnimation = new Animation(ContentPaths.Effects.flash, 32, 32, 0, 1, 2, 3)
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "Craftdwarf";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }
        public CraftDwarfClass()
        {
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }
    }
}
