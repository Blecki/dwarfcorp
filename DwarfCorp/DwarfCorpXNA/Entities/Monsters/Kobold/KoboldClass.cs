// Goblin.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    public class KoboldClass : EmployeeClass
    {
        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Sneaker",
                    Pay = 25,
                    XP = 0,
                    BaseStats = new StatAdjustment(5)
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
                    BaseStats = new StatAdjustment(5)
                    {
                        Strength = 7,
                        Constitution = 6,
                        Charisma = 6
                    }
                },
                new Level
                {
                    Index = 2,
                    Name = "Stealer",
                    Pay = 100,
                    XP = 250,
                    BaseStats = new StatAdjustment(5)
                    {
                        Strength = 7,
                        Constitution = 7,
                        Charisma = 6
                    }
                },
                new Level
                {
                    Index = 3,
                    Name = "Thief",
                    Pay = 200,
                    XP = 500,
                    BaseStats = new StatAdjustment(5)
                    {
                        Strength = 7,
                        Constitution = 7,
                        Charisma = 6,
                        Dexterity = 6
                    }
                },
            };
        }

        void InitializeAnimations()
        {
            Animations = AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.Kobold.kobold_animations, "Kobold");
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
                new Attack("Claws", 1.0f, 1.0f, 1.5f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_ic_goblin_attack_1, ContentPaths.Audio.Oscar.sfx_ic_goblin_attack_2, ContentPaths.Audio.Oscar.sfx_ic_goblin_attack_3), ContentPaths.Effects.claw)
                {
                    Knockback = 2.5f,
                    TriggerMode = Attack.AttackTrigger.Animation,
                    TriggerFrame = 2
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "Kobold";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }

        public KoboldClass()
        {
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }
    }
}
