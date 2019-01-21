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
    public class Fairy : Creature
    {
        [EntityFactory("Fairy")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Fairy(Manager, "Player", Position);
        }

        private static FairyClass SharedClass = new FairyClass(true);

        public Timer ParticleTimer { get; set; }
        public DateTimer DeathTimer { get; set; }
        public Fairy()
        {

        }

        public Fairy(ComponentManager manager, string allies, Vector3 position) :
            base(manager, new CreatureStats(SharedClass, 0), "Player", manager.World.PlanService, manager.World.Factions.Factions[allies], "Fairy")
        {
            Physics = new Physics(manager, "Fairy", Matrix.CreateTranslation(position),
                      new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            HasMeat = false;
            HasBones = false;
            ParticleTimer = new Timer(0.2f, false);
            DeathTimer = new DateTimer(manager.World.Time.CurrentDate, new TimeSpan(1, 0, 0, 0, 0));
            
            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            Physics.AddChild(new CreatureAI(Manager, "Fairy AI", Sensors));

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Physics.AddChild(Shadow.Create(0.75f, Manager));

            Physics.Tags.Add("Dwarf");

            Physics.AddChild(new ParticleTrigger("star_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.wurp,
            });
          
            Stats.FullName = TextGenerator.GenerateRandom("$firstname");
            
            Stats.CanSleep = false;
            Stats.CanEat = false;
            AI.Movement.CanClimbWalls = true;
            AI.Movement.CanFly = true;
            AI.Movement.SetCost(MoveType.Fly, 1.0f);
            AI.Movement.SetSpeed(MoveType.Fly, 1.0f);
            AI.Movement.SetCost(MoveType.ClimbWalls, 1.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 1.0f);
            AI.Movement.SetCan(MoveType.Dig, true);
            Species = "Fairy";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Stats.CurrentClass = SharedClass;

            CreateSprite(Stats.CurrentClass, manager);
            Sprite.AddChild(new Bobber(Manager, 0.25f, 3.0f, MathFunctions.Rand(), Sprite.LocalTransform.Translation.Y)).SetFlag(Flag.ShouldSerialize, false);
            Sprite.LightsWithVoxels = false;

            Physics.AddChild(Shadow.Create(0.75f, manager));
            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 0, 0))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.tinkle
            };

            NoiseMaker.Noises["Chew"] = new List<string>
            {
                ContentPaths.Audio.tinkle
            };

            NoiseMaker.Noises["Jump"] = new List<string>
            {
                ContentPaths.Audio.tinkle
            };

            base.CreateCosmeticChildren(manager);
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (Active)
            {
                if (ParticleTimer.HasTriggered)
                {
                    Manager.World.ParticleManager.Trigger("star_particle", Sprite.Position, Color.White, 1);
                }
                DeathTimer.Update(World.Time.CurrentDate);
                ParticleTimer.Update(gameTime);

                if (DeathTimer.HasTriggered)
                {
                    Physics.Die();
                }
            }
            base.Update(gameTime, chunks, camera);
        }
    }
}
