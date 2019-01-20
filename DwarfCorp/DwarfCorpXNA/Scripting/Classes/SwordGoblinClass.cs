// SwordGoblinClass.cs
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class SwordGoblinClass : EmployeeClass
    {
        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Gobbo",
                    Pay = 25,
                    XP = 0,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 3,
                        Constitution = 3
                    }

                },
                new Level
                {
                    Index = 1,
                    Name = "Sneaker",
                    Pay = 50,
                    XP = 100,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 7,
                        Constitution = 6,
                        Charisma = 6
                    }
                },
                new Level
                {
                    Index = 2,
                    Name = "Slasher",
                    Pay = 100,
                    XP = 250,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 7,
                        Constitution = 7,
                        Charisma = 6
                    }
                },
                new Level
                {
                    Index = 3,
                    Name = "Piercer",
                    Pay = 200,
                    XP = 500,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 7,
                        Constitution = 7,
                        Charisma = 6,
                        Dexterity = 6
                    }
                },
                new Level
                {
                    Index = 4,
                    Name = "Smasher",
                    Pay = 500,
                    XP = 1000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 8,
                        Constitution = 7,
                        Charisma = 6,
                        Dexterity = 6
                    }
                },
                new Level
                {
                    Index = 5,
                    Name = "Master Smasher",
                    Pay = 1000,
                    XP = 5000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 9,
                        Constitution = 8,
                        Charisma = 7,
                        Dexterity = 7
                    }
                },
                new Level
                {
                    Index = 6,
                    Name = "Super Smasher",
                    Pay = 5000,
                    XP = 10000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 10,
                        Constitution = 8,
                        Charisma = 8,
                        Dexterity = 8
                    }
                },
                new Level
                {
                    Index = 7,
                    Name = "Stealer",
                    Pay = 10000,
                    XP = 20000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 10,
                        Constitution = 9,
                        Charisma = 9,
                        Dexterity = 9,
                        Intelligence = 6
                    }

                },
                new Level
                {
                    Index = 8,
                    Name = "Master Stealer",
                    Pay = 50000,
                    XP = 1000000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 10,
                        Constitution = 10,
                        Charisma = 10,
                        Dexterity = 10,
                        Intelligence = 6
                    }
                },
                new Level
                {
                    Index = 9,
                    Name = "Warlord",
                    Pay = 100000,
                    XP = 2000000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 10,
                        Constitution = 10,
                        Charisma = 10,
                        Dexterity = 10,
                        Intelligence = 10
                    }
                },
                new Level
                {
                    Index = 10,
                    Name = "King of Goblins",
                    Pay = 100000,
                    XP = 5000000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 10,
                        Constitution = 10,
                        Charisma = 10,
                        Dexterity = 10,
                        Intelligence = 10
                    }
                }
            };
        }

        void InitializeAnimations()
        {
            Animations = AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.Goblin.Sprites.goblin_animations, "Goblin");
        }

        void InitializeActions()
        {
            Actions =
                Task.TaskCategory.Chop |
                Task.TaskCategory.Gather |
                Task.TaskCategory.Guard |
                Task.TaskCategory.Attack;
        }

        public void InitializeWeapons()
        {
            Attacks = new List<Attack>()
            {
                new Attack("Sword", 2.0f, 0.5f, 2.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_ic_goblin_attack_1, ContentPaths.Audio.Oscar.sfx_ic_goblin_attack_2, ContentPaths.Audio.Oscar.sfx_ic_goblin_attack_3), ContentPaths.Effects.slash)
                {
                    Knockback = 2.5f,
                    TriggerMode = Attack.AttackTrigger.Animation,
                    TriggerFrame = 2
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "SwordGoblin";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }


        public SwordGoblinClass(bool initialize)
        {
            if (initialize && !staticsInitiailized)
            {
                InitializeStatics();
            }
        }
    }
}
