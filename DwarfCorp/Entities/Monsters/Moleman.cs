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
    public class Moleman : Creature
    {
        [EntityFactory("Moleman")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Moleman(
                new CreatureStats("Moleman", "Moleman", null),
                Manager.World.Factions.Factions["Molemen"],
                Manager,
                "Moleman",
                Position).Physics;
        }

        [EntityFactory("Player Moleman")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Moleman(
                new CreatureStats("Moleman", "Moleman", null),
                Manager.World.PlayerFaction,
                Manager,
                "Moleman",
                Position).Physics;
        }

        public Moleman()
        {
            
        }

        public Moleman(CreatureStats stats, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, faction, name)
        {
            Physics = new Physics(manager, "Moleman", Matrix.CreateTranslation(position), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.Orientation = Physics.OrientMode.RotateY;
            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(10, 5, 10), Vector3.Zero));

            Physics.AddChild(new CreatureAI(Manager, "Moleman AI", Sensor));

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Physics.Tags.Add("Necromancer");

            Physics.AddChild(new Flammable(Manager, "Flames"));

            Stats.FullName = TextGenerator.GenerateRandom("$goblinname");
            //Stats.LastName = TextGenerator.GenerateRandom("$goblinfamily");
            Stats.BaseSize = 4;
            Stats.CanEat = false;
            AI.Movement.CanClimbWalls = true;
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            AI.Movement.SetCan(MoveType.Dig, true);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            var spriteSheet = new SpriteSheet("Entities\\Moleman\\moleman", 40, 40);
            var sprite = new CharacterSprite(manager, "Sprite", Matrix.CreateTranslation(0, 0.15f, 0));
            sprite.SpriteSheet = spriteSheet;

            var anims = Library.LoadNewLayeredAnimationFormat("Entities\\Moleman\\moleman-animations.json");
            sprite.SetAnimations(anims);

            Physics.AddChild(sprite);
            sprite.SetFlag(Flag.ShouldSerialize, false);


            Physics.AddChild(Shadow.Create(0.75f, manager));
            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 0, 1))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_moleman_hurt_1,
                ContentPaths.Audio.Oscar.sfx_ic_moleman_hurt_2
            };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_ic_moleman_death
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }
    }
}
