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
                    performer.AI.SetMessage("Failed to dig. Voxel was not valid.");
                    yield return Act.Status.Fail;
                    yield break;
                }

                Drawer2D.DrawLoadBar(performer.World.Camera, DigAct.Voxel.WorldPosition + Vector3.One * 0.5f, Color.White, Color.Black, 32, 1, (float)DigAct.VoxelHealth / DigAct.Voxel.Type.StartingHealth);

                switch (Weapon.TriggerMode)
                {
                    case Weapon.AttackTrigger.Timer:
                        RechargeTimer.Update(time);
                        if (!RechargeTimer.HasTriggered)
                        {
                            yield return Act.Status.Running;
                            continue;
                        }
                        break;
                    case Weapon.AttackTrigger.Animation:
                        if (!performer.Sprite.AnimPlayer.HasValidAnimation() ||
                            performer.Sprite.AnimPlayer.CurrentFrame < Weapon.TriggerFrame)
                        {
                            if (performer.Sprite.AnimPlayer.HasValidAnimation())
                                performer.Sprite.AnimPlayer.Play();
                            yield return Act.Status.Running;
                            continue;
                        }
                        break;
                }

                DigAct.VoxelHealth -= (Weapon.DamageAmount + bonus);

                DigAct.Voxel.Type.HitSound.Play(DigAct.Voxel.WorldPosition);
                if (!String.IsNullOrEmpty(Weapon.HitParticles))
                    performer.Manager.World.ParticleManager.Trigger(Weapon.HitParticles, DigAct.Voxel.WorldPosition, Color.White, 5);

                if (Weapon.HitAnimation != null)
                    IndicatorManager.DrawIndicator(Weapon.HitAnimation, DigAct.Voxel.WorldPosition + Vector3.One * 0.5f,
                        10.0f, 1.0f, MathFunctions.RandVector2Circle() * 10, Weapon.HitColor, MathFunctions.Rand() > 0.5f);

                yield return Act.Status.Success;
                yield break;
            }
        }

        public void LaunchProjectile(Vector3 start, Vector3 end, GameComponent target)
        {
            Vector3 velocity = (end - start);
            float dist = velocity.Length();
            float T = dist / Weapon.LaunchSpeed;
            velocity = 1.0f/T*(end - start) - 0.5f*Vector3.Down*10*T;
            Blackboard data = new Blackboard();
            data.SetData("Velocity", velocity);
            data.SetData("Target", target);
            EntityFactory.CreateEntity<GameComponent>(Weapon.ProjectileType, start, data);
        }

        public bool PerformNoDamage(Creature performer, DwarfTime time, Vector3 pos)
        {
            switch (Weapon.TriggerMode)
            {
                case Weapon.AttackTrigger.Timer:
                    RechargeTimer.Update(time);
                    if (!RechargeTimer.HasTriggered)
                    {
                        HasTriggered = false;
                        return false;
                    }
                    break;
                case Weapon.AttackTrigger.Animation:
                    if (!performer.Sprite.AnimPlayer.HasValidAnimation() ||
                        performer.Sprite.AnimPlayer.CurrentFrame != Weapon.TriggerFrame)
                    {
                        HasTriggered = false;
                        return false;
                    }
                    break;
            }
            if (Weapon.Mode == Weapon.AttackMode.Melee)
            {
                if (Weapon.HitParticles != "")
                    performer.Manager.World.ParticleManager.Trigger(Weapon.HitParticles, pos, Color.White, 5);

                if (Weapon.HitAnimation != null && !HasTriggered)
                {
                    IndicatorManager.DrawIndicator(Weapon.HitAnimation, pos, 10.0f, 1.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);
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
                var otherCreature = other.GetRoot().GetComponent<Creature>();
                if (otherCreature != null)
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
                health.Damage(Weapon.DamageAmount + bonus);
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
            if (Weapon.HitParticles != "")
            {
                performer.Manager.World.ParticleManager.Trigger(Weapon.HitParticles, other.LocalTransform.Translation, Color.White, 5);

                if (Weapon.ShootLaser)
                {
                    performer.Manager.World.ParticleManager.TriggerRay(Weapon.HitParticles, performer.AI.Position, other.LocalTransform.Translation);
                }
            }

            if (Weapon.HitAnimation != null)
                IndicatorManager.DrawIndicator(Weapon.HitAnimation, other.BoundingBox.Center(), 10.0f, 1.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);

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

            switch (Weapon.TriggerMode)
            {
                case Weapon.AttackTrigger.Timer:
                    RechargeTimer.Update(time);
                    if (!RechargeTimer.HasTriggered)
                    {
                        HasTriggered = false;
                        return false;
                    }
                    break;
                case Weapon.AttackTrigger.Animation:
                    if (!performer.Sprite.AnimPlayer.HasValidAnimation() ||
                        performer.Sprite.AnimPlayer.CurrentFrame != Weapon.TriggerFrame)
                    {
                        HasTriggered = false;
                        return false;
                    }
                    break;
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
                        BoundingBox box = new BoundingBox(performer.AI.Position - Vector3.One * Weapon.Range, performer.AI.Position + Vector3.One * Weapon.Range);
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
                case Weapon.AttackMode.Ranged:
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
            Weapon.HitNoise.Play(position);
        }
    }

}