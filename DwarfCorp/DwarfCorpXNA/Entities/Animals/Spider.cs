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
    public class Spider : Creature
    {
        [EntityFactory("Spider")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            // Todo: Why are we passing in the assets??
            return new Spider(Manager, ContentPaths.Entities.Animals.Spider.spider_animation, Position);
        }

        public string SpriteAsset { get; set; }
        public Spider()
        {

        }

        public Spider(ComponentManager manager, string sprites, Vector3 position) :
            base
            (
                manager,
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
                "Carnivore",
                manager.World.PlanService,
                manager.World.Factions.Factions["Carnivore"],
                "Spider"
            )
        {
            Physics = new Physics
                (
                    manager, 
                    // It is called "bird"
                    "Spider",
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
            SpriteAsset = sprites;
            HasBones = false;
            // When true, causes the bird to face the direction its moving in
            Physics.Orientation = Physics.OrientMode.RotateY;
                
            CreateSprite(sprites, Manager);

            // Used to sense hostile creatures
            Sensors = Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero)) as EnemySensor;

            // Controls the behavior of the creature
            AI = Physics.AddChild(new PacingCreatureAI(Manager, "Spider AI", Sensors, PlanService)) as CreatureAI;

            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Sting", 0.01f, 1.0f, 3.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_giant_spider_attack_1, ContentPaths.Audio.Oscar.sfx_oc_giant_spider_attack_2), ContentPaths.Effects.bite),
                new Attack("Web", 0.0f, 1.0f, 5.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_giant_spider_attack_1, ContentPaths.Audio.Oscar.sfx_oc_giant_spider_attack_2), ContentPaths.Effects.claw) {Mode = Attack.AttackMode.Ranged, LaunchSpeed = 10, ProjectileType = "Web"} };


            // The bird can hold one item at a time in its inventory
            Inventory = Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset)) as Inventory;

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
            Physics.Tags.Add("Spider");
            Physics.Tags.Add("Animal");

            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the Spider";
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Spider",
                Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Spider" } }
            };

            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_giant_spider_hurt_1 };
            NoiseMaker.Noises["Chirp"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_oc_giant_spider_neutral_1,
                ContentPaths.Audio.Oscar.sfx_oc_giant_spider_neutral_2
            };
            AI.Movement.CanClimbWalls = true;
            AI.Movement.CanSwim = false;
            Species = "Spider";
            CanReproduce = true;
            BabyType = "Spider";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(SpriteAsset, manager);
            Physics.AddChild(Shadow.Create(0.25f, manager));
            base.CreateCosmeticChildren(manager);
        }

    }

    public class PacingCreatureAI : CreatureAI
    {
        public PacingCreatureAI()
        {

        }

        public PacingCreatureAI(ComponentManager Manager, string name, EnemySensor sensors, PlanService planService) :
            base(Manager, name, sensors, planService)
        {

        }

        public override Act ActOnWander()
        {
            return new WanderAct(this, 6, 0.5f + MathFunctions.Rand(-0.25f, 0.25f), 1.0f) & new LongWanderAct(this) { PathLength = 10, Radius = 50 };
        }
    }
   
}
