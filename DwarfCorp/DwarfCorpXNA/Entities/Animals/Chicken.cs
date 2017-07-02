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
    public class Chicken : Creature
    {

        public Chicken()
        {

        }

        public Chicken(Vector3 position, ComponentManager manager, string name) :
            // Creature base constructor
            base
            (
                manager,
                // Default stats
                new CreatureStats
                {
                    Dexterity = 2,
                    Constitution = 1,
                    Strength = 1,
                    Wisdom = 1,
                    Charisma = 1,
                    Intelligence = 1,
                    Size = 0.25f,
                    CanSleep = false,
                    LaysEggs = true
                },
                // Belongs to herbivore team
                "Herbivore",
                // Uses the default plan service
                manager.World.PlanService,
                // Belongs to the herbivore team
                manager.World.Factions.Factions["Herbivore"],
                name
            )
        {
            Physics = new Physics
                (
                manager,
                    // It is called "bird"
                    "A Chicken",
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

            Initialize(ContentPaths.Entities.Animals.chicken_animations);
        }

        /// <summary>
        /// Initialize function creates all the required components for the bird.
        /// </summary>
        /// <param name="sprites">The sprite sheet to use for the bird</param>
        public void Initialize(string sprites)
        {
            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;


            CreateSprite(ContentPaths.Entities.Animals.chicken_animations, Manager);

            // Used to grab other components
            Hands = Physics.AddChild(new Grabber("hands", Manager, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero)) as Grabber;

            // Used to sense hostile creatures
            Sensors = Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero)) as EnemySensor;

            // Controls the behavior of the creature
            AI = Physics.AddChild(new PacingCreatureAI(Manager, "Chicken AI", Sensors, PlanService)) as CreatureAI;

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Peck", 0.01f, 2.0f, 1.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_chicken_attack), ContentPaths.Effects.pierce) {Mode = Attack.AttackMode.Dogfight} };


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
            Physics.Tags.Add("Chicken");
            Physics.Tags.Add("Animal");
            Physics.Tags.Add("DomesticAnimal");
            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the chicken";
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Chicken",
                Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Chicken" } }
            };


            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_chicken_hurt_1, ContentPaths.Audio.Oscar.sfx_oc_chicken_hurt_2 };
            NoiseMaker.Noises["Chirp"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_chicken_neutral_1, ContentPaths.Audio.Oscar.sfx_oc_chicken_neutral_2};
            NoiseMaker.Noises["Lay Egg"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_chicken_lay_egg};
            Species = "Chicken";

            var deathParticleTrigger = Parent.EnumerateAll().OfType<ParticleTrigger>().FirstOrDefault();
            if (deathParticleTrigger != null)
            {
                deathParticleTrigger.SoundToPlay = NoiseMaker.Noises["Hurt"][0];
            }

        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(ContentPaths.Entities.Animals.chicken_animations, manager);
            Physics.AddChild(Shadow.Create(0.25f, manager));
            base.CreateCosmeticChildren(manager);
        }
    }
}
