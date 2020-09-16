using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Attack
    {
        public Timer RechargeTimer;
        public bool HasTriggered;
        public Weapon Weapon;

        public Attack(Weapon Weapon)
        {
            this.Weapon = Weapon;
            RechargeTimer = new Timer(Weapon.RechargeRate, false);
        }

        public IEnumerable<Act.Status> PerformOnVoxel(Creature performer, Vector3 pos, KillVoxelTask DigAct, DwarfTime time, float bonus, string faction)
        {
            while (true)
            {
                if (!DigAct.Voxel.IsValid)
                {
                    performer.AI.SetTaskFailureReason("Failed to dig. Voxel was not valid.");
                    yield return Act.Status.Fail;
                    yield break;
                }

                Drawer2D.DrawLoadBar(performer.World.Renderer.Camera, DigAct.Voxel.WorldPosition + Vector3.One * 0.5f, Color.White, Color.Black, 32, 1, (float)DigAct.VoxelHealth / DigAct.Voxel.Type.StartingHealth);

                while (!performer.Sprite.HasValidAnimation() ||
                    performer.Sprite.GetCurrentFrame() < Weapon.TriggerFrame)
                {
                    if (performer.Sprite.HasValidAnimation())
                        performer.Sprite.PlayAnimations();
                    yield return Act.Status.Running;
                }

                DigAct.VoxelHealth -= (Weapon.DamageAmount + bonus);

                DigAct.Voxel.Type.HitSound.Play(DigAct.Voxel.WorldPosition);
                if (!String.IsNullOrEmpty(Weapon.HitParticles))
                    performer.Manager.World.ParticleManager.Trigger(Weapon.HitParticles, DigAct.Voxel.WorldPosition, Color.White, 5);

                if (Weapon.HitAnimation != null)
                    IndicatorManager.DrawIndicator(Weapon.HitAnimation.SpriteSheet, Weapon.HitAnimation.Animation, DigAct.Voxel.WorldPosition + Vector3.One * 0.5f,
                        10.0f, 1.0f, MathFunctions.RandVector2Circle() * 10, Weapon.HitColor, MathFunctions.Rand() > 0.5f);

                yield return Act.Status.Success;
                yield break;
            }
        }

        public void LaunchProjectile(Creature Performer, Vector3 start, Vector3 end, GameComponent target)
        {
            Vector3 velocity = (end - start);
            float dist = velocity.Length();
            float T = dist / Weapon.LaunchSpeed;
            velocity = 1.0f / T * (end - start) - 0.5f * Vector3.Down * 10 * T;
            Blackboard data = new Blackboard();
            data.SetData("Velocity", velocity);
            data.SetData("Target", target);
            data.SetData("Shooter", Performer);
            EntityFactory.CreateEntity<GameComponent>(Weapon.ProjectileType, start, data);
        }

        public bool PerformNoDamage(Creature performer, DwarfTime time, Vector3 pos)
        {
            if (!performer.Sprite.HasValidAnimation() ||
                performer.Sprite.GetCurrentFrame() != Weapon.TriggerFrame)
            {
                HasTriggered = false;
                return false;
            }

            if (Weapon.Mode == Weapon.AttackMode.Melee)
            {
                if (Weapon.HitParticles != "")
                    performer.Manager.World.ParticleManager.Trigger(Weapon.HitParticles, pos, Color.White, 5);

                if (Weapon.HitAnimation != null && !HasTriggered)
                {
                    IndicatorManager.DrawIndicator(Weapon.HitAnimation.SpriteSheet, Weapon.HitAnimation.Animation, pos, 10.0f, 1.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);
                    PlayNoise(pos);
                }
            }
            HasTriggered = true;
            return true;
        }

        public void DoDamage(Creature performer, GameComponent other, float bonus)
        {

            if (!String.IsNullOrEmpty(Weapon.DiseaseToSpread))
            {
                if (other.GetRoot().GetComponent<Creature>().HasValue(out var otherCreature))
                {
                    var disease = DiseaseLibrary.GetDisease(Weapon.DiseaseToSpread);
                    if (disease != null)
                        if (MathFunctions.RandEvent(disease.LikelihoodOfSpread))
                            otherCreature.Stats.AcquireDisease(disease);
                }
            }

            var health = other.GetRoot().EnumerateAll().OfType<Health>().FirstOrDefault();
            if (health != null)
            {
                health.Damage(performer.AI.FrameDeltaTime, Weapon.DamageAmount + bonus);
                var injury = DiseaseLibrary.GetRandomInjury();

                if (MathFunctions.RandEvent(injury.LikelihoodOfSpread))
                {
                    if (other.GetRoot().GetComponent<Creature>().HasValue(out var creature))
                        creature.Stats.AcquireDisease(injury);
                }

                Vector3 knock = other.Position - performer.Physics.Position;
                knock.Normalize();
                knock *= 0.2f;
                if (other.AnimationQueue.Count == 0)
                    other.AnimationQueue.Add(new KnockbackAnimation(0.15f, other.LocalTransform, knock));
            }
            else
                other.GetRoot().Die();

            PlayNoise(other.GlobalTransform.Translation);
            if (Weapon.HitParticles != "")
            {
                performer.Manager.World.ParticleManager.Trigger(Weapon.HitParticles, other.LocalTransform.Translation, Color.White, 5);

                if (Weapon.ShootLaser)
                {
                    performer.Manager.World.ParticleManager.TriggerRay(Weapon.HitParticles, performer.AI.Position, other.LocalTransform.Translation);
                }
            }

            if (Weapon.HitAnimation != null)
                IndicatorManager.DrawIndicator(Weapon.HitAnimation.SpriteSheet, Weapon.HitAnimation.Animation, other.BoundingBox.Center(), 10.0f, 1.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);

            Physics physics = other as Physics;

            if (physics != null)
            {
                Vector3 force = other.Position - performer.AI.Position;

                if (force.LengthSquared() > 0.01f)
                {
                    force.Normalize();
                    physics.ApplyForce(force * Weapon.Knockback, 1.0f);
                }
            }
        }

        public bool Perform(Creature performer, GameComponent other, DwarfTime time, float bonus, Vector3 pos, string faction)
        {

            if (!performer.Sprite.HasValidAnimation() ||
                performer.Sprite.GetCurrentFrame() != Weapon.TriggerFrame)
            {
                HasTriggered = false;
                return false;
            }

            if (HasTriggered)
                return true;

            HasTriggered = true;
            switch (Weapon.Mode)
            {
                case Weapon.AttackMode.Melee:
                case Weapon.AttackMode.Dogfight:
                    {
                        DoDamage(performer, other, bonus);
                        break;
                    }
                case Weapon.AttackMode.Area:
                    {
                        var box = new BoundingBox(performer.AI.Position - Vector3.One * Weapon.Range, performer.AI.Position + Vector3.One * Weapon.Range);

                        foreach (var body in performer.World.EnumerateIntersectingRootObjects(box, CollisionType.Both))
                        {
                            if (body.GetComponent<CreatureAI>().HasValue(out var creature))
                            {
                                if (creature.Faction == performer.Faction)
                                    continue;

                                
                                if (performer.World.Overworld.GetPolitics(creature.Faction.ParentFaction, performer.Faction.ParentFaction).GetCurrentRelationship() != Relationship.Hateful)
                                    continue;

                                DoDamage(performer, body, bonus);
                            }
                            else
                            {
                                if (body.GetComponent<Health>().HasValue(out var health))
                                    DoDamage(performer, body, bonus);

                                continue;
                            }
                        }
                        break;
                    }
                case Weapon.AttackMode.Ranged:
                    {
                        PlayNoise(other.GlobalTransform.Translation);
                        LaunchProjectile(performer, pos, other.Position, other);

                        var injury = DiseaseLibrary.GetRandomInjury();

                        if (MathFunctions.RandEvent(injury.LikelihoodOfSpread))
                            if (other.GetRoot().GetComponent<Creature>().HasValue(out var creature))
                                creature.Stats.AcquireDisease(injury);
                        break;
                    }
            }

            return true;
        }

        public void PlayNoise(Vector3 position)
        {
            Weapon.HitNoise.Play(position);
        }
    }
}