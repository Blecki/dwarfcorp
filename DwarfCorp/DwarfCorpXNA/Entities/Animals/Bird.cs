using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Bird : Creature
    {
        [EntityFactory("Bird")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Bird(ContentPaths.Entities.Animals.Birds.GetRandomBird(), Position, Manager, "Bird");
        }

        public string SpriteAsset { get; set; }

        public Bird()
        {
            
        }

        public Bird(string sprites, Vector3 position, ComponentManager manager, string name) :
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
                    LaysEggs = true,
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
                manager,
                    "A Bird",
                    Matrix.CreateTranslation(position),
                    new Vector3(0.25f, 0.25f, 0.25f),
                    new Vector3(0.0f, 0.0f, 0.0f),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                );

            Physics.Orientation = Physics.OrientMode.RotateY;

            Physics.AddChild(this);


            SpriteAsset = sprites;
            BaseMeatResource = "Bird Meat";
            CreateSprite(ContentPaths.Entities.Animals.Birds.GetBirdAnimations(SpriteAsset), Manager, 0.35f);

            // Used to sense hostile creatures
             Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            // Controls the behavior of the creature
            Physics.AddChild(new BirdAI(Manager, "Bird AI", Sensors));
            
            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Peck", 0.1f, 2.0f, 1.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_bird_attack), ContentPaths.Effects.pierce) { Mode = Attack.AttackMode.Dogfight } };


            // The bird can hold one item at a time in its inventory
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Physics.AddChild(Shadow.Create(0.25f, Manager));

            // The bird will emit a shower of blood when it dies
            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_oc_bird_hurt
            });

            // The bird is flammable, and can die when exposed to fire.
            Physics.AddChild(new Flammable(Manager, "Flames"));

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Bird");
            Physics.Tags.Add("Animal");
            Physics.Tags.Add("DomesticAnimal");
            NoiseMaker.Noises.Add("chirp", new List<string>(){ContentPaths.Audio.Oscar.sfx_oc_bird_neutral_1, ContentPaths.Audio.Oscar.sfx_oc_bird_neutral_2});
            NoiseMaker.Noises["Hurt"] =  new List<string>(){ContentPaths.Audio.Oscar.sfx_oc_bird_hurt};
            NoiseMaker.Noises["Lay Egg"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_bird_lay_egg };
            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the bird";
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Bird",
                Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Bird" } }
            };

            AI.Movement.CanFly = true;
            AI.Movement.CanWalk = false;
            AI.Movement.CanClimb = false;
            Species = "Bird";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(ContentPaths.Entities.Animals.Birds.GetBirdAnimations(SpriteAsset), Manager, 0.35f);
            Physics.AddChild(Shadow.Create(0.3f, manager));

            base.CreateCosmeticChildren(manager);
        }
    }
}
