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
    public class Scorpion : Creature
    {
        [EntityFactory("Scorpion")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            // Todo: Why are we passing in the graphic here?
            return new Scorpion(ContentPaths.Entities.Animals.Scorpion.scorption_animation, Position, Manager, "Scorpion");
        }

        public string SpriteAsset { get; set; }
        public Scorpion()
        {

        }

        public Scorpion(string sprites, Vector3 position, ComponentManager manager, string name) :
            base
            (
                manager,
                new CreatureStats
                {
                    Dexterity = 2,
                    Constitution = 2,
                    Strength = 2,
                    Wisdom = 1,
                    Charisma = 1,
                    Intelligence = 1,
                    Size = 0.25f,
                    CanSleep = false
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
                    // It is called "bird"
                    "Scorpion",
                    // It's attached to the root component of the component manager
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

            Physics.AddChild(new SelectionCircle(Manager)
            {
                IsVisible = false
            });

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

            SpriteAsset = sprites;
            CreateSprite(sprites, Manager);

            Physics.AddChild(Shadow.Create(0.25f, Manager));

            // Used to sense hostile creatures
            Sensors = Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero)) as EnemySensor;

            // Controls the behavior of the creature
            AI = Physics.AddChild(new PacingCreatureAI(Manager, "Scorpion AI", Sensors, PlanService)) as CreatureAI;

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Sting", 4.0f, 2.0f, 1.5f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_scorpion_attack_1, ContentPaths.Audio.Oscar.sfx_oc_scorpion_attack_2), ContentPaths.Effects.pierce) { TriggerFrame = 2, TriggerMode = Attack.AttackTrigger.Animation} };


            // The bird can hold one item at a time in its inventory
            Inventory = Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset)) as Inventory;

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
            Physics.Tags.Add("Scorpion");
            Physics.Tags.Add("Animal");

            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the Scorpion";
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Scorpion",
                Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Scorpion" } }
            };

            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_scorpion_hurt_1 };
            NoiseMaker.Noises["Chirp"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_scorpion_neutral_1, ContentPaths.Audio.Oscar.sfx_oc_scorpion_neutral_2 };
            AI.Movement.CanClimbWalls = true;
            AI.Movement.CanSwim = false;
            AI.Movement.SetSpeed(MoveType.Jump, 1.5f);
            AI.Movement.SetSpeed(MoveType.Climb, 1.5f);
            AI.Movement.SetCost(MoveType.Climb, 0.1f);
            Species = "Scorpion";
            CanReproduce = true;
            BabyType = "Scorpion";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(SpriteAsset, manager);
            Physics.AddChild(Shadow.Create(0.25f, manager));
            base.CreateCosmeticChildren(manager);
        }
    }
}
