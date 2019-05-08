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
    public class ActualActOfAttacking
    {
        public Timer RechargeTimer;
        public bool HasTriggered;
        public Attack Attack;

        public ActualActOfAttacking(Attack Attack)
        {
            this.Attack = Attack;
            RechargeTimer = new Timer(Attack.RechargeRate, false);
        }

        public IEnumerable<Act.Status> PerformOnVoxel(Creature performer, Vector3 pos, KillVoxelTask DigAct, DwarfTime time, float bonus, string faction)
        {
            while (true)
            {
                if (!DigAct.Voxel.IsValid)
                {
                    performer.AI.SetMessage("Failed to dig. Voxel was not valid.");
                    yield return Act.Status.Fail;
                    yield break;
                }

                Drawer2D.DrawLoadBar(performer.World.Camera, DigAct.Voxel.WorldPosition + Vector3.One * 0.5f, Color.White, Color.Black, 32, 1, (float)DigAct.VoxelHealth / DigAct.Voxel.Type.StartingHealth);

                switch (Attack.TriggerMode)
                {
                    case Attack.AttackTrigger.Timer:
                        RechargeTimer.Update(time);
                        if (!RechargeTimer.HasTriggered)
                        {
                            yield return Act.Status.Running;
                            continue;
                        }
                        break;
                    case Attack.AttackTrigger.Animation:
                        if (!performer.Sprite.AnimPlayer.HasValidAnimation() ||
                            performer.Sprite.AnimPlayer.CurrentFrame < Attack.TriggerFrame)
                        {
                            if (performer.Sprite.AnimPlayer.HasValidAnimation())
                                performer.Sprite.AnimPlayer.Play();
                            yield return Act.Status.Running;
                            continue;
                        }
                        break;
                }

                DigAct.VoxelHealth -= (Attack.DamageAmount + bonus);

                DigAct.Voxel.Type.HitSound.Play(DigAct.Voxel.WorldPosition);
                if (!String.IsNullOrEmpty(Attack.HitParticles))
                    performer.Manager.World.ParticleManager.Trigger(Attack.HitParticles, DigAct.Voxel.WorldPosition, Color.White, 5);

                if (Attack.HitAnimation != null)
                    IndicatorManager.DrawIndicator(Attack.HitAnimation, DigAct.Voxel.WorldPosition + Vector3.One * 0.5f,
                        10.0f, 1.0f, MathFunctions.RandVector2Circle() * 10, Attack.HitColor, MathFunctions.Rand() > 0.5f);

                yield return Act.Status.Success;
                yield break;
            }
        }

        public void LaunchProjectile(Vector3 start, Vector3 end, GameComponent target)
        {
            Vector3 velocity = (end - start);
            float dist = velocity.Length();
            float T = dist / Attack.LaunchSpeed;
            velocity = 1.0f/T*(end - start) - 0.5f*Vector3.Down*10*T;
            Blackboard data = new Blackboard();
            data.SetData("Velocity", velocity);
            data.SetData("Target", target);
            EntityFactory.CreateEntity<GameComponent>(Attack.ProjectileType, start, data);
        }

        public bool PerformNoDamage(Creature performer, DwarfTime time, Vector3 pos)
        {
            switch (Attack.TriggerMode)
            {
                case Attack.AttackTrigger.Timer:
                    RechargeTimer.Update(time);
                    if (!RechargeTimer.HasTriggered)
                    {
                        HasTriggered = false;
                        return false;
                    }
                    break;
                case Attack.AttackTrigger.Animation:
                    if (!performer.Sprite.AnimPlayer.HasValidAnimation() ||
                        performer.Sprite.AnimPlayer.CurrentFrame != Attack.TriggerFrame)
                    {
                        HasTriggered = false;
                        return false;
                    }
                    break;
            }
            if (Attack.Mode == Attack.AttackMode.Melee)
            {
                if (Attack.HitParticles != "")
                    performer.Manager.World.ParticleManager.Trigger(Attack.HitParticles, pos, Color.White, 5);

                if (Attack.HitAnimation != null && !HasTriggered)
                {
                    IndicatorManager.DrawIndicator(Attack.HitAnimation, pos, 10.0f, 1.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);
                    PlayNoise(pos);
                }
            }
            HasTriggered = true;
            return true;
        }

        public void DoDamage(Creature performer, GameComponent other, float bonus)
        {

            if (!String.IsNullOrEmpty(Attack.DiseaseToSpread))
            {
                var otherCreature = other.GetRoot().GetComponent<Creature>();
                if (otherCreature != null)
                {
                    var disease = DiseaseLibrary.GetDisease(Attack.DiseaseToSpread);
                    if (disease != null)
                        if (MathFunctions.RandEvent(disease.LikelihoodOfSpread))
                            otherCreature.Stats.AcquireDisease(disease);
                }
            }

            var health = other.GetRoot().EnumerateAll().OfType<Health>().FirstOrDefault();
            if (health != null)
            {
                health.Damage(Attack.DamageAmount + bonus);
                var injury = DiseaseLibrary.GetRandomInjury();

                if (MathFunctions.RandEvent(injury.LikelihoodOfSpread))
                {
                    var creature = other.GetRoot().GetComponent<Creature>();
                    if (creature != null)
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
            if (Attack.HitParticles != "")
            {
                performer.Manager.World.ParticleManager.Trigger(Attack.HitParticles, other.LocalTransform.Translation, Color.White, 5);

                if (Attack.ShootLaser)
                {
                    performer.Manager.World.ParticleManager.TriggerRay(Attack.HitParticles, performer.AI.Position, other.LocalTransform.Translation);
                }
            }

            if (Attack.HitAnimation != null)
                IndicatorManager.DrawIndicator(Attack.HitAnimation, other.BoundingBox.Center(), 10.0f, 1.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);

            Physics physics = other as Physics;

            if (physics != null)
            {
                Vector3 force = other.Position - performer.AI.Position;

                if (force.LengthSquared() > 0.01f)
                {
                    force.Normalize();
                    physics.ApplyForce(force * Attack.Knockback, 1.0f);
                }
            }
        }

        public bool Perform(Creature performer, GameComponent other, DwarfTime time, float bonus, Vector3 pos, string faction)
        {

            switch (Attack.TriggerMode)
            {
                case Attack.AttackTrigger.Timer:
                    RechargeTimer.Update(time);
                    if (!RechargeTimer.HasTriggered)
                    {
                        HasTriggered = false;
                        return false;
                    }
                    break;
                case Attack.AttackTrigger.Animation:
                    if (!performer.Sprite.AnimPlayer.HasValidAnimation() ||
                        performer.Sprite.AnimPlayer.CurrentFrame != Attack.TriggerFrame)
                    {
                        HasTriggered = false;
                        return false;
                    }
                    break;
            }

            if (HasTriggered)
                return true;

            HasTriggered = true;
            switch (Attack.Mode)
            {
                case Attack.AttackMode.Melee:
                case Attack.AttackMode.Dogfight:
                {
                        DoDamage(performer, other, bonus);
                        break;
                }
                case Attack.AttackMode.Area:
                {
                        BoundingBox box = new BoundingBox(performer.AI.Position - Vector3.One * Attack.Range, performer.AI.Position + Vector3.One * Attack.Range);
                        foreach(var body in performer.World.EnumerateIntersectingObjects(box, CollisionType.Both).Where(b => b.IsRoot()))
                        {
                            var creature = body.GetRoot().GetComponent<CreatureAI>();

                            if (creature == null)
                            {
                                var health = body.GetRoot().GetComponent<Health>();
                                if (health != null)
                                    DoDamage(performer, body, bonus);
                                continue;
                            }

                            if (creature.Faction == performer.Faction)
                                continue;
                            var alliance = performer.World.Diplomacy.GetPolitics(creature.Faction, performer.Faction).GetCurrentRelationship() != Relationship.Hateful;
                            if (alliance)
                                continue;

                            DoDamage(performer, body, bonus);
                        }
                        break;
                }
                case Attack.AttackMode.Ranged:
                {
                    PlayNoise(other.GlobalTransform.Translation);
                    LaunchProjectile(pos, other.Position, other);

                    var injury = DiseaseLibrary.GetRandomInjury();

                    if (MathFunctions.RandEvent(injury.LikelihoodOfSpread))
                    {
                        var creature = other.GetRoot().GetComponent<Creature>();
                        if (creature != null)
                            creature.Stats.AcquireDisease(injury);
                    }
                    break;
                }
            }

            return true;
        }

        public void PlayNoise(Vector3 position)
        {
            Attack.HitNoise.Play(position);
        }
    }

}