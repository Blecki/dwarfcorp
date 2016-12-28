// CraftDwarfClass.cs
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

using System.Collections.Generic;

namespace DwarfCorp
{
    public class CraftDwarfClass : EmployeeClass
    {
        public CraftDwarfClass()
        {
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }

        private void InitializeLevels()
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
                    BaseStats = new CreatureStats.StatNums
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
                    BaseStats = new CreatureStats.StatNums
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
                    BaseStats = new CreatureStats.StatNums
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
                    BaseStats = new CreatureStats.StatNums
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
                    BaseStats = new CreatureStats.StatNums
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
                    BaseStats = new CreatureStats.StatNums
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
                    BaseStats = new CreatureStats.StatNums
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
                    BaseStats = new CreatureStats.StatNums
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
                    BaseStats = new CreatureStats.StatNums
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
                    BaseStats = new CreatureStats.StatNums
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

        private void InitializeActions()
        {
            Actions = new List<GameMaster.ToolMode>
            {
                GameMaster.ToolMode.Build,
                GameMaster.ToolMode.Attack,
                GameMaster.ToolMode.Gather,
                GameMaster.ToolMode.Farm,
                GameMaster.ToolMode.Craft,
                GameMaster.ToolMode.Cook
            };
        }

        private void InitializeAnimations()
        {
            var descriptor =
                FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(
                    ContentPaths.GetFileAsString(ContentPaths.Entities.Dwarf.Sprites.crafter_animation));
            Animations = new List<Animation>();
            Animations.AddRange(descriptor.GenerateAnimations(CompositeLibrary.Dwarf));
        }

        public void InitializeWeapons()
        {
            Attacks = new List<Attack>
            {
                new Attack("Hammer", 1.0f, 0.5f, 1.0f, ContentPaths.Audio.hammer, ContentPaths.Effects.hit)
                {
                    Knockback = 2.5f,
                    TriggerMode = Attack.AttackTrigger.Animation,
                    TriggerFrame = 2
                }
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
    }
}