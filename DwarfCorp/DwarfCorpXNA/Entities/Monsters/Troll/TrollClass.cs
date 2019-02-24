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
    public class TrollClass : EmployeeClass
    {
        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Troll",
                    Pay = 25,
                    XP = 0,
                    BaseStats = new StatAdjustment(5)
                    {
                        Strength = 8,
                        Constitution = 9,
                        Intelligence = 1,
                        Size = 10,
                        Dexterity = 3
                    }

                }
            };
        }

        void InitializeAnimations()
        {
            Animations = AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.Troll.troll_animation, "Troll");
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
                new Attack("Slam", 15.0f, 2.0f, 5.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_ic_demon_hurt_1, ContentPaths.Audio.Oscar.sfx_ic_demon_hurt_2, ContentPaths.Audio.Oscar.sfx_ic_demon_mumble_1), ContentPaths.Effects.rings)
                {
                    Knockback = 2.5f,
                    TriggerMode = Attack.AttackTrigger.Animation,
                    TriggerFrame = 3,
                    Mode = Attack.AttackMode.Area,
                    HitParticles = "dirt_particle"
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "Troll";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }


        public TrollClass()
        {
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }
    }
}
