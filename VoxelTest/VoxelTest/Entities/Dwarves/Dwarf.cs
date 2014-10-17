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

        public static List<Animation> CreateDefaultAnimations(Texture2D dwarfSprites, int frameWidth, int frameHeight)
        {
            return new List<Animation>()
            {
                
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Walking, OrientedAnimation.Orientation.Forward, dwarfSprites, 15.0f, frameWidth, frameHeight, 0, 0, 1, 2, 1, 0, 1, 2, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Walking, OrientedAnimation.Orientation.Right, dwarfSprites, 15.0f, frameWidth, frameHeight, 2, 0, 1, 2, 1, 0, 1, 2, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Walking, OrientedAnimation.Orientation.Left, dwarfSprites, 15.0f, frameWidth, frameHeight, 1, 0, 1, 2, 1, 0, 1, 2, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Walking, OrientedAnimation.Orientation.Backward, dwarfSprites, 15.0f, frameWidth, frameHeight, 3, 0, 1, 2, 1),

            CharacterSprite.CreateAnimation(Creature.CharacterMode.Idle, OrientedAnimation.Orientation.Forward, dwarfSprites, 0.8f, frameWidth, frameHeight, 0, 1, 3, 1),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Idle, OrientedAnimation.Orientation.Right, dwarfSprites, 0.8f, frameWidth, frameHeight, 2, 2, 0, 2),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Idle, OrientedAnimation.Orientation.Left, dwarfSprites, 0.8f, frameWidth, frameHeight, 1, 1, 3, 1),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Idle, OrientedAnimation.Orientation.Backward, dwarfSprites, 0.8f, frameWidth, frameHeight, 3, 1),

            CharacterSprite.CreateAnimation(Creature.CharacterMode.Attacking, OrientedAnimation.Orientation.Forward, dwarfSprites, 8.0f, frameWidth, frameHeight, 8, 0, 1, 2, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Attacking, OrientedAnimation.Orientation.Right, dwarfSprites, 8.0f, frameWidth, frameHeight, 10, 0, 1, 2, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Attacking, OrientedAnimation.Orientation.Left, dwarfSprites, 8.0f, frameWidth, frameHeight, 9, 0, 1, 2, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Attacking, OrientedAnimation.Orientation.Backward, dwarfSprites, 8.0f, frameWidth, frameHeight, 11, 0, 1, 2, 3),

            CharacterSprite.CreateAnimation(Creature.CharacterMode.Falling, OrientedAnimation.Orientation.Forward, dwarfSprites, 15.0f, frameWidth, frameHeight, 4, 1),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Falling, OrientedAnimation.Orientation.Right, dwarfSprites, 15.0f, frameWidth, frameHeight, 6, 1),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Falling, OrientedAnimation.Orientation.Left, dwarfSprites, 15.0f, frameWidth, frameHeight, 5, 1),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Falling, OrientedAnimation.Orientation.Backward, dwarfSprites, 15.0f, frameWidth, frameHeight, 7, 1),

            CharacterSprite.CreateAnimation(Creature.CharacterMode.Sleeping, OrientedAnimation.Orientation.Forward, dwarfSprites, 4.0f, frameWidth, frameHeight, 0, 4, 5, 6, 7),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Sleeping, OrientedAnimation.Orientation.Right, dwarfSprites, 4.0f, frameWidth, frameHeight, 0, 4, 5, 6, 7),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Sleeping, OrientedAnimation.Orientation.Left, dwarfSprites, 4.0f, frameWidth, frameHeight, 0, 4, 5, 6, 7),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Sleeping, OrientedAnimation.Orientation.Backward, dwarfSprites, 4.0f, frameWidth, frameHeight, 0, 4, 5, 6, 7),


            CharacterSprite.CreateAnimation(Creature.CharacterMode.Sitting, OrientedAnimation.Orientation.Forward, dwarfSprites, 15.0f, frameWidth, frameHeight, 4, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Sitting, OrientedAnimation.Orientation.Right, dwarfSprites, 15.0f, frameWidth, frameHeight, 6, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Sitting, OrientedAnimation.Orientation.Left, dwarfSprites, 15.0f, frameWidth, frameHeight, 5, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Sitting, OrientedAnimation.Orientation.Backward, dwarfSprites, 15.0f, frameWidth, frameHeight, 7, 3),


            CharacterSprite.CreateAnimation(Creature.CharacterMode.Jumping, OrientedAnimation.Orientation.Forward, dwarfSprites, 15.0f, frameWidth, frameHeight, 4, 0),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Jumping, OrientedAnimation.Orientation.Right, dwarfSprites, 15.0f, frameWidth, frameHeight, 6, 0),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Jumping, OrientedAnimation.Orientation.Left, dwarfSprites, 15.0f, frameWidth, frameHeight, 5, 0),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Jumping, OrientedAnimation.Orientation.Backward, dwarfSprites, 15.0f, frameWidth, frameHeight, 7, 0),
            };
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
                TriggerAmount = 100,
                SoundToPlay = ContentPaths.Entities.Dwarf.Audio.dwarfhurt1,
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