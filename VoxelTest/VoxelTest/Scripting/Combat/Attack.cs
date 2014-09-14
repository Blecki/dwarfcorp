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
        }

        public Attack(string name, float damage, float time, float range, string noise)
        {
            Name = name;
            DamageAmount = damage;
            RechargeTimer = new Timer(time + MathFunctions.Rand(-0.2f, 0.2f), false);
            Range = range;
            HitNoise = noise;
            Mode = AttackMode.Melee;
            Knockback = 0.0f;
            HitAnimation = null;
        }

        public bool Perform(Voxel other, GameTime time, float bonus)
        {
            if (other == null)
            {
                return false;
            }

            RechargeTimer.Update(time);

            if (RechargeTimer.HasTriggered)
            {
                other.Health -= DamageAmount + bonus;
                PlayNoise(other.Position);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Perform(GameComponent other, GameTime time, float bonus, Vector3 pos)
        {
            RechargeTimer.Update(time);

            if (RechargeTimer.HasTriggered)
            {
                Health health = other.GetChildrenOfType<Health>().FirstOrDefault();
                if (health != null)
                {
                    health.Damage(DamageAmount + bonus);
                }

                Body body = other as Body;

                if (body != null)
                {
                    PlayNoise(body.LocalTransform.Translation);

                    if (HitAnimation != null)
                    {
                        HitAnimation.Reset();
                        HitAnimation.Play();
                        IndicatorManager.DrawIndicator(HitAnimation, body.BoundingBox.Center(), 0.6f, 2.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);
                    }

                    Physics physics = other as Physics;

                    if (physics != null)
                    {
                        Vector3 force = body.LocalTransform.Translation - pos;
                        force.Normalize();
                        physics.ApplyForce(force * Knockback, 1.0f);
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