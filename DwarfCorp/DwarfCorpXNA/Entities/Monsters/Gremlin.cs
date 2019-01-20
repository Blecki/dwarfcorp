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
    public class Gremlin : Creature
    {
        public class GremlinClass : EmployeeClass
        {
            void InitializeLevels()
            {
                Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Tinkerer",
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
                    Name = "Inventor",
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
                    Name = "Mischief Maker",
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
                    Name = "Bombadier",
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
            };
            }

            void InitializeAnimations()
            {
                Animations = AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.Gremlin.gremlin_animations, "Gremlin");
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
                new Attack("Wrench", 1.0f, 1.0f, 1.5f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_ic_goblin_attack_1, ContentPaths.Audio.Oscar.sfx_ic_goblin_attack_2, ContentPaths.Audio.Oscar.sfx_ic_goblin_attack_3), ContentPaths.Effects.claw)
                {
                    Knockback = 2.5f,
                    TriggerMode = Attack.AttackTrigger.Animation,
                    TriggerFrame = 2
                }
            };
            }

            protected override sealed void InitializeStatics()
            {
                Name = "Gremlin";
                InitializeLevels();
                InitializeAnimations();
                InitializeWeapons();
                InitializeActions();
                base.InitializeStatics();
            }


            public GremlinClass()
            {
                if (!staticsInitiailized)
                {
                    InitializeStatics();
                }
            }
        }


        [EntityFactory("Gremlin")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Gremlin(
                new CreatureStats(SharedClass, 0),
                "Goblins",
                Manager.World.PlanService,
                Manager.World.Factions.Factions["Goblins"],
                Manager,
                "Gremlin",
                Position).Physics;
        }

        private static GremlinClass SharedClass = new GremlinClass();

        public Gremlin()
        {

        }

        public Gremlin(CreatureStats stats, string allies, PlanService planService, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, allies, planService, faction, name)
        {
            IsCloaked = true;

            Physics = new Physics(manager, "Gremlin", Matrix.CreateTranslation(position), new Vector3(0.5f, 0.9f, 0.5f), new Vector3(0.0f, 0.0f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            HasMeat = false;
            HasBones = false;

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            Physics.AddChild(new GremlinAI(Manager, "Gremlin AI", Sensors));

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

            Physics.Tags.Add("Gremlin");

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 3,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_ic_goblin_angered,
            });

            Physics.AddChild(new Flammable(Manager, "Flames"));

            Stats.FullName = TextGenerator.GenerateRandom("$goblinname");
            //Stats.LastName = TextGenerator.GenerateRandom("$goblinfamily");
            Stats.Size = 4;
            AI.Movement.CanClimbWalls = true;
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            AI.Movement.SetCan(MoveType.Dig, true);
            (AI as GremlinAI).DestroyPlayerObjectProbability = 0.5f;
            (AI as GremlinAI).PlantBomb = "Explosive";
            Species = "Gremlin";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Stats.CurrentClass = SharedClass;

            CreateSprite(Stats.CurrentClass, manager);
            Physics.AddChild(Shadow.Create(0.75f, manager));
            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 0, 5))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_goblin_angered,
            };

            base.CreateCosmeticChildren(manager);
        }
    }
}
