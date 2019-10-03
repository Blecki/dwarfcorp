using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class BearTrap : CraftedBody
    {
        [EntityFactory("Bear Trap")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new BearTrap(Manager, Position, Data.GetData<Resource>("Resources", null));
        }

        public float DamageAmount { get; set; }
        public Faction Allies { get; set; }
        public bool ShouldDie = false;
        public Timer DeathTimer { get; set; }

        public BearTrap()
        {
            
        }

        public BearTrap(ComponentManager manager, Vector3 pos, Resource Resource) :
            base(manager,
            "BearTrap", Matrix.CreateTranslation(pos),
            new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(manager, Resource))
        {
            Allies = manager.World.PlayerFaction;
            
            DeathTimer = new Timer(0.6f, true);

            DamageAmount = 200;

            this.PropogateTransforms();
            CreateCosmeticChildren(manager);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            AddChild(new Shadow(manager));

            var spriteSheet = new SpriteSheet(ContentPaths.Entities.DwarfObjects.beartrap, 32);

            var sprite = AddChild(new AnimatedSprite(Manager, "Sprite", Matrix.Identity)) as AnimatedSprite;

            sprite.AddAnimation(Library.CreateAnimation(spriteSheet, new List<Point> { Point.Zero }, "BearTrapIdle"));

            var sprung = Library.CreateAnimation
                (spriteSheet, new List<Point>
                {
                    new Point(0,1),
                    new Point(1,1),
                    new Point(2,1),
                    new Point(3,1)
                }, "BearTrapTrigger");

            sprung.FrameHZ = 6.6f;

            sprite.AddAnimation(sprung);

            sprite.SetFlag(Flag.ShouldSerialize, false);
            sprite.SetCurrentAnimation("BearTrapIdle", false);

            AddChild(new GenericVoxelListener(manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);


            var sensor = AddChild(new Sensor(manager, "Sensor", Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero)
            {
                FireTimer = new Timer(0.5f, false, Timer.TimerMode.Real)
            }) as Sensor;
            sensor.OnSensed += Sensor_OnSensed;
            sensor.SetFlag(Flag.ShouldSerialize, false);

            AddChild(new ParticleTrigger("explode", Manager, "DeathParticles",
                Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero)
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_trap_destroyed
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }

        void Sensor_OnSensed(IEnumerable<GameComponent> sensed)
        {
            if (!Active)
                return;

            if (ShouldDie)
            {
                return;
            }

            foreach (GameComponent body in sensed)
            {
                var creature = body.EnumerateAll().OfType<CreatureAI>().FirstOrDefault();

                if (creature == null) continue;
                if (World.Overworld.GetPolitics(creature.Creature.Faction.ParentFaction, Allies.ParentFaction).GetCurrentRelationship() == Relationship.Loving) continue;

                creature.Creature.Damage(DamageAmount);
                creature.Creature.Physics.Velocity *= 0.0f;
                Trigger();
                break;
            }
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (ShouldDie)
            {
                DeathTimer.Update(gameTime);

                if (DeathTimer.HasTriggered)
                {
                    Die();
                }
            }
        }

        public void Trigger()
        {
            EnumerateChildren().OfType<AnimatedSprite>().FirstOrDefault().SetCurrentAnimation("BearTrapTrigger", true);
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_trap_trigger, GlobalTransform.Translation, false);
            ShouldDie = true;
            DeathTimer.Reset(DeathTimer.TargetTimeSeconds);
        }
    }
}
