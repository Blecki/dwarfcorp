// WorkerClass.cs
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
    public class MusketDwarfClass : EmployeeClass
    {
        public MusketDwarfClass()
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
                    Name = "Hired Musket",
                    Pay = 25,
                    XP = 0,
                    BaseStats = new CreatureStats.StatNums()
                },
                new Level
                {
                    Index = 1,
                    Name = "Musketdwarf",
                    Pay = 50,
                    XP = 100,
                    BaseStats = new CreatureStats.StatNums
                    {
                        Strength = 6,
                        Constitution = 6,
                        Charisma = 6
                    }
                },
                new Level
                {
                    Index = 2,
                    Name = "Private",
                    Pay = 100,
                    XP = 250,
                    BaseStats = new CreatureStats.StatNums
                    {
                        Strength = 7,
                        Constitution = 6,
                        Charisma = 6
                    }
                },
                new Level
                {
                    Index = 3,
                    Name = "Corporal",
                    Pay = 200,
                    XP = 500,
                    BaseStats = new CreatureStats.StatNums
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
                    Name = "Sergant",
                    Pay = 500,
                    XP = 1000,
                    BaseStats = new CreatureStats.StatNums
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
                    Name = "Master Sergant",
                    Pay = 1000,
                    XP = 5000,
                    BaseStats = new CreatureStats.StatNums
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
                    Name = "Lieutenant",
                    Pay = 5000,
                    XP = 10000,
                    BaseStats = new CreatureStats.StatNums
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
                    Name = "Major",
                    Pay = 10000,
                    XP = 20000,
                    BaseStats = new CreatureStats.StatNums
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
                    Name = "Colonel",
                    Pay = 50000,
                    XP = 1000000,
                    BaseStats = new CreatureStats.StatNums
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
                    Name = "General",
                    Pay = 100000,
                    XP = 2000000,
                    BaseStats = new CreatureStats.StatNums
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
                    Name = "Commander in Chief",
                    Pay = 100000,
                    XP = 5000000,
                    BaseStats = new CreatureStats.StatNums
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

        private void InitializeActions()
        {
            Actions =
                Task.TaskCategory.Attack |
                Task.TaskCategory.Guard |
                Task.TaskCategory.Gather;
        }

        private void InitializeAnimations()
        {
            /*
            Texture2D dwarfSprites = TextureManager.GetTexture(ContentPaths.Entities.Dwarf.Sprites.dwarf_animations);
            Animations = Dwarf.CreateDefaultAnimations(dwarfSprites, 32, 40);
             */
            var descriptor =
                FileUtils.LoadJsonFromString<AnimationSetDescriptor>(
                    ContentPaths.GetFileAsString(ContentPaths.Entities.Dwarf.Sprites.musketdwarf_animations));
            Animations = new List<Animation>();
            Animations.AddRange(descriptor.GenerateAnimations(CompositeLibrary.Dwarf));
        }

        public void InitializeWeapons()
        {
            Attacks = new List<Attack>
            {
                new Attack("Musket", 20.0f, 2.0f, 5.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_ic_dwarf_attack_musket_1,
                    ContentPaths.Audio.Oscar.sfx_ic_dwarf_attack_musket_2, ContentPaths.Audio.Oscar.sfx_ic_dwarf_attack_musket_3), ContentPaths.Effects.explode)
                {
                    Mode = Attack.AttackMode.Ranged,
                    LaunchSpeed = 30.0f,
                    ProjectileType = "Bullet",
                    TriggerMode = Attack.AttackTrigger.Animation,
                    TriggerFrame = 2
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "Musket";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }
    }
}