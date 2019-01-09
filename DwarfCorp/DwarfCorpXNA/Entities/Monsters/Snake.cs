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
    public class Snake : Creature
    {
        [EntityFactory("Snake")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var r =  new Snake(false, Position, Manager, "Snake").Physics;
            r.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 2, 4)));
            return r;
        }

        [EntityFactory("Necrosnake")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var r = new Snake(true, Position, Manager, "Snake");
            r.Attacks[0].DiseaseToSpread = "Necrorot";
            r.Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 1, 4)));
            return r.Physics;
        }
        
        public class TailSegment
        {
            public Body Sprite;
            public Vector3 Target;
        }

        [JsonIgnore]
        public List<TailSegment> Tail;

        public bool Bonesnake = false;
        
        public Snake()
        {
            UpdateRate = 1;
        }

        public Snake(bool Bone, Vector3 position, ComponentManager manager, string name):
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
            UpdateRate = 1;
            Bonesnake = Bone;
            HasMeat = !Bone;
            HasBones = true;
            _maxPerSpecies = 4;
            Physics = new Physics
                (
                    manager,
                    name,
                    Matrix.CreateTranslation(position),
                    new Vector3(0.5f, 0.5f, 0.5f),
                    new Vector3(0, 0, 0),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                );

            Physics.AddChild(this);

            Initialize();
        }

        public void Initialize()
        {
            Physics.Orientation = Physics.OrientMode.Fixed;
            Species = "Snake";
            CreateGraphics();

            // Add sensor
            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            // Add AI
            Physics.AddChild(new PacingCreatureAI(Manager, "snake AI", Sensors));


            Attacks = new List<Attack>()
            {
                new Attack("Bite", 50.0f, 1.0f, 3.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_giant_snake_attack_1), ContentPaths.Effects.bite)
                {
                    TriggerMode = Attack.AttackTrigger.Animation,
                    TriggerFrame = 2,
                }
            };

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Physics.Tags.Add("Snake");
            Physics.Tags.Add("Animal");
            AI.Movement.SetCan(MoveType.ClimbWalls, true);
            AI.Movement.SetCan(MoveType.Dig, true);
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
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_oc_giant_snake_hurt_1,
            });


            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_giant_snake_hurt_1 };
            NoiseMaker.Noises["Chirp"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_oc_giant_snake_neutral_1,
                ContentPaths.Audio.Oscar.sfx_oc_giant_snake_neutral_2
            };

            Physics.AddChild(new Flammable(Manager, "Flames"));
        }

        private void CreateGraphics()
        {
            Physics.AddChild(new Shadow(Manager));

            var animFile = Bonesnake ? ContentPaths.Entities.Animals.Snake.bonesnake_animation :
                ContentPaths.Entities.Animals.Snake.snake_animation;

            var tailFile = Bonesnake ? ContentPaths.Entities.Animals.Snake.bonetail_animation :
                ContentPaths.Entities.Animals.Snake.tail_animation;


            var sprite = CreateSprite(animFile, Manager, 0.35f);
            Tail = new List<TailSegment>();

            for (int i = 0; i < 10; ++i)
            {
                var tailPiece = CreateSprite(tailFile, Manager, 0.25f, false);
                tailPiece.Name = "Snake Tail";
                Tail.Add(
                    new TailSegment()
                    {
                        Sprite = Manager.RootComponent.AddChild(tailPiece) as Body,
                        Target = Physics.LocalTransform.Translation
                    });


                tailPiece.AddChild(new Shadow(Manager));

                var inventory = tailPiece.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset)) as Inventory;
                inventory.SetFlag(Flag.ShouldSerialize, false);

                if (HasMeat)
                {
                    ResourceType type = Species + " " + ResourceType.Meat;

                    if (!ResourceLibrary.Resources.ContainsKey(type))
                    {
                        ResourceLibrary.Add(new Resource(ResourceLibrary.Resources[ResourceType.Meat])
                        {
                            Name = type,
                            ShortName = type
                        });
                    }

                    inventory.AddResource(new ResourceAmount(type, 1));
                }

                if (HasBones)
                {
                    ResourceType type = Name + " " + ResourceType.Bones;

                    if (!ResourceLibrary.Resources.ContainsKey(type))
                    {
                        ResourceLibrary.Add(new Resource(ResourceLibrary.Resources[ResourceType.Bones])
                        {
                            Name = type,
                            ShortName = type
                        });
                    }

                    inventory.AddResource(new ResourceAmount(type, 1));
                }
            }
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            CreateGraphics();
            base.CreateCosmeticChildren(Manager);
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
                tail.Sprite.GetRoot().Delete();
            }
            base.Delete();
        
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if ((Physics.Position - Tail.First().Target).LengthSquared() > 0.5f)
            {
                for (int i = Tail.Count  - 1; i > 0; i--)
                {
                    Tail[i].Target = Tail[i - 1].Target;
                }
                Tail[0].Target = Physics.Position;
            }

            int k = 0;
            foreach (var tail in Tail)
            {
                Vector3 diff = Vector3.UnitX;
                if (k == Tail.Count - 1)
                {
                    diff = AI.Position - tail.Sprite.LocalPosition;
                }
                else
                {
                    diff = Tail[k + 1].Sprite.LocalPosition - tail.Sprite.LocalPosition;
                }
                var mat = Matrix.CreateRotationY((float)Math.Atan2(diff.X, -diff.Z));
                mat.Translation = 0.9f*tail.Sprite.LocalPosition + 0.1f*tail.Target;
                tail.Sprite.LocalTransform = mat;
                tail.Sprite.UpdateTransform();
                tail.Sprite.PropogateTransforms();
                k++;
            }
            foreach (var tail in Tail)
            {
                tail.Sprite.SetVertexColorRecursive(Sprite.VertexColorTint);
            }

            base.Update(gameTime, chunks, camera);
        }
    }

    public class Dragon : Creature
    {
        [EntityFactory("Dragon")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var r = new Dragon(Position, Manager, "Dragon").Physics;
            r.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 2, 4)));
            return r;
        }

        public class TailSegment
        {
            public Body Sprite;
            public Vector3 Target;
        }

        [JsonIgnore]
        public List<TailSegment> Tail;


        private class Claw
        {
            public Body Parent;
            public SimpleBobber Sprite;
            public Vector3 Offset;
        }

        private List<Claw> Claws;

        public Dragon()
        {
            UpdateRate = 1;
        }

        public Dragon(Vector3 position, ComponentManager manager, string name) :
            base
            (
                manager,
                new CreatureStats
                {
                    Dexterity = 3,
                    Constitution = 10,
                    Strength = 10,
                    Wisdom = 10,
                    Charisma = 10,
                    Intelligence = 10,
                    Size = 10
                },
                "Carnivore",
                manager.World.PlanService,
                manager.World.Factions.Factions["Carnivore"],
                name
            )
        {
            UpdateRate = 1;
            HasMeat = true;
            HasBones = true;
            _maxPerSpecies = 4;
            Physics = new Physics
                (
                    manager,
                    name,
                    Matrix.CreateTranslation(position),
                    new Vector3(0.5f, 0.5f, 0.5f),
                    new Vector3(0, 0, 0),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                );

            Physics.AddChild(this);

            Initialize();
        }

        public void Initialize()
        {
            Physics.Orientation = Physics.OrientMode.Fixed;
            Species = "Dragon";
            CreateGraphics();

            // Add sensor
            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            // Add AI
            Physics.AddChild(new DragonAI(Manager, "Dragon AI", Sensors) { Movement = { CanFly = true, CanSwim = true, CanDig = true } });
            AI.Movement.SetCost(MoveType.Fly, 1.0f);

            Attacks = new List<Attack>()
            {
                new Attack("Fireball", 0.1f, 1.0f, 5.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_ic_demon_fire_spit_1, ContentPaths.Audio.Oscar.sfx_ic_demon_fire_spit_2), ContentPaths.Effects.explode)
                {
                    Mode = Attack.AttackMode.Ranged,
                    LaunchSpeed = 10.0f,
                    ProjectileType = "Fireball",
                    TriggerMode = Attack.AttackTrigger.Animation,
                    TriggerFrame = 2,
                    DamageAmount = 25
                }
            };

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Physics.Tags.Add("Dragon");
            Physics.Tags.Add("Animal");
            AI.Movement.SetCan(MoveType.ClimbWalls, true);
            AI.Movement.SetCan(MoveType.Dig, true);
            AI.Stats.FullName = String.Format("{0} the Dragon", TextGenerator.GenerateRandom("$goblinname"));
            AI.Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Dragon",
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
                        Name = "Dragon",
                        Index = 0
                    },

                }
            };
            AI.Stats.LevelIndex = 0;
            Resistances[DamageType.Normal] = 1.0f;
            Resistances[DamageType.Fire] = 5.0f;
            Resistances[DamageType.Slashing] = 2.0f;
            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                BoxTriggerTimes = 10,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_oc_giant_snake_hurt_1,
            });


            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_giant_snake_hurt_1 };
            NoiseMaker.Noises["Chirp"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_oc_giant_snake_neutral_1,
                ContentPaths.Audio.Oscar.sfx_oc_giant_snake_neutral_2
            };
        }

        private void CreateGraphics()
        {
            Physics.AddChild(new Shadow(Manager));

            var animFile = ContentPaths.Entities.Animals.Snake.dragon_animation;

            Claws = new List<Claw>();
            var sprite = CreateSprite(animFile, Manager, 0.35f);
            Tail = new List<TailSegment>();
            var sheet = new SpriteSheet(ContentPaths.Entities.Animals.Snake.dragon, 40, 48);
            for (int i = 0; i < 7; ++i)
            {
                var tailPiece = Manager.RootComponent.AddChild(new AnimatedSprite(Manager, "Tail", Matrix.Identity)) as AnimatedSprite;

                if (i == 0 || i == 3)
                {
                    tailPiece.AddAnimation(new Animation()
                    {
                        Name = "Tail",
                        SpriteSheet = sheet,
                        FrameHZ = 10,
                        Frames = new List<Point> { new Point(1, 0), new Point(2, 0), new Point(3, 0) },
                        Loops = true,
                        SpeedMultiplier = 1.0f
                    });
                }
                else if (i < 3)
                {
                    tailPiece.AddAnimation(new Animation()
                    {
                        Name = "Tail",
                        SpriteSheet = sheet,
                        FrameHZ = 10,
                        Frames = new List<Point> { new Point(1, 1) },
                        Loops = true,
                        SpeedMultiplier = 1.0f,
                    });
                } else
                {
                    tailPiece.AddAnimation(new Animation()
                    {
                        Name = "Tail",
                        SpriteSheet = sheet,
                        FrameHZ = 10,
                        Frames = new List<Point> { new Point(1, 2) },
                        Loops = true,
                        SpeedMultiplier = 1.0f,
                    });
                }


                if (i == 0 || i == 3)
                {
                    Claws.Add(new Claw()
                    {
                        Sprite = Manager.RootComponent.AddChild(new SimpleBobber(Manager, "Claw", Matrix.Identity, sheet, new Point(3, 1), 0.25f, 1.0f, MathFunctions.Rand())
                        {
                            OrientationType = SimpleSprite.OrientMode.Spherical,
                            UpdateRate = 1
                        }) as SimpleBobber,
                        Parent = tailPiece,
                        Offset = Vector3.Left * 0.6f
                    });
                    Claws.Add(new Claw()
                    {
                        Sprite = Manager.RootComponent.AddChild(new SimpleBobber(Manager, "Claw", Matrix.Identity, sheet, new Point(2, 1), 0.25f, 1.0f, MathFunctions.Rand())
                        {
                            OrientationType = SimpleSprite.OrientMode.Spherical,
                            UpdateRate = 1
                        }) as SimpleBobber,
                        Parent = tailPiece,
                        Offset = Vector3.Right * 0.6f
                    });
                }

                tailPiece.SetCurrentAnimation("Tail", true);
                tailPiece.Name = "Dragon Tail";
                Tail.Add(
                    new TailSegment()
                    {
                        Sprite = tailPiece as Body,
                        Target = Physics.LocalTransform.Translation
                    });


                tailPiece.AddChild(new Shadow(Manager) { GlobalScale = i > 3 ? 0.5f : 1.0f });

                var inventory = tailPiece.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset)) as Inventory;
                inventory.SetFlag(Flag.ShouldSerialize, false);

                if (HasMeat)
                {
                    ResourceType type = Species + " " + ResourceType.Meat;

                    if (!ResourceLibrary.Resources.ContainsKey(type))
                    {
                        ResourceLibrary.Add(new Resource(ResourceLibrary.Resources[ResourceType.Meat])
                        {
                            Name = type,
                            ShortName = type
                        });
                    }

                    inventory.AddResource(new ResourceAmount(type, 1));
                }

                if (HasBones)
                {
                    ResourceType type = Name + " " + ResourceType.Bones;

                    if (!ResourceLibrary.Resources.ContainsKey(type))
                    {
                        ResourceLibrary.Add(new Resource(ResourceLibrary.Resources[ResourceType.Bones])
                        {
                            Name = type,
                            ShortName = type
                        });
                    }

                    inventory.AddResource(new ResourceAmount(type, 1));
                }

                foreach (var claw in Claws)
                {
                    claw.Sprite.SetFlag(Flag.ShouldSerialize, false);
                }
            }
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            CreateGraphics();
            base.CreateCosmeticChildren(Manager);
        }

        public override void Die()
        {
            foreach (var tail in Tail)
            {
                tail.Sprite.Die();
            }

            foreach (var claw in Claws)
            {
                claw.Sprite.Die();
            }
            base.Die();
        }

        public override void Delete()
        {
            foreach (var tail in Tail)
            {
                tail.Sprite.GetRoot().Delete();
            }

            foreach (var claw in Claws)
            {
                claw.Sprite.GetRoot().Delete();
            }
            base.Delete();

        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if ((Physics.Position - Tail.First().Target).LengthSquared() > 0.5f)
            {
                for (int i = Tail.Count - 1; i > 0; i--)
                {
                    Tail[i].Target = Tail[i - 1].Target;
                }
                Tail[0].Target = Physics.Position;
            }

            int k = 0;
            foreach (var tail in Tail)
            {
                Vector3 diff = Vector3.UnitX;
                if (k == Tail.Count - 1)
                {
                    diff = AI.Position - tail.Sprite.LocalPosition;
                }
                else
                {
                    diff = Tail[k + 1].Sprite.LocalPosition - tail.Sprite.LocalPosition;
                }
                var mat = Matrix.CreateRotationY((float)Math.Atan2(diff.X, -diff.Z));
                mat.Translation = 0.9f * tail.Sprite.LocalPosition + 0.1f * tail.Target;
                tail.Sprite.LocalTransform = mat;
                tail.Sprite.UpdateTransform();
                tail.Sprite.PropogateTransforms();
                k++;
            }

            foreach (var claw in Claws)
            {
                var z = (claw.Parent.Position - Physics.Position);
                z.Normalize();
                var x = Vector3.Cross(Vector3.UnitY, z);
                var offset = claw.Offset.X * x;
                claw.Sprite.LocalTransform = Matrix.CreateTranslation(claw.Parent.Position + offset + Vector3.Down * 0.1f);
            }

            foreach (var tail in Tail)
            {
                tail.Sprite.SetVertexColorRecursive(Sprite.VertexColorTint);
            }

            foreach (var claw in Claws)
            {
                if (currentCharacterMode == CharacterMode.Walking || currentCharacterMode == CharacterMode.Swimming)
                {
                    claw.Sprite.Rate = 10.0f;
                }
                else
                {
                    claw.Sprite.Rate = 1.0f;
                }
                claw.Sprite.SetVertexColorRecursive(Sprite.VertexColorTint);
            }

            base.Update(gameTime, chunks, camera);
        }
    }

    public class DragonAI : PacingCreatureAI
    {
        public DragonAI()
        {

        }

        public DragonAI(ComponentManager manager, string name, EnemySensor sensors) :
            base(manager, name, sensors)
        {

        }

        public override Task ActOnIdle()
        {
            int randEvent = MathFunctions.RandInt(0, 4);

            //switch (randEvent)
            {
                bool fallthrough = false;
                // Destroy crops.
                if (randEvent == 1)
                {
                        var farms = World.ComponentManager.EnumerateComponents().OfType<Plant>().Where(p => p.Farm != null);
                        var closest = farms.OrderBy(p => (p.Position - Position).LengthSquared()).FirstOrDefault();
                        if (closest != null)
                        {
                            return new KillEntityTask(closest.GetRoot() as Body, KillEntityTask.KillType.Auto) { Name = "Destroy Crops", Priority = Task.PriorityType.High };
                        }
                    fallthrough = true;
                }
                // Destroy objects.
                if (randEvent == 2 || fallthrough)
                {
                        var closestObject = World.PlayerFaction.OwnedObjects.OrderBy(b => (b.Position - Position).LengthSquared()).FirstOrDefault();
                        if (closestObject != null)
                        {
                            return new KillEntityTask(closestObject.GetRoot() as Body, KillEntityTask.KillType.Auto) { Priority = Task.PriorityType.High };
                        }
                    fallthrough = true;
                }

                // Kill agents.
                if (randEvent == 3 || fallthrough)
                {
                    var closestEnemy = World.PlayerFaction.Minions.OrderBy(b => (b.Position - Position).LengthSquared()).FirstOrDefault();
                    if (closestEnemy != null)
                    {
                        return new KillEntityTask(closestEnemy.GetRoot() as Body, KillEntityTask.KillType.Auto);
                    }
                    fallthrough = true;
                }

                // Steal money.
                if (randEvent == 0 || fallthrough)
                {
                    var treasuries = World.PlayerFaction.Treasurys;
                    var biggest = treasuries.OrderByDescending(t => (float)(decimal)t.Money).FirstOrDefault();
                    if (biggest != null)
                    {
                        return new ActWrapperTask(new GetMoneyAct(this, 2048m, World.PlayerFaction)) { Name = "Steal money", Priority = Task.PriorityType.High };
                    }
                }

                return new ActWrapperTask(ActOnWander()) { Priority = Task.PriorityType.Low, Name = "Wander" };
            }

        }
    }
}
