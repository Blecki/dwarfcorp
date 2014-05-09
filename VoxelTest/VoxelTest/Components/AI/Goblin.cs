using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{

    /// <summary>
    /// Convenience class for initializing goblins as creatures.
    /// </summary>
    public class Goblin : Creature
    {
        public Goblin(CreatureStats stats, string allies, PlanService planService, Faction faction, ComponentManager manager, string name, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, Texture2D GoblinTexture, Vector3 position) :
            base(stats, allies, planService, faction, new PhysicsComponent(manager, "goblin", manager.RootComponent, Matrix.CreateTranslation(position), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0)),
                manager, chunks, graphics, content, name)
        {
            Initialize(GoblinTexture);
        }

        public void Initialize(Texture2D goblinSprites)
        {
            Physics.OrientWithVelocity = true;
            const int frameWidth = 24;
            const int frameHeight = 32;
            Sprite = new CharacterSprite(Graphics, Manager, "Goblin Sprite", Physics, Matrix.CreateTranslation(new Vector3(0, 0.1f, 0)));

            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Forward, goblinSprites, 6.0f, frameWidth, frameHeight, 0, 0, 1);
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Right, goblinSprites, 6.0f, frameWidth, frameHeight, 2, 0, 1);
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Left, goblinSprites, 6.0f, frameWidth, frameHeight, 1, 0, 1);
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Backward, goblinSprites, 6.0f, frameWidth, frameHeight, 3, 0, 1);

            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Forward, goblinSprites, 0.8f, frameWidth, frameHeight, 0, 1, 0, 1);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Right, goblinSprites, 0.8f, frameWidth, frameHeight, 2, 2, 0, 1);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Left, goblinSprites, 0.8f, frameWidth, frameHeight, 1, 1, 0, 1);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Backward, goblinSprites, 0.8f, frameWidth, frameHeight, 0, 1);

            Sprite.AddAnimation(CharacterMode.Attacking, OrientedAnimation.Orientation.Forward, goblinSprites, 5.0f, frameWidth, frameHeight, 8, 0, 1);
            Sprite.AddAnimation(CharacterMode.Attacking, OrientedAnimation.Orientation.Right, goblinSprites, 5.0f, frameWidth, frameHeight, 10, 0, 1);
            Sprite.AddAnimation(CharacterMode.Attacking, OrientedAnimation.Orientation.Left, goblinSprites, 5.0f, frameWidth, frameHeight, 9, 0, 1);
            Sprite.AddAnimation(CharacterMode.Attacking, OrientedAnimation.Orientation.Backward, goblinSprites, 5.0f, frameWidth, frameHeight, 11, 0, 1);

            Sprite.AddAnimation(CharacterMode.Falling, OrientedAnimation.Orientation.Forward, goblinSprites, 6.0f, frameWidth, frameHeight, 4, 0);
            Sprite.AddAnimation(CharacterMode.Falling, OrientedAnimation.Orientation.Right, goblinSprites, 6.0f, frameWidth, frameHeight, 6, 0);
            Sprite.AddAnimation(CharacterMode.Falling, OrientedAnimation.Orientation.Left, goblinSprites, 6.0f, frameWidth, frameHeight, 5, 0);
            Sprite.AddAnimation(CharacterMode.Falling, OrientedAnimation.Orientation.Backward, goblinSprites, 6.0f, frameWidth, frameHeight, 7, 0);

            Sprite.AddAnimation(CharacterMode.Jumping, OrientedAnimation.Orientation.Forward, goblinSprites, 6.0f, frameWidth, frameHeight, 4, 1);
            Sprite.AddAnimation(CharacterMode.Jumping, OrientedAnimation.Orientation.Right, goblinSprites, 6.0f, frameWidth, frameHeight, 6, 1);
            Sprite.AddAnimation(CharacterMode.Jumping, OrientedAnimation.Orientation.Left, goblinSprites, 6.0f, frameWidth, frameHeight, 5, 1);
            Sprite.AddAnimation(CharacterMode.Jumping, OrientedAnimation.Orientation.Backward, goblinSprites, 6.0f, frameWidth, frameHeight, 7, 1);

            Sprite.AddAnimation(CharacterMode.Swimming, OrientedAnimation.Orientation.Forward, goblinSprites, 6.0f, frameWidth, frameHeight, 12, 0, 1);
            Sprite.AddAnimation(CharacterMode.Swimming, OrientedAnimation.Orientation.Right, goblinSprites, 6.0f, frameWidth, frameHeight, 14, 0, 1);
            Sprite.AddAnimation(CharacterMode.Swimming, OrientedAnimation.Orientation.Left, goblinSprites, 6.0f, frameWidth, frameHeight, 13, 0, 1);
            Sprite.AddAnimation(CharacterMode.Swimming, OrientedAnimation.Orientation.Backward, goblinSprites, 6.0f, frameWidth, frameHeight, 15, 0, 1);


            Hands = new Grabber(Manager, "hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);

            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            AI = new CreatureAIComponent(this, "Goblin AI", Sensors, PlanService);

            Weapon = new Weapon("Sword", 1.0f, 2.0f, 1.0f, AI, ContentPaths.Audio.sword);

            Health = new HealthComponent(Manager, "Health", Physics, Stats.MaxHealth, 0.0f, Stats.MaxHealth);


            Inventory = new Inventory(Manager, "Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 16
                }
            };

            Matrix shadowTransform = Matrix.CreateRotationX((float) Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

            Texture2D shadowTexture = TextureManager.GetTexture(ContentPaths.Effects.shadowcircle);

            Shadow = new ShadowComponent(Manager, "Shadow", Physics, shadowTransform, shadowTexture);
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(Graphics, shadowTexture, "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");
            Physics.Tags.Add("Goblin");

            DeathEmitter = new EmitterComponent("blood_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 100
            };
            Flames = new FlammableComponent(Manager, "Flames", Physics, Health);


            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Entities.Goblin.Audio.goblinhurt1,
                ContentPaths.Entities.Goblin.Audio.goblinhurt2,
                ContentPaths.Entities.Goblin.Audio.goblinhurt3,
                ContentPaths.Entities.Goblin.Audio.goblinhurt4,
            };


            MinimapIcon minimapIcon = new MinimapIcon(Physics, new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.map_icons), 16, 3, 0));



            NoiseMaker.Noises["Chew"] = new List<string>
            {
                ContentPaths.Audio.chew
            };

            NoiseMaker.Noises["Jump"] = new List<string>
            {
                ContentPaths.Audio.jump
            };

        }
    }

}