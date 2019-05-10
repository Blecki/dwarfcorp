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
    public class Elf : Creature
    {
        [EntityFactory("Elf")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Elf(
                new CreatureStats("Elf", "Elf", 0),
                "Elf",
                Manager.World.Factions.Factions["Elf"],
                Manager,
                "Elf",
                Position).Physics;
        }

        [EntityFactory("Player Elf")]
        private static GameComponent __factory5(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var toReturn = new Elf(new CreatureStats("Elf", "Elf", 0), Manager.World.PlayerFaction.Name, Manager.World.PlayerFaction, Manager, "elf", Position);
            return toReturn.Physics;
        }

        public Elf()
        {
            
        }

        public Elf(CreatureStats stats, string allies, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, allies, faction, name)
        {
            Physics = new Physics(manager, "Elf", Matrix.CreateTranslation(position), new Vector3(0.5f, 1.0f, 0.5f), new Vector3(0.0f, -0.0f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));
            Physics.AddChild(this);
            Initialize();
        }

        public void Initialize()
        {
            Physics.Orientation = Physics.OrientMode.RotateY;
            CreateCosmeticChildren(Manager);

            HasBones = false;

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            Physics.AddChild(new CreatureAI(Manager, "Elf AI", Sensors));

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Physics.Tags.Add("Elf");

            Physics.AddChild(new Flammable(Manager, "Flames"));

            Stats.FullName = TextGenerator.GenerateRandom("$elfname");
            Stats.BaseSize = 4;
            AI.Movement.CanClimbWalls = true;
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            AI.Movement.SetCan(MoveType.Dig, true);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.Elf.Sprites.elf_animation, "Elf"), manager);
            Physics.AddChild(Shadow.Create(0.75f, manager));
            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 1, 1))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_elf_hurt_1,
                ContentPaths.Audio.Oscar.sfx_ic_elf_hurt_2,
            };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_ic_elf_death
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }
    }

}
