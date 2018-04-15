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
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// Convenience class for initializing demons as creatures.
    /// </summary>
    public class MudGolem : Creature
    {
        [EntityFactory("MudGolem")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new MudGolem(
                new CreatureStats(new MudGolemClass(), 0),
                "dirt_particle",
                "Carnivore",
                Manager.World.PlanService,
                Manager.World.Factions.Factions["Carnivore"],
                Manager,
                "Mud Golem",
                Position);
        }

        [EntityFactory("SnowGolem")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new MudGolem(
                new CreatureStats(new SnowGolemClass(), 0),
                "snow_particle",
                "Carnivore",
                Manager.World.PlanService,
                Manager.World.Factions.Factions["Carnivore"],
                Manager,
                "Snow Golem",
                Position);
        }

        public MudGolem()
        {
            
        }

        public MudGolem(CreatureStats stats, string blood, string allies, PlanService planService, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, allies, planService, faction, name)
        {
            Physics = new Physics(Manager, name, Matrix.CreateTranslation(position), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.AddChild(new SelectionCircle(Manager)
            {
                IsVisible = false
            });

            Initialize(blood);
        }

        public void Initialize(string blood)
        {
            HasMeat = false;
            HasBones = false;
            Physics.Orientation = Physics.OrientMode.RotateY;
            CreateSprite(Stats.CurrentClass, Manager, 0.5f);
            Hands = Physics.AddChild(new Grabber("hands", Manager, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero)) as Grabber;

            Sensors = Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero)) as EnemySensor;

            AI = Physics.AddChild(new MudGolemAI(Manager, Sensors, Manager.World.PlanService) { Movement = { IsSessile = true, CanFly = false, CanSwim = false, CanWalk = false, CanClimb = false, CanClimbWalls = false } }) as CreatureAI;

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Inventory = Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset)) as Inventory;

            var gems = ResourceLibrary.GetResourcesByTag(Resource.ResourceTags.Gem);
            for (int i = 0; i < 16;  i++)
            {
                int num = MathFunctions.RandInt(1, 32 - i);
                Inventory.AddResource(new ResourceAmount(Datastructures.SelectRandom(gems), num));
                i += num - 1;
            }

            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

       
            Physics.Tags.Add("MudGolem");
            Physics.Mass = 100;

            Physics.AddChild(new ParticleTrigger(blood, Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.gravel
            });

            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.demon0,
                ContentPaths.Audio.gravel,
            };


            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 2, 1)));

            Stats.FullName = TextGenerator.GenerateRandom("$goblinname");
            //Stats.LastName = TextGenerator.GenerateRandom("$elffamily");
            Stats.Size = 4;
            Resistances[DamageType.Fire] = 5;
            Resistances[DamageType.Acid] = 5;
            Resistances[DamageType.Cold] = 5;
            Species = "Mud Golem";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(Stats.CurrentClass, manager);
            base.CreateCosmeticChildren(manager);
        }
    }

    [JsonObject(IsReference = true)]
    public class MudGolemAI : CreatureAI
    {
        public MudGolemAI()
        {
            
        }

        public MudGolemAI(ComponentManager Manager, EnemySensor enemySensor, PlanService planService) :
            base(Manager, "MudGolemAI", enemySensor, planService)
        {
            
        }
        public override Task ActOnIdle()
        {
            return null;
        }

        public override Act ActOnWander()
        {
            return null;
        }
    }

    public class MudGolemClass : EmployeeClass
    {
        public MudGolemClass()
        {
            if (!staticClassInitialized)
            {
                InitializeClassStatics();
            }
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }

        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "MudGolem",
                    Pay = 25,
                    XP = 0,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Constitution = 20.0f
                    }
                }
            };
        }

        void InitializeActions()
        {
            Actions =
                Task.TaskCategory.Gather |
                Task.TaskCategory.Guard |
                Task.TaskCategory.Attack;
        }

        void InitializeAnimations()
        {
            Animations = AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.mudman_animation, "MudGolem");
        }

        public void InitializeWeapons()
        {
            Attacks = new List<Attack>()
            {
                new Attack("Mud", 0.1f, 2.0f, 50.0f, ContentPaths.Audio.demon_attack, ContentPaths.Effects.hit)
                {
                    Mode = Attack.AttackMode.Ranged,
                    LaunchSpeed = 5.0f,
                    ProjectileType = "Mud",
                    TriggerMode = Attack.AttackTrigger.Animation,
                    TriggerFrame = 1
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "Mud Golem";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }
    }

    public class SnowGolemClass : EmployeeClass
    {
        public SnowGolemClass()
        {
            if (!staticClassInitialized)
            {
                InitializeClassStatics();
            }
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }

        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Snow Golem",
                    Pay = 25,
                    XP = 0,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Constitution = 20.0f
                    }
                }
            };
        }

        void InitializeActions()
        {
            Actions =
                Task.TaskCategory.Gather |
                Task.TaskCategory.Guard |
                Task.TaskCategory.Attack;
        }

        void InitializeAnimations()
        {
            Animations = AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.snowman_animation, "Snowman");
        }

        public void InitializeWeapons()
        {
            Attacks = new List<Attack>()
            {
                new Attack("Snowball", 0.1f, 1.0f, 50.0f, ContentPaths.Audio.demon_attack, ContentPaths.Effects.hit)
                {
                    Mode = Attack.AttackMode.Ranged,
                    LaunchSpeed = 10.0f,
                    ProjectileType = "Snowball",
                    TriggerMode = Attack.AttackTrigger.Timer
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "Snow Golem";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }
    }
}
