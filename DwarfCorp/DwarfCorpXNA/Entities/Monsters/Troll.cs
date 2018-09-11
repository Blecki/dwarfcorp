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
    public class Troll : Creature
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
                    BaseStats = new CreatureStats.StatNums()
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


        [EntityFactory("Troll")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Troll(
                new CreatureStats(new TrollClass(), 0),
                "Goblins",
                Manager.World.PlanService,
                Manager.World.Factions.Factions["Goblins"],
                Manager,
                "Troll",
                Position).Physics;
        }

        public Troll()
        {

        }
        public Troll(CreatureStats stats, string allies, PlanService planService, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, allies, planService, faction, name)
        {
            Physics = new Physics(manager, "Troll", Matrix.CreateTranslation(position), new Vector3(0.5f, 0.9f, 0.5f), new Vector3(0.0f, 0.0f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Initialize();
        }

        public void Initialize()
        {
            Physics.Orientation = Physics.OrientMode.RotateY;
            CreateSprite(Stats.CurrentClass, Manager, 0.25f);

            HasMeat = true;
            HasBones = true;

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            Physics.AddChild(new CreatureAI(Manager, "Troll AI", Sensors, PlanService));

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

            Physics.AddChild(Shadow.Create(0.75f, Manager));

            Physics.Tags.Add("Troll");

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 3,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_ic_demon_hurt_1,
            });

            Physics.AddChild(new Flammable(Manager, "Flames"));


            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_goblin_angered,
            };


            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 1, 2)));

            Stats.FullName = TextGenerator.GenerateRandom("$goblinname");
            //Stats.LastName = TextGenerator.GenerateRandom("$goblinfamily");
            AI.Movement.CanClimbWalls = true;
            AI.Movement.CanSwim = true;
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            AI.Movement.SetSpeed(MoveType.Swim, 0.15f);
            AI.Movement.SetCan(MoveType.Dig, true);
            Species = "Troll";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(Stats.CurrentClass, manager);
            Physics.AddChild(Shadow.Create(0.75f, manager));
            base.CreateCosmeticChildren(manager);
        }
    }

}
