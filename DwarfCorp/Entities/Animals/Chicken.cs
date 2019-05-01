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
    public class Chicken : Creature
    {
        [EntityFactory("Chicken")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Chicken(Position, Manager, "Chicken", "Chicken");
        }

        [EntityFactory("Turkey")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Chicken(Position, Manager, "Turkey", "Turkey");
        }

        [EntityFactory("Penguin")]
        private static GameComponent __factory2(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Chicken(Position, Manager, "Penguin", "Penguin");
        }

        public Chicken()
        {

        }

        public Chicken(Vector3 position, ComponentManager manager, string name, string species) :
            // Creature base constructor
            base
            (
                manager,
                // Default stats
                new CreatureStats
                {
                    BaseDexterity = 2,
                    BaseConstitution = 1,
                    BaseStrength = 1,
                    BaseWisdom = 1,
                    BaseCharisma = 1,
                    BaseIntelligence = 1,
                    BaseSize = 0.25f,
                    CanSleep = false,
                    LaysEggs = true,
                    IsMigratory = true
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
                    species,
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
            BaseMeatResource = "Bird Meat";
            Species = species;
            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            // Used to sense hostile creatures
            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            // Controls the behavior of the creature
            Physics.AddChild(new PacingCreatureAI(Manager, "AI", Sensors));

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack>
            {
                new Attack("Peck", 0.01f, 2.0f, 0.5f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_chicken_attack), ContentPaths.Effects.pierce)
                {
                    Mode = Attack.AttackMode.Melee,
                    TriggerFrame = 2,
                    TriggerMode = Attack.AttackTrigger.Animation
                }
            };

            // The bird can hold one item at a time in its inventory
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            // The bird is flammable, and can die when exposed to fire.
            Physics.AddChild(new Flammable(Manager, "Flames"));

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add(species);
            Physics.Tags.Add("Animal");
            Physics.Tags.Add("DomesticAnimal");
            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the " + species;
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Stats.CurrentClass = SharedClasses[Species];

            CreateSprite(ContentPaths.Entities.Animals.fowl[Species], manager);
            Physics.AddChild(Shadow.Create(0.5f, manager));

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_chicken_hurt_1, ContentPaths.Audio.Oscar.sfx_oc_chicken_hurt_2 };
            NoiseMaker.Noises["Chirp"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_chicken_neutral_1, ContentPaths.Audio.Oscar.sfx_oc_chicken_neutral_2 };
            NoiseMaker.Noises["Lay Egg"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_chicken_lay_egg };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_oc_chicken_hurt_1
            }).SetFlag(Flag.ShouldSerialize, false);

            Physics.AddChild(new ParticleTrigger("feather", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 10
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }

        private static Dictionary<String, CreatureClass> SharedClasses = new Dictionary<string, CreatureClass>
        {
            { "Chicken", new CreatureClass()
                {
                    Name = "Chicken",
                    Levels = new List<CreatureClass.Level>() { new CreatureClass.Level() { Index = 0, Name = "Chicken" } }
                }
            },
            { "Turkey", new CreatureClass()
                {
                    Name = "Turkey",
                    Levels = new List<CreatureClass.Level>() { new CreatureClass.Level() { Index = 0, Name = "Turkey" } }
                }
            },
            { "Penguin", new CreatureClass()
                {
                    Name = "Penguin",
                    Levels = new List<CreatureClass.Level>() { new CreatureClass.Level() { Index = 0, Name = "Penguin" } }
                }
            },
        };
    }
}
