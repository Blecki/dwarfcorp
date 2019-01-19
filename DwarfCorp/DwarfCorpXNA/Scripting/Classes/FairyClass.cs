// FairyClass.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class FairyClass : EmployeeClass
    {
        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Fairy",
                    Pay = 0,
                    XP = 0,
                   
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Constitution = 1.0f,
                        Dexterity = 10,
                        Charisma = 10,
                        Intelligence = 10,
                        Size = 1,
                        Strength = 1,
                        Wisdom = 10
                    }
                },
                new Level
                {
                    Index = 0,
                    Name = "Fairy",
                    Pay = 0,
                    XP = 9999999,
                   
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Constitution = 1.0f,
                        Dexterity = 10,
                        Charisma = 10,
                        Intelligence = 10,
                        Size = 1,
                        Strength = 1,
                        Wisdom = 10
                    }
                }
            };
        }

        void InitializeActions()
        {
            Actions =
                Task.TaskCategory.Gather |
                Task.TaskCategory.Dig |
                Task.TaskCategory.CraftItem |
                Task.TaskCategory.BuildBlock |
                Task.TaskCategory.BuildObject |
                Task.TaskCategory.BuildZone |
                Task.TaskCategory.Chop |
                Task.TaskCategory.TillSoil |
                Task.TaskCategory.Plant |
                Task.TaskCategory.Wrangle;
        }

        void InitializeAnimations()
        {
            Animations = AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.Dwarf.Sprites.fairy_animation, "Dwarf");
        }

        public void InitializeWeapons()
        {
            Attacks = new List<Attack>()
            {
                new Attack("Fairy Dust", 10.0f, 0.2f, 2.0f, SoundSource.Create(ContentPaths.Audio.tinkle), ContentPaths.Effects.hit)
                {
                    Knockback = 0.5f,
                    HitParticles = "star_particle",
                    TriggerMode = Attack.AttackTrigger.Animation,
                    TriggerFrame = 2
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "Fairy Helper";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }
        public FairyClass(bool initialize)
        {
            if (initialize && !staticsInitiailized)
            {
                InitializeStatics();
            }
        }
    }
}
