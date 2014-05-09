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
    public class Deer : Creature
    {
        private float ANIM_SPEED = 5.0f;


        public Deer(string sprites, Vector3 position, ComponentManager manager, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, string name):
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
                    "deer",
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
            Physics.OrientWithVelocity = true;

            const int frameWidth = 48;
            const int frameHeight = 40;

            Sprite = new CharacterSprite
                (Graphics,
                Manager,
                "Deer Sprite",
                Physics,
                Matrix.Identity
                );

            // Add the idle animation
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Forward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Left, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 1, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Right, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 2, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Backward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 3, 0);

            // Add the running animation
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Forward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 4, 0, 1, 2, 3);
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Left, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 5, 0, 1, 2, 3);
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Right, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 6, 0, 1, 2, 3);
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Backward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 7, 0, 1, 2, 3);

            // Add the jumping animation
            Sprite.AddAnimation(CharacterMode.Jumping, OrientedAnimation.Orientation.Forward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 8, 0, 1);
            Sprite.AddAnimation(CharacterMode.Jumping, OrientedAnimation.Orientation.Left, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 9, 0, 1);
            Sprite.AddAnimation(CharacterMode.Jumping, OrientedAnimation.Orientation.Right, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 10, 0, 1);
            Sprite.AddAnimation(CharacterMode.Jumping, OrientedAnimation.Orientation.Backward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 11, 0, 1);

            // TODO: figure out what these numbers mean
            // Add hands
            Hands = new Grabber(Manager, "hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);

            // Add sensor
            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            // Add AI
            AI = new CreatureAIComponent(this, "Deer AI", Sensors, PlanService);

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
            Physics.Tags.Add("Deer");
            Physics.Tags.Add("Animal");

        }

    }
}
