using System;
using System.Collections.Generic;
using System.Linq;
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
            Area
        }

        public float DamageAmount { get; set; }
        public float RechargeRate { get
        {
            if (RechargeTimer != null) return RechargeTimer.TargetTimeSeconds;
            else return 0.0f;
        } }
        public string Name { get; set; }
        public float Range { get; set; }
        public string HitNoise { get; set; }
        public AttackMode Mode { get; set; }
        public Timer RechargeTimer { get; set; }
        public float Knockback { get; set; }
        public Animation HitAnimation { get; set; }
        public string HitParticles { get; set; }
        public string ProjectileType { get; set; }
        public float LaunchSpeed { get; set; }
        public string Faction { get; set; }

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
            HitAnimation = other.HitAnimation;
            HitParticles = other.HitParticles;
            ProjectileType = other.ProjectileType;
            LaunchSpeed = other.LaunchSpeed;
            Faction = other.Faction;
        }

        public Attack(string name, float damage, float time, float range, string noise, string faction)
        {
            Name = name;
            DamageAmount = damage;
            RechargeTimer = new Timer(time + MathFunctions.Rand(-0.2f, 0.2f), false);
            Range = range;
            HitNoise = noise;
            Mode = AttackMode.Melee;
            Knockback = 0.0f;
            HitAnimation = null;
            HitParticles = "";
            ProjectileType = "";
            Faction = faction;
        }

        public bool Perform(Vector3 pos, Voxel other, DwarfTime time, float bonus)
        {
            if (other == null)
            {
                return false;
            }

            RechargeTimer.Update(time);

            if (RechargeTimer.HasTriggered)
            {
                switch (Mode)
                {
                    case AttackMode.Melee:
                    {
                        other.Health -= DamageAmount + bonus;
                        PlayNoise(other.Position);
                        if (HitParticles != "")
                        {
                            PlayState.ParticleManager.Trigger(HitParticles, other.Position, Color.White, 5);
                        }

                        if (HitAnimation != null)
                        {
                            HitAnimation.Reset();
                            HitAnimation.Play();
                            IndicatorManager.DrawIndicator(HitAnimation, other.Position + Vector3.One * 0.5f, 0.6f, 2.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);
                        }
                        break;
                    }
                    case AttackMode.Ranged:
                    {
                        LaunchProjectile(pos, other.Position);
                        break;
                    }
                    
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public void LaunchProjectile(Vector3 start, Vector3 end)
        {
            Vector3 velocity = (end - start);
            float dist = velocity.Length();
            float T = dist / LaunchSpeed;
            velocity = 1.0f/T*(end - start) - 0.5f*Vector3.Down*10*T;
            Blackboard data = new Blackboard();
            data.SetData("Velocity", velocity);
            data.SetData("Faction", Faction);
            EntityFactory.CreateEntity<Body>(ProjectileType, start, data);
        }

        public void PerformNoDamage(DwarfTime time, Vector3 pos)
        {
            RechargeTimer.Update(time);

            if (RechargeTimer.HasTriggered)
            {
                if (Mode == AttackMode.Melee)
                {
                    PlayNoise(pos);
                    if (HitParticles != "")
                    {
                        PlayState.ParticleManager.Trigger(HitParticles, pos, Color.White, 5);
                    }


                    if (HitAnimation != null)
                    {
                        HitAnimation.Reset();
                        HitAnimation.Play();
                        IndicatorManager.DrawIndicator(HitAnimation, pos, 0.6f, 2.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);
                    }
                }
            }
        }

        public bool Perform(Body other, DwarfTime time, float bonus, Vector3 pos)
        {
            RechargeTimer.Update(time);

            if (RechargeTimer.HasTriggered)
            {
                switch (Mode)
                {
                    case AttackMode.Melee:
                    {
                        Health health = other.GetChildrenOfType<Health>().FirstOrDefault();
                        if (health != null)
                        {
                            health.Damage(DamageAmount + bonus);
                        }

                        PlayNoise(other.LocalTransform.Translation);
                        if (HitParticles != "")
                        {
                            PlayState.ParticleManager.Trigger(HitParticles, other.LocalTransform.Translation, Color.White, 5);
                        }

                        if (HitAnimation != null)
                        {
                            HitAnimation.Reset();
                            HitAnimation.Play();
                            IndicatorManager.DrawIndicator(HitAnimation, other.BoundingBox.Center(), 0.6f, 2.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);
                        }

                        Physics physics = other as Physics;

                        if (physics != null)
                        {
                            Vector3 force = other.Position - pos;
                            force.Normalize();
                            physics.ApplyForce(force * Knockback, 1.0f);
                        }
                        
                        break;
                    }
                    case AttackMode.Ranged:
                    {
                        PlayNoise(other.LocalTransform.Translation);
                        LaunchProjectile(pos, other.Position);
                        break;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public void PlayNoise(Vector3 position)
        {
            SoundManager.PlaySound(HitNoise, position, true);
        }
    }

}