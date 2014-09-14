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

    /// <summary>
    /// Convenience class for initializing Dwarves as Creatures.
    /// </summary>
    public class Dwarf : Creature
    {
        public Texture2D SpriteSheet { get; set; }

        public Dwarf()
        {
            
        }
        public Dwarf(CreatureStats stats, string allies, PlanService planService, Faction faction,  string name, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, EmployeeClass workerClass, Vector3 position) :
            base(stats, allies, planService, faction, 
            new Physics( "Dwarf", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(position), 
                        new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0)),
               chunks, graphics, content, name)
        {
            SpriteSheet = workerClass.Animations[0].SpriteSheet;
            Initialize(workerClass);
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);
        }

        public void Initialize(EmployeeClass dwarfClass)
        {
            Physics.OrientWithVelocity = true;
            Sprite = new CharacterSprite(Graphics, Manager, "Dwarf Sprite", Physics, Matrix.CreateTranslation(new Vector3(0, 0.1f, 0)));
            foreach (Animation animation in dwarfClass.Animations)
            {
                Sprite.AddAnimation(new Animation(animation, animation.SpriteSheet, GameState.Game.GraphicsDevice));
            }

            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);

            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            AI = new CreatureAI(this, "Dwarf AI", Sensors, PlanService);

            Attacks = new List<Attack>() {new Attack(dwarfClass.MeleeAttack)};

            Health = new Health(Manager, "HP", Physics, Stats.MaxHealth, 0.0f, Stats.MaxHealth);

            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 128
                }
            };

            Matrix shadowTransform = Matrix.CreateRotationX((float) Math.PI * 0.5f);
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

            DeathParticleTrigger = new ParticleTrigger("blood_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 100
            };
            Flames = new Flammable(Manager, "Flames", Physics, Health);

            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Entities.Dwarf.Audio.dwarfhurt1,
                ContentPaths.Entities.Dwarf.Audio.dwarfhurt2,
                ContentPaths.Entities.Dwarf.Audio.dwarfhurt3,
                ContentPaths.Entities.Dwarf.Audio.dwarfhurt4,
            };


            NoiseMaker.Noises["Chew"] = new List<string>
            {
                ContentPaths.Audio.chew
            };

            NoiseMaker.Noises["Jump"] = new List<string>
            {
                ContentPaths.Audio.jump
            };

            MinimapIcon minimapIcon = new MinimapIcon(Physics, new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.map_icons), 16, 0, 0));


            Stats.FirstName = TextGenerator.GenerateRandom("$DwarfName");
            Stats.LastName = TextGenerator.GenerateRandom("$DwarfFamily");
            Stats.Size = 5;
            Stats.CanSleep = true;
        }
    }

}