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
    public class Bat : Creature
    {

        public Bat()
        {

        }

        public Bat(ComponentManager manager, Vector3 position) :
            // Creature base constructor
            base
            (
                manager,
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
                manager.World.PlanService,
                // Belongs to the herbivore team
                manager.World.Factions.Factions["Carnivore"],
                // The physics component this creature belongs to
                
                // All the rest of the arguments are passed in directly
                manager.World.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Bat"
            )
        {
            Physics = new Physics
                (
                manager,
                    // It is called "bird"
                    "bat",
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
                );

            Physics.AddChild(this);

            SelectionCircle = Physics.AddChild(new SelectionCircle(Manager)
            {
                IsVisible = false
            }) as SelectionCircle;

            // Called from constructor with appropriate sprite asset as a string
            Initialize();
        }

        /// <summary>
        /// Initialize function creates all the required components for the bat.
        /// </summary>
        public void Initialize()
        {
            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;


            // Create the sprite component for the bird.
            Sprite = Physics.AddChild(new CharacterSprite
                                  (Graphics,
                                  Manager,
                                  "Bat Sprite",
                                  Matrix.CreateTranslation(0, 0.5f, 0)
                                  )) as CharacterSprite;

            CompositeAnimation.Descriptor descriptor =
                FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(
                    ContentPaths.GetFileAsString(ContentPaths.Entities.Animals.Bat.bat_animations));

            List<CompositeAnimation> animations = descriptor.GenerateAnimations("Bat");

            foreach (CompositeAnimation animation in animations)
            {
                Sprite.AddAnimation(animation);
            }

            // Used to grab other components
            Hands = Physics.AddChild(new Grabber("hands", Manager, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero)) as Grabber;

            // Used to sense hostile creatures
            Sensors = Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero)) as EnemySensor;

            // Controls the behavior of the creature
            AI = Physics.AddChild(new BatAI(Manager, "Bat AI", Sensors, PlanService)) as BatAI;
            AI.Movement.CanFly = true;
            AI.Movement.CanSwim = false;
            AI.Movement.CanClimb = false;
            AI.Movement.CanWalk = false;

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Bite", 0.01f, 2.0f, 1.0f, ContentPaths.Audio.bunny, ContentPaths.Effects.bite) {TriggerMode = Attack.AttackTrigger.Animation, TriggerFrame = 1, Mode = Attack.AttackMode.Dogfight}};


            // The bird can hold one item at a time in its inventory
            Inventory = Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.BoundingBoxPos)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 1
                }
            }) as Inventory;

            // The shadow is rotated 90 degrees along X, and is 0.25 blocks beneath the creature
            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.25f, 0.0f);
            shadowTransform *= Matrix.CreateScale(0.75f);

            SpriteSheet shadowTexture = new SpriteSheet(ContentPaths.Effects.shadowcircle);
            var shadow = Physics.AddChild(new Shadow(Manager, "Shadow", shadowTransform, shadowTexture)) as Shadow;

            // We set up the shadow's animation so that it's just a static black circle
            // TODO: Make the shadow set this up automatically
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle), "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            shadow.SetCurrentAnimation("sh");

            // The bird will emit a shower of blood when it dies
            DeathParticleTrigger = Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1
            }) as ParticleTrigger;

            // The bird is flammable, and can die when exposed to fire.
            Physics.AddChild(new Flammable(Manager, "Flames"));

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Bat");
            Physics.Tags.Add("Animal");

            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the bat";
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Bat",
                Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Bat" } }
            };


            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.bunny };
            Species = "Bat";
            CanReproduce = true;
            BabyType = "Bat";
        }
    }


    /// <summary>
    /// Extends CreatureAI specifically for
    /// bat behavior.
    /// </summary>
    public class BatAI : CreatureAI
    {
        public BatAI()
        {

        }

        public BatAI(ComponentManager Manager, string name, EnemySensor sensor, PlanService planService) :
            base(Manager, name, sensor, planService)
        {

        }

        IEnumerable<Act.Status> ChirpRandomly()
        {
            Timer chirpTimer = new Timer(MathFunctions.Rand(6f, 10f), false);
            while (true)
            {
                chirpTimer.Update(DwarfTime.LastTime);
                if (chirpTimer.HasTriggered)
                {
                    Creature.NoiseMaker.MakeNoise(ContentPaths.Audio.bunny, Creature.AI.Position, true, 0.01f);
                }
                yield return Act.Status.Running;
            }
        }


        // Overrides the default ActOnIdle so we can
        // have the bird act in any way we wish.
        public override Task ActOnIdle()
        {
            return new ActWrapperTask(
                new Parallel(new FlyWanderAct(this, 10.0f + MathFunctions.Rand() * 2.0f, 2.0f + MathFunctions.Rand() * 0.5f, 20.0f, 4.0f + MathFunctions.Rand() * 2, 10.0f) { CanPerchOnGround =  false, CanPerchOnWalls = true}
                , new Wrap(ChirpRandomly)) { ReturnOnAllSucces = false, Name = "Fly" });
        }
    }
}
