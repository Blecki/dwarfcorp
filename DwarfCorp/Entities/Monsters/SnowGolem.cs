using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class SnowGolem : Creature
    {
        [EntityFactory("SnowGolem")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new SnowGolem(
                new CreatureStats("SnowGolem", "SnowGolem", 0),
                Manager.World.Factions.Factions["Evil"],
                Manager,
                "Snow Golem",
                Position);
        }

        public SnowGolem()
        {

        }

        public SnowGolem(CreatureStats stats, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, faction, name)
        {
            Physics = new Physics(Manager, name, Matrix.CreateTranslation(position), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(5, 5, 5), Vector3.Zero));

            Physics.AddChild(new GolemAI(Manager, Sensor) { Movement = { IsSessile = true, CanFly = false, CanSwim = false, CanWalk = false, CanClimb = false, CanClimbWalls = false } });

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            var gems = Library.EnumerateResourceTypesWithTag("Gem");
            for (int i = 0; i < 16; i++)
            {
                for (var j = 0; j < MathFunctions.RandInt(1, 32 - i); ++j)
                    Inventory.AddResource(new Resource(Datastructures.SelectRandom(gems).Name));
            }

            Physics.Tags.Add("MudGolem");
            Physics.Mass = 100;

            Stats.FullName = TextGenerator.GenerateRandom("$goblinname");
            Stats.BaseSize = 4;
            Resistances[DamageType.Fire] = 5;
            Resistances[DamageType.Acid] = 5;
            Resistances[DamageType.Cold] = 5;
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(ContentPaths.Entities.Golems.snow_golem, manager, 0.15f);
            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 3, 3))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.demon0,
                ContentPaths.Audio.gravel,
            };

            Physics.AddChild(new ParticleTrigger("snow_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.gravel
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }
    }
}