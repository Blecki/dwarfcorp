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
    public class WhiteRabbit : Creature
    {
        [EntityFactory("White Rabbit")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new WhiteRabbit(Position, Manager);
        }

        public WhiteRabbit()
        {

        }

        public WhiteRabbit(Vector3 position, ComponentManager manager) :
            base
            (
                manager,
                new CreatureStats("White Rabbit", "White Rabbit", null),
                manager.World.Factions.Factions["Herbivore"],
                "White Rabbit"
            )
        {
            Physics = new Physics
                (
                Manager,
                    "White Rabbit",
                    Matrix.CreateTranslation(position),
                    new Vector3(0.25f, 0.25f, 0.25f),
                    new Vector3(0.0f, 0.0f, 0.0f),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                );

            Physics.AddChild(this);

            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(5, 5, 5), Vector3.Zero));
            Physics.AddChild(new PacingCreatureAI(Manager, "Rabbit AI", Sensor));
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));
            Physics.AddChild(new Flammable(Manager, "Flames"));

            Physics.Tags.Add("Rabbit");
            Physics.Tags.Add("Animal");
            Physics.Tags.Add("DomesticAnimal");
            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the rabbit";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(ContentPaths.Entities.Animals.Rabbit.rabbit1_animation, manager, 0.35f);
            Physics.AddChild(Shadow.Create(0.3f, manager));

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
    }
}
