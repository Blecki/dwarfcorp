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
            return new Spider(Manager, Position);
        }

        public Spider()
        {

        }

        public Spider(ComponentManager manager, Vector3 position) :
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
                    CanSleep = false,
                    CanEat = true
                },
                "Carnivore",
                manager.World.PlanService,
                manager.World.Factions.Factions["Carnivore"],
                "Spider"
            )
        {
            Physics = new Physics(
                    manager,
                    "Spider",
                    Matrix.CreateTranslation(position),
                    new Vector3(0.375f, 0.375f, 0.375f),
                    new Vector3(0.0f, 0.0f, 0.0f),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
            );

            Physics.AddChild(this);

            HasBones = false;

            Physics.Orientation = Physics.OrientMode.RotateY;
                
            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            Physics.AddChild(new PacingCreatureAI(Manager, "Spider AI", Sensors));

            Attacks = new List<Attack> {
                new Attack("Sting", 0.01f, 1.0f, 3.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_giant_spider_attack_1, ContentPaths.Audio.Oscar.sfx_oc_giant_spider_attack_2), ContentPaths.Effects.bite),
                new Attack("Web", 0.0f, 1.0f, 5.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_giant_spider_attack_1, ContentPaths.Audio.Oscar.sfx_oc_giant_spider_attack_2), ContentPaths.Effects.claw)
                {
                    Mode = Attack.AttackMode.Ranged,
                    LaunchSpeed = 10,
                    ProjectileType = "Web"
                }
            };

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

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

            CreateCosmetics(manager);
        }

        private void CreateCosmetics(ComponentManager manager)
        {
            CreateSprite(ContentPaths.Entities.Animals.Spider.spider_animation, manager, 0.3f);
            Physics.AddChild(Shadow.Create(0.4f, manager));
            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1
            }).SetFlag(Flag.ShouldSerialize, false);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateCosmetics(manager);
            base.CreateCosmeticChildren(manager);
        }

    }
}
