// Elf.cs
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

    /// <summary>
    /// Convenience class for initializing demons as creatures.
    /// </summary>
    public class Demon : Creature
    {
        public Demon()
        {
            
        }

        public Demon(CreatureStats stats, string allies, PlanService planService, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, allies, planService, faction, name)
        {
            Physics = new Physics(manager, "Demon", Matrix.CreateTranslation(position), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.AddChild(new SelectionCircle(Manager)
            {
                IsVisible = false
            });

            Initialize();
        }

        public void Initialize()
        {
            Physics.Orientation = Physics.OrientMode.RotateY;
            CreateSprite(Stats.CurrentClass, Manager);
            Hands = Physics.AddChild(new Grabber("hands", Manager, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero)) as Grabber;

            Sensors = Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero)) as EnemySensor;

            AI = Physics.AddChild(new PacingCreatureAI(Manager, "Demon AI", Sensors, PlanService) { Movement = { CanFly = true, CanSwim = false } }) as CreatureAI;

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Inventory = Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.BoundingBoxPos)) as Inventory;

            Physics.AddChild(Shadow.Create(0.75f, Manager));

            Physics.Tags.Add("Demon");

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_ic_demon_death
            });

            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_demon_hurt_1,
                ContentPaths.Audio.Oscar.sfx_ic_demon_hurt_2,
            };


            MinimapIcon minimapIcon = Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 3, 1))) as MinimapIcon;



            NoiseMaker.Noises["Chew"] = new List<string>
            {
                ContentPaths.Audio.chew
            };

            NoiseMaker.Noises["Jump"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_demon_angered,
            };

            NoiseMaker.Noises["Flap"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_demon_flap_wings_1,
                ContentPaths.Audio.Oscar.sfx_ic_demon_flap_wings_2,
                ContentPaths.Audio.Oscar.sfx_ic_demon_flap_wings_3,
            };


            NoiseMaker.Noises["Chirp"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_demon_mumble_1,
                ContentPaths.Audio.Oscar.sfx_ic_demon_mumble_2,
                ContentPaths.Audio.Oscar.sfx_ic_demon_mumble_3,
                ContentPaths.Audio.Oscar.sfx_ic_demon_mumble_4,
                ContentPaths.Audio.Oscar.sfx_ic_demon_mumble_5,
                ContentPaths.Audio.Oscar.sfx_ic_demon_mumble_6,
                ContentPaths.Audio.Oscar.sfx_ic_demon_pleased,
            };

            Stats.FullName = TextGenerator.GenerateRandom("$goblinname");
            //Stats.LastName = TextGenerator.GenerateRandom("$elffamily");
            Stats.Size = 4;
            Species = "Demon";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(Stats.CurrentClass, manager);
            Physics.AddChild(Shadow.Create(0.75f, manager));
            base.CreateCosmeticChildren(manager);
        }
    }

    public class DemonClass : EmployeeClass
    {
        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Demon",
                    Pay = 25,
                    XP = 0,
                    BaseStats = new CreatureStats.StatNums()
                },
                new Level
                {
                    Index = 1,
                    Name = "Demon",
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
                    Name = "Demon",
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
                    Name = "Demon",
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
                    Name = "Demon",
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
                    Name = "Demon",
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
                    Name = "Demon",
                    Pay = 5000,
                    XP = 10000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Intelligence = 10,
                        Constitution = 8,
                        Charisma = 8,
                        Dexterity = 8
                    }
                }
            };
        }

        void InitializeActions()
        {
            Actions = Task.TaskCategory.Gather | Task.TaskCategory.Guard | Task.TaskCategory.Attack;
        }

        void InitializeAnimations()
        {
            var descriptor =
    FileUtils.LoadJsonFromString<AnimationSetDescriptor>(
        ContentPaths.GetFileAsString(ContentPaths.Entities.Demon.demon_animations));
            Animations = new List<Animation>();
            Animations.AddRange(descriptor.GenerateAnimations(CompositeLibrary.Demon));
        }

        public void InitializeWeapons()
        {
            Attacks = new List<Attack>()
            {
                new Attack("Fireball", 0.1f, 1.0f, 5.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_ic_demon_fire_spit_1, ContentPaths.Audio.Oscar.sfx_ic_demon_fire_spit_2), ContentPaths.Effects.hit)
                {
                    Mode = Attack.AttackMode.Ranged,
                    LaunchSpeed = 10.0f,
                    ProjectileType = "Fireball",
                    TriggerMode = Attack.AttackTrigger.Animation,
                    TriggerFrame = 2,
                    DamageAmount = 15
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "Demon";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }
        public DemonClass()
        {
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }
    }

}
