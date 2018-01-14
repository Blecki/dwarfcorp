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

    public class Fairy : Creature, IUpdateableComponent
    {

        public Timer ParticleTimer { get; set; }
        public Timer DeathTimer { get; set; }
        public Fairy()
        {

        }
        public Fairy(ComponentManager manager, string allies, Vector3 position) :
            base(manager, new CreatureStats(new FairyClass(), 0), "Player", manager.World.PlanService, manager.World.Factions.Factions[allies], "Fairy")
        {
            Physics = new Physics(manager, "Fairy", Matrix.CreateTranslation(position),
                      new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.AddChild(new SelectionCircle(Manager)
            {
                IsVisible = false
            });

            HasMeat = false;
            HasBones = false;
            ParticleTimer = new Timer(0.2f, false);
            DeathTimer = new Timer(30.0f, true);
            Initialize(new FairyClass());
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (ParticleTimer.HasTriggered)
            {
                Manager.World.ParticleManager.Trigger("star_particle", Sprite.Position, Color.White, 1);    
            }
            DeathTimer.Update(gameTime);
            ParticleTimer.Update(gameTime);

            if (DeathTimer.HasTriggered)
            {
                Physics.Die();
            }

            base.Update(gameTime, chunks, camera);
        }

        

        public void Initialize(EmployeeClass dwarfClass)
        {
            Physics.Orientation = Physics.OrientMode.RotateY;
            CreateSprite(Stats.CurrentClass, Manager);
            Sprite.LightsWithVoxels = false;

            Hands = Physics.AddChild(new Grabber("hands", Manager, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero)) as Grabber;

            Sensors = Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero)) as EnemySensor;

            AI = Physics.AddChild(new CreatureAI(Manager, "Fairy AI", Sensors, PlanService)) as CreatureAI;

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Inventory = Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset)) as Inventory;

            Physics.AddChild(Shadow.Create(0.75f, Manager));

            Physics.Tags.Add("Dwarf");

            Physics.AddChild(new ParticleTrigger("star_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.wurp,
            });
          
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

            MinimapIcon minimapIcon = Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 0, 0))) as MinimapIcon;

            //new LightEmitter("Light Emitter", Sprite, Matrix.Identity, Vector3.One, Vector3.One, 255, 150);
            Stats.FullName = TextGenerator.GenerateRandom("$firstname");
            //Stats.LastName = "The Fairy";
            
            Stats.CanSleep = false;
            Stats.CanEat = false;
            AI.Movement.CanClimbWalls = true;
            AI.Movement.CanFly = true;
            AI.Movement.SetCost(MoveType.Fly, 1.0f);
            AI.Movement.SetSpeed(MoveType.Fly, 1.0f);
            AI.Movement.SetCost(MoveType.ClimbWalls, 1.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 1.0f);
            
            Species = "Fairy";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateSprite(Stats.CurrentClass, manager);
            var bobber = Sprite.AddChild(new Bobber(Manager, 0.25f, 3.0f, MathFunctions.Rand(), Sprite.LocalTransform.Translation.Y));
            bobber.SetFlag(Flag.ShouldSerialize, false);

            Sprite.LightsWithVoxels = false;

            Physics.AddChild(Shadow.Create(0.75f, manager));
            base.CreateCosmeticChildren(manager);
        }
    }

}
