using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Bird : Creature
    {
        public string SpriteAsset { get; set; }

        public Bird()
        {
            
        }

        public Bird(string sprites, Vector3 position, ComponentManager manager, string name) :
            base
            (
                manager,
                new CreatureStats
                {
                    Dexterity = 6,
                    Constitution = 1,
                    Strength = 1,
                    Wisdom = 1,
                    Charisma = 1,
                    Intelligence = 1,
                    Size = 0.25f,
                    CanSleep = false,
                    LaysEggs = true
                },
                "Herbivore",
                manager.World.PlanService,
                manager.World.Factions.Factions["Herbivore"],
                name
            )
        {
            Physics = new Physics
                (
                manager,
                    // It is called "bird"
                    "A Bird",
                    // It's attached to the root component of the component manager
                    // It is located at a position passed in as an argument
                    Matrix.CreateTranslation(position),
                    // It has a size of 0.25 blocks
                    new Vector3(0.25f, 0.25f, 0.25f),
                    // Its bounding box is located in its center
                    new Vector3(0.0f, 0.0f, 0.0f),
                    //It has a mass of 1, a moment of intertia of 1, and very small friction/restitution
                    1.0f, 1.0f, 0.999f, 0.999f,
                    // It has a gravity of 10 blocks per second downward
                    new Vector3(0, -10, 0)
                );

            Physics.AddChild(this);

            Physics.AddChild(new SelectionCircle(Manager)
            {
                IsVisible = false
            });

            SpriteAsset = sprites;
            CreateSpriteSheet(Manager);

            // Used to grab other components
            Hands = Physics.AddChild(new Grabber("hands", Manager, Matrix.Identity, new Vector3(0.2f, 0.2f, 0.2f), Vector3.Zero)) as Grabber;

            // Used to sense hostile creatures
            Sensors = Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero)) as EnemySensor;

            // Controls the behavior of the creature
            AI = Physics.AddChild(new BirdAI(Manager, "Bird AI", Sensors, PlanService)) as BirdAI;
            
            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Peck", 0.1f, 2.0f, 1.0f, ContentPaths.Audio.bird, ContentPaths.Effects.pierce) { Mode = Attack.AttackMode.Dogfight } };


            // The bird can hold one item at a time in its inventory
            Inventory = Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.BoundingBoxPos)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 1
                }
            }) as Inventory;

            Physics.AddChild(Shadow.Create(0.25f, Manager));

            // The bird will emit a shower of blood when it dies
            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1
            });

            // The bird is flammable, and can die when exposed to fire.
            Physics.AddChild(new Flammable(Manager, "Flames"));

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Bird");
            Physics.Tags.Add("Animal");
            Physics.Tags.Add("DomesticAnimal");
            NoiseMaker.Noises.Add("chirp", new List<string>(){ContentPaths.Audio.bird});

            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the bird";
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Bird",
                Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Bird" } }
            };

            AI.Movement.CanFly = true;
            AI.Movement.CanWalk = false;
            AI.Movement.CanClimb = false;
            Species = "Bird";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSpriteSheet(manager);

            Physics.AddChild(Shadow.Create(0.25f, manager));

            base.CreateCosmeticChildren(manager);
        }

        void CreateSpriteSheet(ComponentManager manager)
        {
            var spriteSheet = new SpriteSheet(SpriteAsset);

            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;

            // The dimensions of each frame in the sprite sheet (in pixels), as given by the readme
            const int frameWidth = 24;
            const int frameHeight = 16;

            // Create the sprite component for the bird.
            var sprite = Physics.AddChild(new CharacterSprite
                                  (manager.World.GraphicsDevice,
                                  manager,
                                  "Bird Sprite",
                                  Matrix.CreateTranslation(0, 0.25f, 0)
                                  )) as CharacterSprite;

            // Flying animation (rows 4 5 6 and 7)
            sprite.AddAnimation(CharacterMode.Flying,
                                OrientedAnimation.Orientation.Forward,
                                spriteSheet,
                                // animation will play at 15 FPS
                                15.0f,
                                frameWidth, frameHeight,
                                // animation begins at row 4
                                4,
                                // It consists of columns 0, 1 and 2 looped forever
                                0, 1, 2);
            sprite.AddAnimation(CharacterMode.Flying,
                                OrientedAnimation.Orientation.Left,
                                spriteSheet,
                                15.0f,
                                frameWidth, frameHeight,
                                5,
                                0, 1, 2);
            sprite.AddAnimation(CharacterMode.Flying,
                                OrientedAnimation.Orientation.Right,
                                spriteSheet,
                                15.0f,
                                frameWidth, frameHeight,
                                6,
                                0, 1, 2);
            sprite.AddAnimation(CharacterMode.Flying,
                                OrientedAnimation.Orientation.Backward,
                                spriteSheet,
                                15.0f,
                                frameWidth, frameHeight,
                                7,
                                0, 1, 2);

            // Hopping animation (rows 0 1 2 and 3)
            sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Forward, spriteSheet, 5.0f, frameWidth, frameHeight, 0, 0, 1);
            sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Left, spriteSheet, 5.0f, frameWidth, frameHeight, 1, 0, 1);
            sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Right, spriteSheet, 5.0f, frameWidth, frameHeight, 2, 0, 1);
            sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Backward, spriteSheet, 5.0f, frameWidth, frameHeight, 3, 0, 1);

            // Idle animation (rows 0 1 2 and 3)
            sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Forward, spriteSheet, 5.0f, frameWidth, frameHeight, 0, 0);
            sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Left, spriteSheet, 5.0f, frameWidth, frameHeight, 1, 0);
            sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Right, spriteSheet, 5.0f, frameWidth, frameHeight, 2, 0);
            sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Backward, spriteSheet, 5.0f, frameWidth, frameHeight, 3, 0);

            Sprite.SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
