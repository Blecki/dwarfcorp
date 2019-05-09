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
    public class Troll : Creature
    {
        [EntityFactory("Troll")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Troll(
                new CreatureStats("Troll", 0),
                "Goblins",
                Manager.World.PlanService,
                Manager.World.Factions.Factions["Goblins"],
                Manager,
                "Troll",
                Position).Physics;
        }

        public Troll()
        {

        }
        public Troll(CreatureStats stats, string allies, PlanService planService, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, allies, planService, faction, name)
        {
            Physics = new Physics(manager, "Troll", Matrix.CreateTranslation(position), new Vector3(0.5f, 0.9f, 0.5f), new Vector3(0.0f, 0.0f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.Orientation = Physics.OrientMode.RotateY;
            CreateCosmeticChildren(Manager);

            HasMeat = true;
            HasBones = true;

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            Physics.AddChild(new CreatureAI(Manager, "Troll AI", Sensors));

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Physics.Tags.Add("Troll");

            Physics.AddChild(new Flammable(Manager, "Flames"));

            Stats.FullName = TextGenerator.GenerateRandom("$goblinname");
            AI.Movement.CanClimbWalls = true;
            AI.Movement.CanSwim = true;
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            AI.Movement.SetSpeed(MoveType.Swim, 0.15f);
            AI.Movement.SetCan(MoveType.Dig, true);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.Troll.troll_animation, "Troll"), manager);
            Physics.AddChild(Shadow.Create(0.75f, manager));
            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 1, 3))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_goblin_angered,
            };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 3,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_ic_demon_hurt_1,
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }
    }
}
