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
    public class Chimp : Creature
    {
        [EntityFactory("Chimp")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Chimp(Position, Manager, "Chimp");
        }

        public Chimp()
        {

        }

        public Chimp(Vector3 position, ComponentManager manager, string name) :
            base
            (
                manager,
                new CreatureStats
                {
                    BaseDexterity = 6,
                    BaseConstitution = 5,
                    BaseStrength = 6,
                    BaseWisdom = 2,
                    BaseCharisma = 1,
                    BaseIntelligence = 1,
                    BaseSize = 0.25f,
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
                    "Chimp",
                    Matrix.CreateTranslation(position),
                    new Vector3(0.25f, 0.25f, 0.25f),
                    new Vector3(0.0f, 0.0f, 0.0f),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                );

            Physics.AddChild(this);

            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            // Used to sense hostile creatures
            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            // Controls the behavior of the creature
            Physics.AddChild(new PacingCreatureAI(Manager, "Chimp AI", Sensors));

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Hit", 5.0f, 0.5f, 2.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_rabbit_attack), ContentPaths.Effects.hit) {
                DiseaseToSpread = "Rabies",
                TriggerMode = Attack.AttackTrigger.Animation,
                 TriggerFrame = 2 } };


            // The bird can hold one item at a time in its inventory
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            // The bird is flammable, and can die when exposed to fire.
            Physics.AddChild(new Flammable(Manager, "Flames"));

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Chimp");
            Physics.Tags.Add("Animal");
            Physics.Tags.Add("DomesticAnimal");
            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the Chimp";
            Stats.CurrentClass = SharedClass;
            
            Species = "Chimp";
            CanReproduce = true;
            BabyType = Name;

            AI.Movement.SetCan(MoveType.ClimbWalls, true);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Stats.CurrentClass = SharedClass;

            CreateSprite(ContentPaths.Entities.Animals.Chimp.chimp_animations, manager, 0.6f);
            Physics.AddChild(Shadow.Create(0.5f, manager));

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_rabbit_hurt_1, ContentPaths.Audio.Oscar.sfx_oc_rabbit_hurt_2 };
            NoiseMaker.Noises["Chirp"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_oc_rabbit_neutral_1,
                ContentPaths.Audio.Oscar.sfx_oc_rabbit_neutral_2
            };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_oc_rabbit_hurt_1
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }

        private static CreatureClass SharedClass = new CreatureClass()
        {
            Name = "Chimp",
            Levels = new List<CreatureClass.Level>() { new CreatureClass.Level() { Index = 0, Name = "Chimp" } }
        };
    }
}
