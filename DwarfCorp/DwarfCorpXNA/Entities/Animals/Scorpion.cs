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
            return new Scorpion(Position, Manager, "Scorpion");
        }

        public Scorpion()
        {

        }

        public Scorpion(Vector3 position, ComponentManager manager, string name) :
            base
            (
                manager,
                new CreatureStats
                {
                    BaseDexterity = 2,
                    BaseConstitution = 2,
                    BaseStrength = 2,
                    BaseWisdom = 1,
                    BaseCharisma = 1,
                    BaseIntelligence = 1,
                    BaseSize = 0.25f,
                    CanSleep = false,
                    CanEat = true
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

            HasBones = false;
            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            // Used to sense hostile creatures
            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            // Controls the behavior of the creature
            Physics.AddChild(new PacingCreatureAI(Manager, "Scorpion AI", Sensors));

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Sting", 4.0f, 2.0f, 1.5f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_scorpion_attack_1, ContentPaths.Audio.Oscar.sfx_oc_scorpion_attack_2), ContentPaths.Effects.pierce) { TriggerFrame = 2, TriggerMode = Attack.AttackTrigger.Animation} };


            // The bird can hold one item at a time in its inventory
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            // The bird is flammable, and can die when exposed to fire.
            Physics.AddChild(new Flammable(Manager, "Flames"));

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Scorpion");
            Physics.Tags.Add("Animal");

            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the Scorpion";
            Stats.CurrentClass = SharedClass;

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
            Stats.CurrentClass = SharedClass;

            CreateSprite(ContentPaths.Entities.Animals.Scorpion.scorption_animation, manager, 0.35f);
            Physics.AddChild(Shadow.Create(0.3f, manager));

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_scorpion_hurt_1 };
            NoiseMaker.Noises["Chirp"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_scorpion_neutral_1, ContentPaths.Audio.Oscar.sfx_oc_scorpion_neutral_2 };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }

        private static EmployeeClass SharedClass = new EmployeeClass()
        {
            Name = "Scorpion",
            Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Scorpion" } }
        };
    }
}
