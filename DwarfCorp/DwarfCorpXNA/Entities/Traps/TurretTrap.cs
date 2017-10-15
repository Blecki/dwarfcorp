using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class TurretTrap : Body, IUpdateableComponent
    {
        public Attack Weapon { get; set; }
        public Fixture BaseSprite { get; set; }
        public Fixture TurretSprite { get; set; }
        public Faction Allies { get; set; }
        public EnemySensor Sensor { get; set; }
        private CreatureAI closestCreature = null;
        private Vector3 offset = Vector3.Zero;

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            Sensor.OnEnemySensed += Sensor_OnEnemySensed;
        }

        public TurretTrap()
        {
            
        }

        public TurretTrap(ComponentManager manager, Vector3 position, Faction faction) :
            base(manager, "TurretTrap", Matrix.CreateTranslation(position),
            new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, true)
        {
            Allies = faction;
            SpriteSheet spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32);
            Weapon = new Attack("BowAttack", 5.0f, 1.0f, 5.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_trap_turret_shoot_1, ContentPaths.Audio.Oscar.sfx_trap_turret_shoot_2), ContentPaths.Effects.pierce)
            {
                ProjectileType = "Arrow",
                Mode = Attack.AttackMode.Ranged,
                LaunchSpeed = 15
            };

            AddChild(new ParticleTrigger("explode", Manager, "DeathParticles",
            Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero)
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_trap_turret_shoot_1
            });

            AddChild(new Health(Manager, "health", 50.0f, 0.0f, 50.0f));

            Sensor = AddChild(new EnemySensor(Manager, "sensor", Matrix.Identity, new Vector3(8, 8, 8),
                Vector3.Zero)
            {
                Allies = faction
            }) as EnemySensor;

            Sensor.OnEnemySensed += Sensor_OnEnemySensed;
            BaseSprite = AddChild(new Fixture(Manager, Vector3.Zero, spriteSheet, new Point(2, 7))) as Fixture;
            BaseSprite.OrientMode = SimpleSprite.OrientMode.YAxis;
            TurretSprite = AddChild(new Fixture(Manager, Vector3.Up * 0.25f, spriteSheet, new Point(1, 7), SimpleSprite.OrientMode.Fixed)) as Fixture;
            SetTurretAngle(0.0f);
            CreateCosmeticChildren(manager);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            AddChild(new Shadow(manager));
            base.CreateCosmeticChildren(manager);
        }

        void Sensor_OnEnemySensed(List<CreatureAI> enemies)
        {
            closestCreature = null;
            float minDist = float.MaxValue;
            foreach (CreatureAI enemy in enemies)
            {
                float dist = (enemy.Position - Position).LengthSquared();

                if (dist < minDist)
                {
                    offset = enemy.Position - Position;
                    minDist = dist;
                    closestCreature = enemy;
                }
            }
        }

        public void SetTurretAngle(float radians)
        {
            Matrix turretTransform = Matrix.CreateRotationX(1.57f) * Matrix.CreateRotationY((radians));
            turretTransform.Translation = Vector3.Up * 0.25f;
            TurretSprite.LocalTransform = turretTransform;
            PropogateTransforms();
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (closestCreature != null && !closestCreature.IsDead)
            {
                
                Weapon.RechargeTimer.Update(gameTime);

                SetTurretAngle((float) Math.Atan2(offset.X, offset.Z) - (float)Math.PI * 0.5f);

                if (Weapon.RechargeTimer.HasTriggered)
                {
                    closestCreature.Kill(this);
                    Weapon.LaunchProjectile(Position + Vector3.Up * 0.5f, closestCreature.Position, closestCreature.Physics);
                    Weapon.PlayNoise(Position);
                    Weapon.RechargeTimer.Reset();
                }
            }
            base.Update(gameTime, chunks, camera);
        }
    }
}
