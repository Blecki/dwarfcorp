using System;
using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     Bat creature.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Bat : Creature
    {
        public Bat()
        {
        }

        public Bat(Vector3 position) :
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
            // TODO: should bats be undead, carnivores, or something else?
            "Undead",
            PlayState.PlanService,
            PlayState.ComponentManager.Factions.Factions["Undead"],
            // Physical parameters.
            new Physics
                (
                "bat",
                PlayState.ComponentManager.RootComponent,
                Matrix.CreateTranslation(position),
                new Vector3(0.375f, 0.375f, 0.375f),
                new Vector3(0.0f, 0.0f, 0.0f),
                1.0f, 1.0f, 0.999f, 0.999f,
                new Vector3(0, -10, 0)
                ),
            PlayState.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Bat"
            )
        {
            Initialize();
        }

        public void Initialize()
        {
            Physics.Orientation = Physics.OrientMode.RotateY;
            Sprite = new CharacterSprite
                (Graphics,
                    Manager,
                    "Bat Sprite",
                    Physics,
                    Matrix.CreateTranslation(0, 0.5f, 0)
                );

            var descriptor =
                FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(
                    ContentPaths.GetFileAsString(ContentPaths.Entities.Animals.Bat.bat_animations));

            List<CompositeAnimation> animations = descriptor.GenerateAnimations("Bat");

            foreach (CompositeAnimation animation in animations)
            {
                Sprite.AddAnimation(animation);
            }

            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);
            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20),
                Vector3.Zero);

            AI = new BatAI(this, "Bat AI", Sensors, PlanService)
            {
                Movement = {CanFly = true, CanSwim = false, CanClimb = false, CanWalk = false}
            };

            Attacks = new List<Attack>
            {
                new Attack("Bite", 0.01f, 2.0f, 1.0f, ContentPaths.Audio.bunny, ContentPaths.Effects.bite)
                {
                    TriggerMode = Attack.AttackTrigger.Animation,
                    TriggerFrame = 1,
                    Mode = Attack.AttackMode.Dogfight
                }
            };

            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 1
                }
            };

            Matrix shadowTransform = Matrix.CreateRotationX((float) Math.PI*0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.25f, 0.0f);
            shadowTransform *= Matrix.CreateScale(0.75f);

            var shadowTexture = new SpriteSheet(ContentPaths.Effects.shadowcircle);
            Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform, shadowTexture);
            var shP = new List<Point>
            {
                new Point(0, 0)
            };
            var shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle), "sh",
                32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");

            DeathParticleTrigger = new ParticleTrigger("blood_particle", Manager, "Death Gibs", Physics, Matrix.Identity,
                Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1
            };

            Flames = new Flammable(Manager, "Flames", Physics, this);
            Physics.Tags.Add("Bat");
            Physics.Tags.Add("Animal");

            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the bat";
            Stats.CurrentClass = new EmployeeClass
            {
                Name = "Bat",
                Levels = new List<EmployeeClass.Level> {new EmployeeClass.Level {Index = 0, Name = "Bat"}}
            };


            NoiseMaker.Noises.Add("Hurt", new List<string> {ContentPaths.Audio.bunny});
        }
    }


    /// <summary>
    ///     Extends creature AI for Bat-related behavior.
    /// </summary>
    public class BatAI : CreatureAI
    {
        public BatAI()
        {
        }

        public BatAI(Creature creature, string name, EnemySensor sensor, PlanService planService) :
            base(creature, name, sensor, planService)
        {
        }

        private IEnumerable<Act.Status> ChirpRandomly()
        {
            var chirpTimer = new Timer(MathFunctions.Rand(6f, 10f), false);
            while (true)
            {
                chirpTimer.Update(DwarfTime.LastTime);
                if (chirpTimer.HasTriggered)
                {
                    Creature.NoiseMaker.MakeNoise(ContentPaths.Audio.bunny, Creature.AI.Position, true, 0.01f);
                }
                yield return Act.Status.Running;
            }

            yield return Act.Status.Success;
        }

        public override Task ActOnIdle()
        {
            // This makes the Bat fly around randomly and chirp.
            return new ActWrapperTask(
                new Parallel(
                    new FlyWanderAct(this, 10.0f + MathFunctions.Rand()*2.0f, 2.0f + MathFunctions.Rand()*0.5f, 20.0f,
                        4.0f + MathFunctions.Rand()*2, 10.0f) {CanPerchOnGround = false, CanPerchOnWalls = true}
                    , new Wrap(ChirpRandomly)) {ReturnOnAllSucces = false, Name = "Fly"});
        }
    }
}