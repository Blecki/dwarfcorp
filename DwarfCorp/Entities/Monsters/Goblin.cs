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
    public class Goblin : Creature
    {
        [EntityFactory("Goblin")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Goblin(
                new CreatureStats("Goblin", "Goblin", null),
                Manager.World.Factions.Factions["Goblins"],
                Manager,
                "Goblin",
                Position).Physics;
        }

        [EntityFactory("Player Goblin")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Goblin(
                new CreatureStats("Goblin", "Goblin", null),
                Manager.World.PlayerFaction,
                Manager,
                "Goblin",
                Position).Physics;
        }

        public Goblin()
        {
            
        }

        public Goblin(CreatureStats stats, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, faction, name)
        {
            Physics = new Physics(manager, "goblin", Matrix.CreateTranslation(position), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(10, 5, 10), Vector3.Zero));

            Physics.AddChild(new CreatureAI(Manager, "Goblin AI", Sensor));

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));
          
            Physics.Tags.Add("Goblin");            

            Physics.AddChild(new Flammable(Manager, "Flames"));

            Stats.FullName = TextGenerator.ToTitleCase(TextGenerator.GenerateRandom("$names_goblin"));

            Stats.BaseSize = 4;
            AI.Movement.CanClimbWalls = true;
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            AI.Movement.SetCan(MoveType.Dig, true);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            var spriteSheet = new SpriteSheet("Entities\\Goblin\\goblin", 64, 40);
            var sprite = new CharacterSprite(manager, "Sprite", Matrix.CreateTranslation(0, 0.15f, 0));
            sprite.SpriteSheet = spriteSheet;

            var anims = Library.LoadNewLayeredAnimationFormat("Entities\\Goblin\\goblin-animations.json");
            sprite.SetAnimations(anims);

            Physics.AddChild(sprite);
            sprite.SetFlag(Flag.ShouldSerialize, false);

            Physics.AddChild(Shadow.Create(0.75f, manager));
            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 3, 0))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_goblin_angered,
            };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 3,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_ic_goblin_angered,
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }
    }

}
