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
                new CreatureStats(SharedClass, 0),
                "Goblins",
                Manager.World.PlanService,
                Manager.World.Factions.Factions["Goblins"],
                Manager,
                "Goblin",
                Position).Physics;
        }

        [EntityFactory("Player Goblin")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Goblin(
                new CreatureStats(SharedClass, 0),
               Manager.World.PlayerFaction.Name, Manager.World.PlanService, Manager.World.PlayerFaction,
                Manager,
                "Goblin",
                Position).Physics;
        }

        private static GoblinClass SharedClass = new GoblinClass(true);

        public Goblin()
        {
            
        }

        public Goblin(CreatureStats stats, string allies, PlanService planService, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, allies, planService, faction, name)
        {
            Physics = new Physics(manager, "goblin", Matrix.CreateTranslation(position), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            HasMeat = false;
            HasBones = false;

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            Physics.AddChild(new CreatureAI(Manager, "Goblin AI", Sensors));

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));
          
            Physics.Tags.Add("Goblin");            

            Physics.AddChild(new Flammable(Manager, "Flames"));

            Stats.FullName = TextGenerator.ToTitleCase(TextGenerator.GenerateRandom("$names_goblin"));

            Stats.BaseSize = 4;
            AI.Movement.CanClimbWalls = true;
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            AI.Movement.SetCan(MoveType.Dig, true);
            Species = "Goblin";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Stats.CurrentClass = SharedClass;

            CreateSprite(AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.Goblin.Sprites.goblin_animations, "Goblin"), manager);
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
