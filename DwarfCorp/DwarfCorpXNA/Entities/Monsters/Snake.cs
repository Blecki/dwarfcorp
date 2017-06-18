// Snake.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Snake : Creature, IUpdateableComponent
    {
        public class TailSegment
        {
            public Fixture Sprite;
            public Vector3 Target;
        }

        public List<TailSegment> Tail;
        
        public Snake()
        {
            
        }

        public Snake(SpriteSheet sprites, Vector3 position, ComponentManager manager, string name):
            base
            (
                manager,
                new CreatureStats
                {
                    Dexterity = 4,
                    Constitution = 6,
                    Strength = 9,
                    Wisdom = 2,
                    Charisma = 1,
                    Intelligence = 3,
                    Size = 3
                },
                "Carnivore",
                manager.World.PlanService,
                manager.World.Factions.Factions["Carnivore"],
                name
            )
        {
            Physics = new Physics
                (
                    manager,
                    "Giant Snake",
                    Matrix.CreateTranslation(position),
                    new Vector3(0.5f, 0.5f, 0.5f),
                    new Vector3(0, 0, 0),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                );

            Physics.AddChild(this);

            SelectionCircle = Physics.AddChild(new SelectionCircle(Manager)
            {
                IsVisible = false
            }) as SelectionCircle;

            Initialize(sprites);
        }

        public void Initialize(SpriteSheet spriteSheet)
        {
            Physics.Orientation = Physics.OrientMode.Fixed;

            const int frameWidth = 32;
            const int frameHeight = 32;

            Sprite = Physics.AddChild(new CharacterSprite
                (Graphics,
                Manager,
                "snake Sprite",
                Matrix.Identity
                )) as CharacterSprite;

            // Add the idle animation
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Forward, spriteSheet, 1, frameWidth, frameHeight, 0, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Left, spriteSheet, 1, frameWidth, frameHeight, 0, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Right, spriteSheet, 1, frameWidth, frameHeight, 0, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Backward, spriteSheet, 1, frameWidth, frameHeight, 0, 0);

            Tail = new List<TailSegment>();
            Physics.AddChild(new Shadow(Manager));
            for (int i = 0; i < 10; ++i)
            {
                Tail.Add(
                    new TailSegment()
                    {
                        Sprite = Manager.RootComponent.AddChild(new Fixture(Manager, Physics.LocalPosition, spriteSheet, new Point(1, 0))) as Fixture,
                        Target = Physics.LocalTransform.Translation
                    });
                Tail[i].Sprite.AddChild(new Shadow(Manager));
            }

            // Add sensor
            Sensors = Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero)) as EnemySensor;

            // Add AI
            AI = Physics.AddChild(new PacingCreatureAI(Manager, "snake AI", Sensors, PlanService)) as CreatureAI;


            Attacks = new List<Attack>() {new Attack("Bite", 50.0f, 1.0f, 3.0f, ContentPaths.Audio.hiss, ContentPaths.Effects.claws)};

            Inventory = Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.BoundingBoxPos)
            {
                Resources = new ResourceContainer()
                {
                    MaxResources = 1
                }
            }) as Inventory;

            Physics.Tags.Add("Snake");
            Physics.Tags.Add("Animal");
            AI.Movement.SetCan(MoveType.ClimbWalls, true);
            AI.Stats.FullName = "Giant Snake";
            AI.Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Giant Snake",
                Levels = new List<EmployeeClass.Level>()
                {
                    new EmployeeClass.Level()
                    {
                        BaseStats = new CreatureStats.StatNums()
                        {
                            Charisma = AI.Stats.Charisma,
                            Constitution = AI.Stats.Constitution,
                            Dexterity = AI.Stats.Dexterity,
                            Intelligence = AI.Stats.Intelligence,
                            Size = AI.Stats.Size,
                            Strength = AI.Stats.Strength,
                            Wisdom = AI.Stats.Wisdom
                        },
                        Name = "Giant Snake",
                        Index = 0
                    },
                  
                }
            };
            AI.Stats.LevelIndex = 0;

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                BoxTriggerTimes = 10,
                SoundToPlay = ContentPaths.Entities.Dwarf.Audio.dwarfhurt1,
            });

            Physics.AddChild(new Flammable(Manager, "Flames"));
            HasBones = true;
            HasMeat = true;
            Species = "Snake";
        }

        public override void Die()
        {
            foreach (var tail in Tail)
            {
                tail.Sprite.Die();
            }
            base.Die();
        }

        public override void Delete()
        {
            foreach (var tail in Tail)
            {
                tail.Sprite.Delete();
            }
            base.Delete();
        
        }

        public new void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if ((Physics.Position - Tail.First().Target).LengthSquared() > 0.5f)
            {
                for (int i = Tail.Count  - 1; i > 0; i--)
                {
                    Tail[i].Target = Tail[i - 1].Target;
                }
                Tail[0].Target = Physics.Position;
            }

            foreach (var tail in Tail)
            {
                tail.Sprite.LocalPosition = 0.9f*tail.Sprite.LocalPosition + 0.1f*tail.Target;
            }
            base.Update(gameTime, chunks, camera);
        }
    }
}
