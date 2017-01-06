using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Scorpion : Creature
    {

        public Scorpion()
        {

        }

        public Scorpion(string sprites, Vector3 position, ComponentManager manager, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, string name) :
            // Creature base constructor
            base
            (
                // Default stats
                new CreatureStats
                {
                    Dexterity = 6,
                    Constitution = 1,
                    Strength = 1,
                    Wisdom = 1,
                    Charisma = 1,
                    Intelligence = 1,
                    Size = 0.25f,
                    CanSleep = false
                },
                // Belongs to herbivore team
                "Carnivore",
                // Uses the default plan service
                PlayState.PlanService,
                // Belongs to the herbivore team
                manager.Factions.Factions["Carnivore"],
                // The physics component this creature belongs to
                new Physics
                (
                // It is called "bird"
                    "Scorpion",
                // It's attached to the root component of the component manager
                    manager.RootComponent,
                // It is located at a position passed in as an argument
                    Matrix.CreateTranslation(position),
                // It has a size of 0.25 blocks
                    new Vector3(0.375f, 0.375f, 0.375f),
                // Its bounding box is located in its center
                    new Vector3(0.0f, 0.0f, 0.0f),
                //It has a mass of 1, a moment of intertia of 1, and very small friction/restitution
                    1.0f, 1.0f, 0.999f, 0.999f,
                // It has a gravity of 10 blocks per second downward
                    new Vector3(0, -10, 0)
                ),
                // All the rest of the arguments are passed in directly
                chunks, graphics, content, name
            )
        {
            // Called from constructor with appropriate sprite asset as a string
            Initialize(sprites);
        }

        /// <summary>
        /// Initialize function creates all the required components for the bird.
        /// </summary>
        /// <param name="sprites">The sprite sheet to use for the bird</param>
        public void Initialize(string sprites)
        {
            HasBones = false;
            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;


            // Create the sprite component for the bird.
            Sprite = new CharacterSprite
                                  (Graphics,
                                  Manager,
                                  "Scorpion Sprite",
                                  Physics,
                                  Matrix.CreateTranslation(0, 0.5f, 0)
                                  );

            CompositeAnimation.Descriptor descriptor =
                FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(
                    ContentPaths.GetFileAsString(sprites));

            List<CompositeAnimation> animations = descriptor.GenerateAnimations("Scorpion");

            foreach (CompositeAnimation animation in animations)
            {
                Sprite.AddAnimation(animation);
            }

            // Used to grab other components
            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.2f, 0.2f, 0.2f), Vector3.Zero);

            // Used to sense hostile creatures
            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            // Controls the behavior of the creature
            AI = new PacingCreatureAI(this, "Scorpion AI", Sensors, PlanService);

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Sting", 0.01f, 2.0f, 1.0f, ContentPaths.Audio.hiss, ContentPaths.Effects.pierce) };


            // The bird can hold one item at a time in its inventory
            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 1
                }
            };

            // The shadow is rotated 90 degrees along X, and is 0.25 blocks beneath the creature
            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.25f, 0.0f);
            shadowTransform *= Matrix.CreateScale(0.75f);

            SpriteSheet shadowTexture = new SpriteSheet(ContentPaths.Effects.shadowcircle);
            Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform, shadowTexture);

            // We set up the shadow's animation so that it's just a static black circle
            // TODO: Make the shadow set this up automatically
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle), "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");

            // The bird will emit a shower of blood when it dies
            DeathParticleTrigger = new ParticleTrigger("blood_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1
            };

            // The bird is flammable, and can die when exposed to fire.
            Flames = new Flammable(Manager, "Flames", Physics, this);

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Scorpion");
            Physics.Tags.Add("Animal");

            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the Scorpion";
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Scorpion",
                Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Scorpion" } }
            };

            NoiseMaker.Noises.Add("Hurt", new List<string>() { ContentPaths.Audio.hiss });
            AI.Movement.CanClimbWalls = true;
            AI.Movement.CanSwim = false;
        }
    }
}
