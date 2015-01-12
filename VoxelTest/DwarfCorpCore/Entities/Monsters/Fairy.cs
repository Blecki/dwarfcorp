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

        public Timer ParticleTimer { get; set; }
        public Timer DeathTimer { get; set; }
        public Fairy()
        {

        }
        public Fairy(string allies, Vector3 position) :
            base( new CreatureStats(new FairyClass(), 0), "Dwarf", PlayState.PlanService, PlayState.ComponentManager.Factions.Factions[allies],
           new Physics("Fairy", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(position),
                       new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, 0, 0)),
              PlayState.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Fairy")
        {
            ParticleTimer = new Timer(0.2f, false);
            DeathTimer = new Timer(30.0f, true);
            Initialize(new FairyClass());
        }

        public override void Update(DwarfTime DwarfTime, ChunkManager chunks, Camera camera)
        {
            if (ParticleTimer.HasTriggered)
            {
                PlayState.ParticleManager.Trigger("star_particle", Sprite.Position, Color.White, 1);    
            }
            DeathTimer.Update(DwarfTime);
            ParticleTimer.Update(DwarfTime);

            if (DeathTimer.HasTriggered)
            {
                Physics.Die();
            }

            base.Update(DwarfTime, chunks, camera);
        }

        

        public void Initialize(EmployeeClass dwarfClass)
        {
            Physics.Orientation = Physics.OrientMode.RotateY;
            Sprite = new CharacterSprite(Graphics, Manager, "Fairy Sprite", Physics, Matrix.CreateTranslation(new Vector3(0, 0.5f, 0)));
            foreach (Animation animation in dwarfClass.Animations)
            {
                Sprite.AddAnimation(animation.Clone());
            }
            Sprite.LightsWithVoxels = false;

            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);

            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            AI = new CreatureAI(this, "Fairy AI", Sensors, PlanService);

            Attacks = new List<Attack>() { new Attack(dwarfClass.MeleeAttack) };


            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 128
                }
            };

            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

            Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform, TextureManager.GetTexture(ContentPaths.Effects.shadowcircle));
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(Graphics, TextureManager.GetTexture(ContentPaths.Effects.shadowcircle), "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");
            Physics.Tags.Add("Dwarf");



            DeathParticleTrigger = new ParticleTrigger("star_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.wurp,
            };
          
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

            MinimapIcon minimapIcon = new MinimapIcon(Physics, new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.map_icons), 16, 0, 0));

            //new LightEmitter("Light Emitter", Sprite, Matrix.Identity, Vector3.One, Vector3.One, 255, 150);
            new Bobber(0.25f, 3.0f, MathFunctions.Rand(), Sprite);
          
            Stats.FirstName = TextGenerator.GenerateRandom("$DwarfName");
            Stats.LastName = "The Fairy";
            
            Stats.CanSleep = false;
            Stats.CanEat = false;
        }
    }

}