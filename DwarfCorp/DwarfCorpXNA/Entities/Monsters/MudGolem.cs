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
        public MudGolem()
        {
            
        }

        public MudGolem(CreatureStats stats, string allies, PlanService planService, Faction faction, ComponentManager manager, string name, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, Vector3 position) :
            base(stats, allies, planService, faction, new Physics("MudGolem", manager.RootComponent, Matrix.CreateTranslation(position), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0)),
                 chunks, graphics, content, name)
        {
            Initialize();
        }

        public void Initialize()
        {
            HasMeat = false;
            HasBones = false;
            Physics.Orientation = Physics.OrientMode.RotateY;
            Sprite = new CharacterSprite(Graphics, Manager, "MudGolem Sprite", Physics, Matrix.CreateTranslation(new Vector3(0, 0.35f, 0)));
            foreach (Animation animation in Stats.CurrentClass.Animations)
            {
                Sprite.AddAnimation(animation.Clone());
            }

            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);

            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            AI = new MudGolemAI(this, Sensors, Manager.World.PlanService) { Movement = { IsSessile = true, CanFly = false, CanSwim = false, CanWalk = false, CanClimb = false, CanClimbWalls = false} };

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 16
                }
            };

            var gems = ResourceLibrary.GetResourcesByTag(Resource.ResourceTags.Gem);
            for (int i = 0; i < 16;  i++)
            {
                int num = MathFunctions.RandInt(1, 32 - i);
                Inventory.Resources.AddResource(new ResourceAmount(Datastructures.SelectRandom(gems), num));
                i += num - 1;
            }

            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

       
            Physics.Tags.Add("MudGolem");
            Physics.Mass = 100;
            DeathParticleTrigger = new ParticleTrigger("dirt_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.gravel
            };

            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.demon0,
                ContentPaths.Audio.gravel,
            };


            MinimapIcon minimapIcon = new MinimapIcon(Physics, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 3, 0));


            NoiseMaker.Noises["Chew"] = new List<string>
            {
                ContentPaths.Audio.chew
            };

            Stats.FullName = TextGenerator.GenerateRandom("$goblinname");
            //Stats.LastName = TextGenerator.GenerateRandom("$elffamily");
            Stats.Size = 4;
            Resistances[DamageType.Fire] = 5;
            Resistances[DamageType.Acid] = 5;
            Resistances[DamageType.Cold] = 5;
        }
    }

    [JsonObject(IsReference = true)]
    public class MudGolemAI : CreatureAI
    {
        public MudGolemAI()
        {
            
        }

        public MudGolemAI(Creature creature, EnemySensor enemySensor, PlanService planService) :
            base(creature, "MudGolemAI", enemySensor, planService)
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
            Actions = new List<GameMaster.ToolMode>()
            {
                GameMaster.ToolMode.Gather,
                GameMaster.ToolMode.Guard,
                GameMaster.ToolMode.Attack
            };
        }

        void InitializeAnimations()
        {
            CompositeAnimation.Descriptor descriptor =
            FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(
                ContentPaths.GetFileAsString(ContentPaths.Entities.mudman_animation));
            Animations = new List<Animation>();
            Animations.AddRange(descriptor.GenerateAnimations("MudGolem"));
        }

        public void InitializeWeapons()
        {
            Attacks = new List<Attack>()
            {
                new Attack("Mud", 0.1f, 1.0f, 50.0f, ContentPaths.Audio.demon_attack, ContentPaths.Effects.hit)
                {
                    Mode = Attack.AttackMode.Ranged,
                    LaunchSpeed = 10.0f,
                    ProjectileType = "Mud",
                    TriggerMode = Attack.AttackTrigger.Timer
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "MudGolem";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }
        public MudGolemClass()
        {
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }
    }

}
