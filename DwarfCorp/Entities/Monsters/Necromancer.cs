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
    public class Necromancer : Creature
    {
        [EntityFactory("Necromancer")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Necromancer(
                new CreatureStats("Necromancer", "Necromancer", null),
                Manager.World.Factions.Factions["Undead"],
                Manager,
                "Necromancer",
                Position).Physics;
        }

        [EntityFactory("Player Necromancer")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Necromancer(
                new CreatureStats("Necromancer", "Necromancer", null),
                Manager.World.PlayerFaction,
                Manager,
                "Necromancer",
                Position).Physics;
        }

        public Necromancer()
        {
            
        }

        public Necromancer(CreatureStats stats, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, faction, name)
        {
            Physics = new Physics(manager, "Necromancer", Matrix.CreateTranslation(position), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.Orientation = Physics.OrientMode.RotateY;
            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(10, 5, 10), Vector3.Zero));

            Physics.AddChild(new NecromancerAI(Manager, "Necromancer AI", Sensor));

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Physics.Tags.Add("Necromancer");

            Physics.AddChild(new Flammable(Manager, "Flames"));
            
            Stats.FullName = TextGenerator.GenerateRandom("$title") + " " + TextGenerator.ToTitleCase(TextGenerator.GenerateRandom("$names_undead"));
            Stats.BaseSize = 4;
            Stats.CanEat = false;
            AI.Movement.CanClimbWalls = true;
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            AI.Movement.SetCan(MoveType.Dig, true);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(ContentPaths.Entities.Skeleton.necro_animations, manager, 0.15f);
            Physics.AddChild(Shadow.Create(0.75f, manager));
            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 2, 1))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_necromancer_angered,
                ContentPaths.Audio.skel1,
                ContentPaths.Audio.skel2
            };

            Physics.AddChild(new ParticleTrigger("sand_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_ic_necromancer_angered
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }
    }
}
