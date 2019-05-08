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
    public class Bat : Creature
    {
        [EntityFactory("Bat")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Bat(Manager, Position);
        }

        public Bat()
        {

        }

        public Bat(ComponentManager manager, Vector3 position) :
            base
            (
                manager,
                new CreatureStats(CreatureClassLibrary.GetClass("Bat"), 0)
                {
                    CanSleep = false,
                    CanEat = true
                },
                "Carnivore",
                manager.World.PlanService,
                manager.World.Factions.Factions["Carnivore"],
                "Bat"
            )
        {
            Physics = new Physics
                (
                manager,
                    "bat",
                    Matrix.CreateTranslation(position),
                    new Vector3(0.375f, 0.375f, 0.375f),
                    new Vector3(0.0f, 0.0f, 0.0f),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                );

            Physics.AddChild(this);

            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            Physics.AddChild(new BatAI(Manager, "Bat AI", Sensors));
            AI.Movement.CanFly = true;
            AI.Movement.CanSwim = false;
            AI.Movement.CanClimb = false;
            AI.Movement.CanWalk = false;

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));
            Physics.AddChild(new Flammable(Manager, "Flames"));

            Physics.Tags.Add("Bat");
            Physics.Tags.Add("Animal");

            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the bat";

            Species = "Bat";
            Stats.CanReproduce = true;
            BabyType = "Bat";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Stats.CurrentClass = CreatureClassLibrary.GetClass("Bat");

            CreateSprite(ContentPaths.Entities.Animals.Bat.bat_animations, manager, 0.0f);
            Physics.AddChild(Shadow.Create(0.3f, manager));

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_bat_hurt_1 };
            NoiseMaker.Noises["Chirp"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_bat_neutral_1, ContentPaths.Audio.Oscar.sfx_oc_bat_neutral_2 };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }
    }
}