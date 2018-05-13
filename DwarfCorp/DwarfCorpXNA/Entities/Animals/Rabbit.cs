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
    public class Rabbit : Creature
    {
        [EntityFactory("Brown Rabbit")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Rabbit(ContentPaths.Entities.Animals.Rabbit.rabbit0_animation, Position, Manager, "Brown Rabbit");
        }

        [EntityFactory("White Rabbit")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Rabbit(ContentPaths.Entities.Animals.Rabbit.rabbit1_animation, Position, Manager, "White Rabbit");
        }

        public string SpriteAsset { get; set; }

        public Rabbit()
        {

        }

        public Rabbit(string sprites, Vector3 position, ComponentManager manager, string name) :
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
                    IsMigratory = true
                },
                "Herbivore",
                manager.World.PlanService,
                manager.World.Factions.Factions["Herbivore"],
                name
            )
        {
            Physics = new Physics
                (
                Manager,
                    "A Rabbit",
                    Matrix.CreateTranslation(position),
                    new Vector3(0.25f, 0.25f, 0.25f),
                    new Vector3(0.0f, 0.0f, 0.0f),
                    1.0f, 1.0f, 0.999f, 0.999f,
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
            SpriteAsset = sprites;
            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateSprite(sprites, Manager);

            // Used to sense hostile creatures
            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            // Controls the behavior of the creature
            Physics.AddChild(new PacingCreatureAI(Manager, "Rabbit AI", Sensors, PlanService));

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Bite", 0.01f, 2.0f, 1.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_rabbit_attack), ContentPaths.Effects.bite) {DiseaseToSpread = "Rabies"} };


            // The bird can hold one item at a time in its inventory
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

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
            Physics.Tags.Add("Rabbit");
            Physics.Tags.Add("Animal");
            Physics.Tags.Add("DomesticAnimal");
            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the rabbit";
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Rabbit",
                Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Rabbit" } }
            };


            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_rabbit_hurt_1, ContentPaths.Audio.Oscar.sfx_oc_rabbit_hurt_2 };
            NoiseMaker.Noises["Chirp"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_oc_rabbit_neutral_1,
                ContentPaths.Audio.Oscar.sfx_oc_rabbit_neutral_2
            };
            Species = "Rabbit";
            CanReproduce = true;
            BabyType = Name;

            var deathParticleTrigger = Parent.EnumerateAll().OfType<ParticleTrigger>().FirstOrDefault();
            if (deathParticleTrigger != null)
            {
                deathParticleTrigger.SoundToPlay = NoiseMaker.Noises["Hurt"][0];
            }
        }


        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(SpriteAsset, manager);
            Physics.AddChild(Shadow.Create(0.25f, manager));
            base.CreateCosmeticChildren(manager);
        }
    }
}
