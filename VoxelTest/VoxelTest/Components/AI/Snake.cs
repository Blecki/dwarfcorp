using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Snake : Creature
    {
        private float ANIM_SPEED = 5.0f;
        public CharacterSprite Tail;

        public Snake(string sprites, Vector3 position, ComponentManager manager, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, string name):
            base
            (
                new CreatureStats
                {
                    Dexterity = 12,
                    Constitution = 6,
                    Strength = 3,
                    Wisdom = 2,
                    Charisma = 1,
                    Intelligence = 3,
                    Size = 3
                },
                "Herbivore",
                PlayState.PlanService,
                manager.Factions.Factions["Herbivore"],
                new PhysicsComponent
                (
                    manager,
                    "snake",
                    manager.RootComponent,
                    Matrix.CreateTranslation(position),
                    new Vector3(2, 1.5f, .7f),
                    new Vector3(0, 0, 0),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                ),
                manager, chunks, graphics, content, name
            )
        {
            Initialize(TextureManager.GetTexture(sprites));
        }

        public void Initialize(Texture2D spriteSheet)
        {
            Physics.OrientWithVelocity = false;

            const int frameWidth = 32;
            const int frameHeight = 32;

            Sprite = new CharacterSprite
                (Graphics,
                Manager,
                "snake Sprite",
                Physics,
                Matrix.Identity
                );

            // Add the idle animation
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Forward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Left, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Right, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Backward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 0);

            Tail = new CharacterSprite
                (Graphics,
                Manager,
                "tail Sprite",
                Physics,
                Matrix.CreateTranslation(1, 0, 0)
                );

            Tail.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Forward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 1);
            Tail.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Left, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 1);
            Tail.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Right, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 1);
            Tail.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Backward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 1);

            Tail.SetCurrentAnimation(CharacterMode.Idle.ToString());
            //Physics.DrawBoundingBox = true;
            // TODO: figure out what these numbers mean
            // Add hands
            Hands = new Grabber(Manager, "hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);

            // Add sensor
            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            // Add AI
            AI = new CreatureAIComponent(this, "snake AI", Sensors, PlanService);

            Health = new HealthComponent(Manager, "Health", Physics, Stats.MaxHealth, 0.0f, Stats.MaxHealth);

            Weapon = new Weapon("None", 0.0f, 0.0f, 0.0f, AI, ContentPaths.Audio.pick);

            Inventory = new Inventory(Manager, "Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 1
                }
            };

            // Shadow
            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -.25f, 0.0f);
            Texture2D shadowTexture = TextureManager.GetTexture(ContentPaths.Effects.shadowcircle);
            Shadow = new ShadowComponent(Manager, "Shadow", Physics, shadowTransform, shadowTexture);

            List<Point> shP = new List<Point>
            {
                new Point(0,0)
            };
            Animation shadowAnimation = new Animation(Graphics, shadowTexture, "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");

            // The bird will emit a shower of blood when it dies
            DeathEmitter = new EmitterComponent("blood_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 100
            };

            // The bird is flammable, and can die when exposed to fire.
            Flames = new FlammableComponent(Manager, "Flames", Physics, Health);

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Snake");
            Physics.Tags.Add("Animal");

        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);
            double time = gameTime.TotalGameTime.TotalSeconds * 30;
            Tail.LocalTransform = Matrix.CreateTranslation( (float) Math.Cos(time), 0, (float) Math.Sin(time));
        }
    }
}
