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
    /// <summary>
    /// A weapon allows one entity to attack another. (This is assumed to be a melee weapon). A weapon just has a damage
    /// amount, a range, and a hit noise.
    /// </summary>
    [JsonObject(IsReference =  true)]
    public class Attack
    {
        public enum AttackMode
        {
            Melee,
            Ranged,
            Area,
            Dogfight
        }

        public enum AttackTrigger
        {
            Timer,
            Animation
        }

        public float DamageAmount { get; set; }
        [JsonIgnore]
        public float RechargeRate { get
        {
            if (RechargeTimer != null) return RechargeTimer.TargetTimeSeconds;
            else return 0.0f;
        } }
        public string Name { get; set; }
        public float Range { get; set; }
        public SoundSource HitNoise { get; set; }
        public Color HitColor { get; set; }
        public AttackMode Mode { get; set; }
        public Timer RechargeTimer { get; set; }
        public float Knockback { get; set; }
        public string AnimationAsset { get; set; }

        [JsonIgnore] private Animation _hitAnimation = null;
        [JsonIgnore]
        protected Animation HitAnimation
        {
            get
            {
                if (_hitAnimation == null)
                    _hitAnimation = AnimationLibrary.CreateSimpleAnimation(AnimationAsset);
                return _hitAnimation;
            }
        }

        public string HitParticles { get; set; }
        public string ProjectileType { get; set; }
        public float LaunchSpeed { get; set; }
        public bool HasTriggered { get; set; }
        public AttackTrigger TriggerMode { get; set; }
        public int TriggerFrame { get; set; }

        public string DiseaseToSpread { get; set; }

        public bool ShootLaser { get; set; }

        private void GetHitAnimation()
        {

        }

        public Attack()
        {
            
        }

        public Attack(Attack other)
        {
            Name = other.Name;
            DamageAmount = other.DamageAmount;
            Range = other.Range;
            HitNoise = other.HitNoise;
            RechargeTimer = new Timer(other.RechargeRate + MathFunctions.Rand(-0.2f, 0.2f), false);
            Mode = other.Mode;
            Knockback = other.Knockback;
            HitParticles = other.HitParticles;
            HitColor = other.HitColor;
            ProjectileType = other.ProjectileType;
            LaunchSpeed = other.LaunchSpeed;
            AnimationAsset = other.AnimationAsset;
            TriggerMode = other.TriggerMode;
            TriggerFrame = other.TriggerFrame;
            HasTriggered = false;
            DiseaseToSpread = other.DiseaseToSpread;
            ShootLaser = other.ShootLaser;
        }

        public Attack(string name, float damage, float time, float range, SoundSource noise, string animation)
        {
            Name = name;
            DamageAmount = damage;
            RechargeTimer = new Timer(time + MathFunctions.Rand(-0.2f, 0.2f), false);
            Range = range;
            HitNoise = noise;
            Mode = AttackMode.Melee;
            Knockback = 0.0f;
            HitColor = Color.White;
            HitParticles = "";
            ProjectileType = "";
            AnimationAsset = animation;
            ShootLaser = false;
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

                switch (TriggerMode)
                {
                    case AttackTrigger.Timer:
                        RechargeTimer.Update(time);
                        if (!RechargeTimer.HasTriggered)
                        {
                            yield return Act.Status.Running;
                            continue;
                        }
                        break;
                    case AttackTrigger.Animation:
                        if (!performer.Sprite.AnimPlayer.HasValidAnimation() ||
                            performer.Sprite.AnimPlayer.CurrentFrame < TriggerFrame)
                        {
                            if (performer.Sprite.AnimPlayer.HasValidAnimation())
                                performer.Sprite.AnimPlayer.Play();
                            yield return Act.Status.Running;
                            continue;
                        }
                        break;
                }

                //switch (Mode)
                //{
                    //case AttackMode.Melee:
                    //{

                // Okay - so the kill voxel act already makes sure the creature is right next to the voxel. Just... pretend the ranged attack isn't?
                        DigAct.VoxelHealth -= (DamageAmount + bonus);

                        DigAct.Voxel.Type.HitSound.Play(DigAct.Voxel.WorldPosition);
                        if (HitParticles != "")
                            performer.Manager.World.ParticleManager.Trigger(HitParticles, DigAct.Voxel.WorldPosition, Color.White, 5);

                        if (HitAnimation != null)
                            IndicatorManager.DrawIndicator(HitAnimation, DigAct.Voxel.WorldPosition + Vector3.One*0.5f,
                                10.0f, 1.0f, MathFunctions.RandVector2Circle()*10, HitColor, MathFunctions.Rand() > 0.5f);

                        //break;
                //    }
                //    case AttackMode.Ranged:
                //    {
                //        throw new InvalidOperationException("Ranged attacks should never be used for digging.");
                //        //LaunchProjectile(pos, DigAct.GetTargetVoxel().WorldPosition, null);
                //        //break;
                //    }

                //}
                yield return Act.Status.Success;
                yield break;
            }

        }

        public void LaunchProjectile(Vector3 start, Vector3 end, GameComponent target)
        {
            Vector3 velocity = (end - start);
            float dist = velocity.Length();
            float T = dist / LaunchSpeed;
            velocity = 1.0f/T*(end - start) - 0.5f*Vector3.Down*10*T;
            Blackboard data = new Blackboard();
            data.SetData("Velocity", velocity);
            data.SetData("Target", target);
            EntityFactory.CreateEntity<GameComponent>(ProjectileType, start, data);
        }

        public bool PerformNoDamage(Creature performer, DwarfTime time, Vector3 pos)
        {
            switch (TriggerMode)
            {
                case AttackTrigger.Timer:
                    RechargeTimer.Update(time);
                    if (!RechargeTimer.HasTriggered)
                    {
                        HasTriggered = false;
                        return false;
                    }
                    break;
                case AttackTrigger.Animation:
                    if (!performer.Sprite.AnimPlayer.HasValidAnimation() ||
                        performer.Sprite.AnimPlayer.CurrentFrame != TriggerFrame)
                    {
                        HasTriggered = false;
                        return false;
                    }
                    break;
            }
            if (Mode == AttackMode.Melee)
            {
                if (HitParticles != "")
                {
                    performer.Manager.World.ParticleManager.Trigger(HitParticles, pos, Color.White, 5);
                }


                if (HitAnimation != null && !HasTriggered)
                {
                    IndicatorManager.DrawIndicator(HitAnimation, pos, 10.0f, 1.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);
                    PlayNoise(pos);
                }
            }
            HasTriggered = true;
            return true;
        }

        public void DoDamage(Creature performer, GameComponent other, float bonus)
        {

            if (!String.IsNullOrEmpty(DiseaseToSpread))
            {
                var otherCreature = other.GetRoot().GetComponent<Creature>();
                if (otherCreature != null)
                {
                    var disease = DiseaseLibrary.GetDisease(DiseaseToSpread);
                    if (MathFunctions.RandEvent(disease.LikelihoodOfSpread))
                        otherCreature.AcquireDisease(DiseaseToSpread);
                }
            }

            var health = other.GetRoot().EnumerateAll().OfType<Health>().FirstOrDefault();
            if (health != null)
            {
                health.Damage(DamageAmount + bonus);
                var injury = DiseaseLibrary.GetRandomInjury();

                if (MathFunctions.RandEvent(injury.LikelihoodOfSpread))
                {
                    var creature = other.GetRoot().GetComponent<Creature>();
                    if (creature != null)
                        creature.AcquireDisease(injury.Name);
                }

                Vector3 knock = other.Position - performer.Physics.Position;
                knock.Normalize();
                knock *= 0.2f;
                if (other.AnimationQueue.Count == 0)
                    other.AnimationQueue.Add(new KnockbackAnimation(0.15f, other.LocalTransform, knock));
            }
            else
            {
                other.GetRoot().Die();
            }

            PlayNoise(other.GlobalTransform.Translation);
            if (HitParticles != "")
            {
                performer.Manager.World.ParticleManager.Trigger(HitParticles, other.LocalTransform.Translation, Color.White, 5);

                if (ShootLaser)
                {
                    performer.Manager.World.ParticleManager.TriggerRay(HitParticles, performer.AI.Position, other.LocalTransform.Translation);
                }
            }

            if (HitAnimation != null)
            {
                IndicatorManager.DrawIndicator(HitAnimation, other.BoundingBox.Center(), 10.0f, 1.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);
            }

            Physics physics = other as Physics;

            if (physics != null)
            {
                Vector3 force = other.Position - performer.AI.Position;

                if (force.LengthSquared() > 0.01f)
                {
                    force.Normalize();
                    physics.ApplyForce(force * Knockback, 1.0f);
                }
            }
        }

        public bool Perform(Creature performer, GameComponent other, DwarfTime time, float bonus, Vector3 pos, string faction)
        {

            switch (TriggerMode)
            {
                case AttackTrigger.Timer:
                    RechargeTimer.Update(time);
                    if (!RechargeTimer.HasTriggered)
                    {
                        HasTriggered = false;
                        return false;
                    }
                    break;
                case AttackTrigger.Animation:
                    if (!performer.Sprite.AnimPlayer.HasValidAnimation() ||
                        performer.Sprite.AnimPlayer.CurrentFrame != TriggerFrame)
                    {
                        HasTriggered = false;
                        return false;
                    }
                    break;
            }

            if (HasTriggered)
            {
                return true;
            }

            HasTriggered = true;
            switch (Mode)
            {
                case AttackMode.Melee:
                case AttackMode.Dogfight:
                {
                        DoDamage(performer, other, bonus);
                        break;
                }
                case AttackMode.Area:
                {
                        BoundingBox box = new BoundingBox(performer.AI.Position - Vector3.One * Range, performer.AI.Position + Vector3.One * Range);
                        foreach(var body in performer.World.EnumerateIntersectingObjects(box, CollisionType.Both))
                        {
                            var creature = body.GetRoot().GetComponent<CreatureAI>();
                            if (creature == null)
                            {
                                var health = body.GetRoot().GetComponent<Health>();
                                if (health != null)
                                {
                                    DoDamage(performer, body, bonus);
                                }
                                continue;
                            }
                            if (creature.Faction == performer.Faction)
                                continue;
                            var alliance = performer.World.Diplomacy.GetPolitics(creature.Faction, performer.Faction).GetCurrentRelationship() != Relationship.Hateful;
                            if (alliance)
                            {
                                continue;
                            }

                            DoDamage(performer, body, bonus);
                        }
                        break;
                }
                case AttackMode.Ranged:
                {
                    PlayNoise(other.GlobalTransform.Translation);
                    LaunchProjectile(pos, other.Position, other);

                    var injury = DiseaseLibrary.GetRandomInjury();

                    if (MathFunctions.RandEvent(injury.LikelihoodOfSpread))
                    {
                        var creature = other.GetRoot().GetComponent<Creature>();
                        if (creature != null)
                            creature.AcquireDisease(injury.Name);
                    }
                    break;
                }
            }

            return true;
        }

        public void PlayNoise(Vector3 position)
        {
            HitNoise.Play(position);
        }
    }

}