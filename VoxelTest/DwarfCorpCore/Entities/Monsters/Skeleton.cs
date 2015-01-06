using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{

    /// <summary>
    /// Convenience class for initializing Skeletons as creatures.
    /// </summary>
    public class Skeleton : Creature
    {
        public Skeleton(CreatureStats stats, string allies, PlanService planService, Faction faction, ComponentManager manager, string name, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, Vector3 position) :
            base(stats, allies, planService, faction, new Physics("Skeleton", manager.RootComponent, Matrix.CreateTranslation(position), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0)),
                 chunks, graphics, content, name)
        {
            Initialize();
        }

        public void Initialize()
        {
            Physics.Orientation = Physics.OrientMode.RotateY;
            Sprite = new CharacterSprite(Graphics, Manager, "Skeleton Sprite", Physics, Matrix.CreateTranslation(new Vector3(0, 0.1f, 0)));
            foreach (Animation animation in Stats.CurrentClass.Animations)
            {
                Sprite.AddAnimation(animation.Clone());
            }



            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);

            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            AI = new CreatureAI(this, "Skeleton AI", Sensors, PlanService);

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.MeleeAttack) };


            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 16
                }
            };

            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

            Texture2D shadowTexture = TextureManager.GetTexture(ContentPaths.Effects.shadowcircle);

            Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform, shadowTexture);
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(Graphics, shadowTexture, "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");
            Physics.Tags.Add("Skeleton");

            DeathParticleTrigger = new ParticleTrigger("sand_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 25,
                SoundToPlay = ContentPaths.Audio.gravel
            };
            Flames = new Flammable(Manager, "Flames", Physics, this);


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

            Stats.FirstName = TextGenerator.GenerateRandom("$GoblinName");
            Stats.LastName = TextGenerator.GenerateRandom("$GoblinFamily");
            Stats.Size = 3;

        }
    }

}