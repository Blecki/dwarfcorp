using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Frog : Creature
    {
        [EntityFactory("Frog")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Frog(ContentPaths.Entities.Animals.Frog.frog0_animation, Position, Manager, "Frog");
        }

        [EntityFactory("Tree Frog")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Frog(ContentPaths.Entities.Animals.Frog.frog1_animation, Position, Manager, "Frog");
        }
        
        public string SpriteAsset { get; set; }

        public Frog()
        {

        }

        public Frog(string sprites, Vector3 position, ComponentManager manager, string name) :
            // Creature base constructor
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
            SpriteAsset = sprites;
            Physics = new Physics
                (
                    manager,
                    // It is called "bird"
                    "A Frog",
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

            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;

            SpriteAsset = sprites;

            CreateCosmeticChildren(Manager);

            // Used to sense hostile creatures
            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            // Controls the behavior of the creature
            Physics.AddChild(new PacingCreatureAI(Manager, "Rabbit AI", Sensors));

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Bite", 0.01f, 2.0f, 1.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_frog_attack), ContentPaths.Effects.bite) };


            // The bird can hold one item at a time in its inventory
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            // The bird will emit a shower of blood when it dies
            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_oc_frog_hurt_1
            });

            // The bird is flammable, and can die when exposed to fire.
            Physics.AddChild(new Flammable(Manager, "Flames"));

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Frog");
            Physics.Tags.Add("Animal");
            Physics.Tags.Add("DomesticAnimal");
            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the frog";
            Stats.CurrentClass = SharedClass;

            Species = "Frog";
            CanReproduce = true;
            BabyType = "Frog";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Stats.CurrentClass = SharedClass;
            CreateSprite(SpriteAsset, manager, 0.35f);
            Physics.AddChild(Shadow.Create(0.3f, manager));
            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Idle"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_frog_neutral_1, ContentPaths.Audio.Oscar.sfx_oc_frog_neutral_2 };
            NoiseMaker.Noises["Chrip"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_frog_neutral_1, ContentPaths.Audio.Oscar.sfx_oc_frog_neutral_2 };
            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_frog_hurt_1, ContentPaths.Audio.Oscar.sfx_oc_frog_hurt_2 };
            base.CreateCosmeticChildren(manager);
        }

        private static EmployeeClass SharedClass = new EmployeeClass()
        {
            Name = "Frog",
            Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Frog" } }
        };
    }
}
