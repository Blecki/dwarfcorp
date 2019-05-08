using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    public class Demon : Creature
    {
        [EntityFactory("Demon")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Demon(
                new CreatureStats(CreatureClassLibrary.GetClass("Demon"), 0),
                "Demon",
                Manager.World.PlanService,
                Manager.World.Factions.Factions["Demon"],
                Manager,
                "Demon",
                Position).Physics;
        }

        [EntityFactory("Player Demon")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Demon(
                new CreatureStats(CreatureClassLibrary.GetClass("Demon"), 0),
                Manager.World.PlayerFaction.Name, Manager.World.PlanService, Manager.World.PlayerFaction,
                Manager,
                "Demon",
                Position).Physics;
        }

        public Demon()
        {
            
        }

        public Demon(CreatureStats stats, string allies, PlanService planService, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, allies, planService, faction, name)
        {
            Physics = new Physics(manager, "Demon", Matrix.CreateTranslation(position), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            Physics.AddChild(new PacingCreatureAI(Manager, "Demon AI", Sensors) { Movement = { CanFly = true, CanSwim = false, CanDig = true} });

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));
            
            Physics.Tags.Add("Demon");

            Stats.FullName = TextGenerator.ToTitleCase(TextGenerator.GenerateRandom("$names_demon"));
            Stats.BaseSize = 4;
            Species = "Demon";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Stats.CurrentClass = CreatureClassLibrary.GetClass("Demon");
            CreateSprite(AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.Demon.demon_animations, "Demon"), manager);
            Physics.AddChild(Shadow.Create(0.75f, manager));
            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 3, 1))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_demon_hurt_1,
                ContentPaths.Audio.Oscar.sfx_ic_demon_hurt_2,
            };

            NoiseMaker.Noises["Chew"] = new List<string>
            {
                ContentPaths.Audio.chew
            };

            NoiseMaker.Noises["Jump"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_demon_angered,
            };

            NoiseMaker.Noises["Flap"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_demon_flap_wings_1,
                ContentPaths.Audio.Oscar.sfx_ic_demon_flap_wings_2,
                ContentPaths.Audio.Oscar.sfx_ic_demon_flap_wings_3,
            };

            NoiseMaker.Noises["Chirp"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_demon_mumble_1,
                ContentPaths.Audio.Oscar.sfx_ic_demon_mumble_2,
                ContentPaths.Audio.Oscar.sfx_ic_demon_mumble_3,
                ContentPaths.Audio.Oscar.sfx_ic_demon_mumble_4,
                ContentPaths.Audio.Oscar.sfx_ic_demon_mumble_5,
                ContentPaths.Audio.Oscar.sfx_ic_demon_mumble_6,
                ContentPaths.Audio.Oscar.sfx_ic_demon_pleased,
            };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_ic_demon_death
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }
    }
}
