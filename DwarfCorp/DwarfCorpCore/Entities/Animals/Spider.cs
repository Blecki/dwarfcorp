using System;
using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Spider : Creature
    {
        public Spider()
        {
        }

        public Spider(string sprites, Vector3 position) :
            // Creature base constructor
            base
            (
            // Default stats
            new CreatureStats
            {
                Dexterity = 6,
                Constitution = 3,
                Strength = 3,
                Wisdom = 3,
                Charisma = 3,
                Intelligence = 3,
                Size = 0.25f,
                CanSleep = false
            },
            // Belongs to herbivore team
            "Carnivore",
            // Uses the default plan service
            PlayState.PlanService,
            // Belongs to the herbivore team
            PlayState.ComponentManager.Factions.Factions["Carnivore"],
            // The physics component this creature belongs to
            new Physics
                (
                // It is called "bird"
                "Spider",
                // It's attached to the root component of the component manager
                PlayState.ComponentManager.RootComponent,
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
            PlayState.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Spider"
            )
        {
            // Called from constructor with appropriate sprite asset as a string
            Initialize(sprites);
        }

        /// <summary>
        ///     Initialize function creates all the required components for the bird.
        /// </summary>
        /// <param name="spriteSheet">The sprite sheet to use for the bird</param>
        public void Initialize(string sprites)
        {
            HasBones = false;
            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;


            // Create the sprite component for the bird.
            Sprite = new CharacterSprite
                (Graphics,
                    Manager,
                    "Spider Sprite",
                    Physics,
                    Matrix.CreateTranslation(0, 0.5f, 0)
                );

            var descriptor =
                FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(
                    ContentPaths.GetFileAsString(sprites));

            List<CompositeAnimation> animations = descriptor.GenerateAnimations("Spider");

            foreach (CompositeAnimation animation in animations)
            {
                Sprite.AddAnimation(animation);
            }

            // Used to grab other components
            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.2f, 0.2f, 0.2f), Vector3.Zero);

            // Used to sense hostile creatures
            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20),
                Vector3.Zero);

            // Controls the behavior of the creature
            AI = new PacingCreatureAI(this, "Spider AI", Sensors, PlanService);

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack>
            {
                new Attack("Sting", 0.01f, 1.0f, 3.0f, ContentPaths.Audio.hiss, ContentPaths.Effects.bite),
                new Attack("Web", 0.0f, 1.0f, 5.0f, ContentPaths.Audio.hiss, ContentPaths.Effects.claws)
                {
                    Mode = Attack.AttackMode.Ranged,
                    LaunchSpeed = 10,
                    ProjectileType = "Web"
                }
            };


            // The bird can hold one item at a time in its inventory
            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 1
                }
            };

            // The shadow is rotated 90 degrees along X, and is 0.25 blocks beneath the creature
            Matrix shadowTransform = Matrix.CreateRotationX((float) Math.PI*0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.25f, 0.0f);
            shadowTransform *= Matrix.CreateScale(0.75f);

            var shadowTexture = new SpriteSheet(ContentPaths.Effects.shadowcircle);
            Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform, shadowTexture);

            // We set up the shadow's animation so that it's just a static black circle
            // TODO: Make the shadow set this up automatically
            var shP = new List<Point>
            {
                new Point(0, 0)
            };
            var shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle), "sh", 32,
                32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");

            // The bird will emit a shower of blood when it dies
            DeathParticleTrigger = new ParticleTrigger("blood_particle", Manager, "Death Gibs", Physics, Matrix.Identity,
                Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1
            };

            // The bird is flammable, and can die when exposed to fire.
            Flames = new Flammable(Manager, "Flames", Physics, this);

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Spider");
            Physics.Tags.Add("Animal");

            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the Spider";
            Stats.CurrentClass = new EmployeeClass
            {
                Name = "Spider",
                Levels = new List<EmployeeClass.Level> {new EmployeeClass.Level {Index = 0, Name = "Spider"}}
            };

            NoiseMaker.Noises.Add("Hurt", new List<string> {ContentPaths.Audio.hiss});
            AI.Movement.CanClimbWalls = true;
            AI.Movement.CanSwim = false;
        }
    }

    public class PacingCreatureAI : CreatureAI
    {
        public PacingCreatureAI()
        {
        }

        public PacingCreatureAI(Creature spider, string name, EnemySensor sensors, PlanService planService) :
            base(spider, name, sensors, planService)
        {
        }

        public override Act ActOnWander()
        {
            return new WanderAct(this, 6, 0.5f + MathFunctions.Rand(-0.25f, 0.25f), 1.0f) &
                   new LongWanderAct(this) {PathLength = 10, Radius = 50};
        }
    }
}