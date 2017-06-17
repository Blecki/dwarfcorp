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
            base(manager, new CreatureStats(new FairyClass(), 0), "Player", manager.World.PlanService, manager.World.Factions.Factions[allies],
           
              manager.World.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Fairy")
        {
            Physics = new Physics(manager, "Fairy", Matrix.CreateTranslation(position),
                       new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, 0, 0));

            Physics.AddChild(this);

            SelectionCircle = Physics.AddChild(new SelectionCircle(Manager)
            {
                IsVisible = false
            }) as SelectionCircle;

            HasMeat = false;
            HasBones = false;
            ParticleTimer = new Timer(0.2f, false);
            DeathTimer = new Timer(30.0f, true);
            Initialize(new FairyClass());
        }

        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
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
            Sprite = Physics.AddChild(new CharacterSprite(Graphics, Manager, "Fairy Sprite", Matrix.CreateTranslation(new Vector3(0, 0.5f, 0)))) as CharacterSprite;
            foreach (Animation animation in dwarfClass.Animations)
            {
                Sprite.AddAnimation(animation.Clone());
            }
            Sprite.LightsWithVoxels = false;

            Hands = Physics.AddChild(new Grabber("hands", Manager, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero)) as Grabber;

            Sensors = Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero)) as EnemySensor;

            AI = Physics.AddChild(new CreatureAI(Manager, "Fairy AI", Sensors, PlanService)) as CreatureAI;

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Inventory = Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.BoundingBoxPos)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 128
                }
            }) as Inventory;

            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

            Shadow = Physics.AddChild(new Shadow(Manager, "Shadow", shadowTransform, new SpriteSheet(ContentPaths.Effects.shadowcircle))) as Shadow;
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle), "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");
            Physics.Tags.Add("Dwarf");



            DeathParticleTrigger = Physics.AddChild(new ParticleTrigger("star_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.wurp,
            }) as ParticleTrigger;
          
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
            Physics.AddChild(new Bobber(Manager, 0.25f, 3.0f, MathFunctions.Rand(), Physics.LocalTransform.Translation.Y));
          
            Stats.FullName = TextGenerator.GenerateRandom("$firstname");
            //Stats.LastName = "The Fairy";
            
            Stats.CanSleep = false;
            Stats.CanEat = false;
            AI.Movement.CanClimbWalls = true;
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            Species = "Fairy";
        }
    }

}
