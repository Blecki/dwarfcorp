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
    public class DeerAI : CreatureAI
    {
        public DeerAI()
        {

        }

        public DeerAI(Creature creature, string name, EnemySensor sensor, PlanService planService) :
            base(creature, name, sensor, planService)
        {

        }


        public IEnumerable<Act.Status> GoToRandomTarget(float radius)
        {
            Vector3 randomVector = MathFunctions.RandVector3Cube()*radius;
            Vector3 target = Physics.ClampToBounds(Position + randomVector);
            float dist = (target - Position).Length();
            Timer timout = new Timer(3.0f, false);
            while (dist > 3.0f)
            {
                timout.Update(Act.LastTime);
                if (timout.HasTriggered)
                {
                    break;
                }
                Vector3 output = Creature.Controller.GetOutput(Act.Dt, target, Position);
                output.Normalize();
                output *= 10;
                Physics.ApplyForce(new Vector3(output.X, 0.0f, output.Z), Act.Dt);
                dist = (target - Position).Length();
                yield return Act.Status.Running;
            }
            yield return Act.Status.Success;
        }

    // Overrides the default ActOnIdle so we can
        // have the bird act in any way we wish.
        public override Task ActOnIdle()
        {
            return
                new ActWrapperTask(new Sequence(new Wrap(() => GoToRandomTarget(10.0f)), new StopAct(this), new Wait(5.0f)));
        }
    }

    [JsonObject(IsReference = true)]
    public class Deer : Creature
    {
        private float ANIM_SPEED = 5.0f;

        public Deer()
        {
            
        }

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
                new Physics
                (
                    "A Deer",
                    manager.RootComponent,
                    Matrix.CreateTranslation(position),
                    new Vector3(0.3f, 0.3f, 0.3f),
                    new Vector3(0, 0, 0),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                ),
                chunks, graphics, content, name
            )
        {
            Initialize(new SpriteSheet(sprites));
        }


        public void Initialize(SpriteSheet spriteSheet)
        {
            Physics.Orientation = Physics.OrientMode.RotateY;

            const int frameWidth = 48;
            const int frameHeight = 40;

            Sprite = new CharacterSprite
                (Graphics,
                Manager,
                "Deer Sprite",
                Physics,
                Matrix.CreateTranslation(Vector3.Up * 0.6f)
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

            // Add hands
            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);

            // Add sensor
            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            // Add AI
            AI = new DeerAI(this, "Deer AI", Sensors, PlanService);

            Attacks = new List<Attack>{new Attack("None", 0.0f, 0.0f, 0.0f, ContentPaths.Audio.pick, "Herbivores")};

            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 1
                }
            };

            // Shadow
            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -.25f, 0.0f);
            SpriteSheet shadowTexture = new SpriteSheet(ContentPaths.Effects.shadowcircle);
            Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform, shadowTexture);

            List<Point> shP = new List<Point>
            {
                new Point(0,0)
            };
            Animation shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle), "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");

            // The bird will emit a shower of blood when it dies
            DeathParticleTrigger = new ParticleTrigger("blood_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                BoxTriggerTimes = 10
            };

            // The bird is flammable, and can die when exposed to fire.
            Flames = new Flammable(Manager, "Flames", Physics, this);

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Deer");
            Physics.Tags.Add("Animal");

            Stats.FirstName = TextGenerator.GenerateRandom("$DwarfName");
            Stats.LastName = " the Deer";
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Deer",
                Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Deer"} }
            };

        }

    }
}
