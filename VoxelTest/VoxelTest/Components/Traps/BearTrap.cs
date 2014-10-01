using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class BearTrap : Body
    {
        public Sensor Sensor { get; set; }
        public Sprite Sprite { get; set; }
        public VoxelListener VoxListener { get; set; }
        public ParticleTrigger DeathParticles { get; set; }
        public float DamageAmount { get; set; }
        public static string IdleAnimation = "Idle";
        public static string TriggerAnimation = "Trigger";
        public string Allies { get; set; }
        public bool ShouldDie = false;
        public Timer DeathTimer { get; set; }

        public BearTrap()
        {
            
        }

        public BearTrap(Vector3 pos) :
            base(
            "BearTrap", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(pos),
            new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, true)
        {
            Allies = "Dwarf";
            Sensor = new Sensor("Sensor", this, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero)
            {
                FireTimer = new Timer(0.5f, false)
            };
            Sensor.OnSensed += Sensor_OnSensed;
            DeathTimer = new Timer(0.6f, true);
            DeathParticles = new ParticleTrigger("puff", PlayState.ComponentManager, "DeathParticles", this, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero);
            DamageAmount = 200;
            VoxListener = new VoxelListener(PlayState.ComponentManager, this, PlayState.ChunkManager, PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(pos));
            Sprite = new Sprite(PlayState.ComponentManager, "Sprite", this, Matrix.Identity, TextureManager.GetTexture(ContentPaths.Entities.DwarfObjects.beartrap), false);
            Sprite.AddAnimation(new Animation(0, ContentPaths.Entities.DwarfObjects.beartrap, 32, 32,  0) {Name = IdleAnimation});
            Sprite.AddAnimation(new Animation(1, ContentPaths.Entities.DwarfObjects.beartrap, 32, 32,  0, 1, 2, 3) {Name = TriggerAnimation, FrameHZ = 6.6f, Loops = true});

        }

        void Sensor_OnSensed(List<Body> sensed)
        {
            if (ShouldDie)
            {
                return;
            }

            foreach (Body body in sensed)
            {
                CreatureAI creature = body.GetChildrenOfTypeRecursive<CreatureAI>().FirstOrDefault();

                if (creature == null) continue;
                if (Alliance.GetRelationship(creature.Creature.Allies, Allies) == Relationship.Loves) continue;

                creature.Creature.Health.Damage(DamageAmount);
                creature.Creature.Physics.Velocity *= 0.0f;
                Trigger();
                break;
            }
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {

            if (ShouldDie)
            {
                DeathTimer.Update(gameTime);

                if (DeathTimer.HasTriggered)
                {
                    Die();
                }
            }
            base.Update(gameTime, chunks, camera);
        }

        public void Trigger()
        {
            Sprite.SetCurrentAnimation(TriggerAnimation);
            Sprite.CurrentAnimation.Play();
          
            ShouldDie = true;
            DeathTimer.Reset(DeathTimer.TargetTimeSeconds);
        }


    }
}
