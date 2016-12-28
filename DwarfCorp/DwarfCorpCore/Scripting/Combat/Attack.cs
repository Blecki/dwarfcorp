// Attack.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     An attack defines how creatures damage other objects, creatures, or voxels.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Attack
    {
        /// <summary>
        ///     Melee attacks are performed when the creature is adjacent to its target.
        ///     Ranged attacks produce projectiles which damage the target.
        ///     Dogfight attacks cause damage to the target as the attacker passes through it.
        /// </summary>
        public enum AttackMode
        {
            Melee,
            Ranged,
            Dogfight
        }

        /// <summary>
        ///     Attacks are either triggered by a timer that counts down, or by an animation
        ///     displaying a specific frame.
        /// </summary>
        public enum AttackTrigger
        {
            Timer,
            Animation
        }

        public Attack()
        {
        }

        /// <summary>
        ///     Deep clone of the attack.
        /// </summary>
        /// <param name="other">The attack to clone.</param>
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
            HitColor = other.HitColor;
            ProjectileType = other.ProjectileType;
            LaunchSpeed = other.LaunchSpeed;
            AnimationAsset = other.AnimationAsset;
            TriggerMode = other.TriggerMode;
            TriggerFrame = other.TriggerFrame;
        }

        /// <summary>
        ///     Create an attack with the given parameters.
        /// </summary>
        /// <param name="name">Name of the attack for debugging</param>
        /// <param name="damage">Damage (in HP) to apply on success.</param>
        /// <param name="time">The time between attacks.</param>
        /// <param name="range">The range (in voxels) of the attack.</param>
        /// <param name="noise">The noise to play whenever the attack succeeds.</param>
        /// <param name="animation">The animation to play whenever the attack succeeds.</param>
        public Attack(string name, float damage, float time, float range, string noise, string animation)
        {
            Name = name;
            DamageAmount = damage;
            RechargeTimer = new Timer(time + MathFunctions.Rand(-0.2f, 0.2f), false);
            Range = range;
            HitNoise = noise;
            Mode = AttackMode.Melee;
            Knockback = 0.0f;
            HitAnimation = null;
            HitColor = Color.White;
            HitParticles = "";
            ProjectileType = "";
            AnimationAsset = animation;
            CreateHitAnimation();
        }

        /// <summary>
        ///     The amount (in HP) of damage that the attack does.
        /// </summary>
        public float DamageAmount { get; set; }

        /// <summary>
        ///     The number of seconds before the attack recharges its power.
        /// </summary>
        [JsonIgnore]
        public float RechargeRate
        {
            get
            {
                if (RechargeTimer != null) return RechargeTimer.TargetTimeSeconds;
                return 0.0f;
            }
        }

        /// <summary>
        ///     Name of the attack for debugging purposes.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The range (in voxels) in which the attack is effective.
        /// </summary>
        public float Range { get; set; }

        /// <summary>
        ///     Sound asset to play if the attack hits the target.
        /// </summary>
        public string HitNoise { get; set; }

        /// <summary>
        ///     The tint of the animation played whenever the attack hits.
        /// </summary>
        public Color HitColor { get; set; }

        /// <summary>
        ///     The style of attack.
        /// </summary>
        public AttackMode Mode { get; set; }

        /// <summary>
        ///     Cooldown timer between attacks.
        /// </summary>
        public Timer RechargeTimer { get; set; }

        /// <summary>
        ///     Force to apply to the target whenever the attack succeeds.
        /// </summary>
        public float Knockback { get; set; }

        /// <summary>
        ///     Animation to play whenever the attack succeeds.
        /// </summary>
        public string AnimationAsset { get; set; }

        /// <summary>
        ///     Animation to play whenever the attack succeeds (private for deserialization)
        /// </summary>
        [JsonIgnore]
        protected Animation HitAnimation { get; set; }

        /// <summary>
        ///     Name of the particle effect to generate whenever the attack succeeds.
        /// </summary>
        public string HitParticles { get; set; }

        /// <summary>
        ///     For ranged attacks, this is the projectile to create (entity ID)
        /// </summary>
        public string ProjectileType { get; set; }

        /// <summary>
        ///     For ranged attacks, this is the speed of the projectile (in voxels per second)
        /// </summary>
        public float LaunchSpeed { get; set; }

        /// <summary>
        ///     Describes how the attack is triggered.
        /// </summary>
        public AttackTrigger TriggerMode { get; set; }

        /// <summary>
        ///     If the trigger mode is "Animation", the frame that the attack is triggered on.
        /// </summary>
        public int TriggerFrame { get; set; }


        /// <summary>
        ///     Called whenever the attack is deserialized from JSON.
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            CreateHitAnimation();
        }

        /// <summary>
        ///     Creates an animated sprite to play whenever the attack succeeds.
        /// </summary>
        public void CreateHitAnimation()
        {
            Texture2D text = TextureManager.GetTexture(AnimationAsset);
            var frames = new List<int>();
            for (int i = 0; i < text.Width/text.Height; i++)
            {
                frames.Add(i);
            }
            HitAnimation = new Animation(AnimationAsset, text.Height, text.Height, frames.ToArray());
        }

        /// <summary>
        ///     Attempt to attack the target. (Whenever the target is a voxel)
        /// </summary>
        /// <param name="performer">The creature that's attacking the target.</param>
        /// <param name="pos">The position of the target.</param>
        /// <param name="other">The voxel to attack.</param>
        /// <param name="time">The current time.</param>
        /// <param name="bonus">Bonus to apply to the attack damage.</param>
        /// <param name="faction">The faction of the target?</param>
        /// <returns>True if the attack was successful, false otherwise.</returns>
        public bool Perform(Creature performer, Vector3 pos, Voxel other, DwarfTime time, float bonus, string faction)
        {
            // Do not damage null voxels.
            if (other == null)
            {
                return false;
            }

            // Attempt to trigger the attack.
            switch (TriggerMode)
            {
                case AttackTrigger.Timer:
                    RechargeTimer.Update(time);
                    if (!RechargeTimer.HasTriggered) return false;
                    break;
                case AttackTrigger.Animation:
                    if (performer.Sprite.CurrentAnimation == null ||
                        performer.Sprite.CurrentAnimation.CurrentFrame != TriggerFrame)
                    {
                        return false;
                    }
                    break;
            }

            // Attack was successfully triggered. Damage the voxel.
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
                        IndicatorManager.DrawIndicator(HitAnimation.Clone(), other.Position + Vector3.One*0.5f, 10.0f,
                            1.0f, MathFunctions.RandVector2Circle()*10, HitColor, MathFunctions.Rand() > 0.5f);
                    }
                    break;
                }
                case AttackMode.Ranged:
                {
                    LaunchProjectile(pos, other.Position, null);
                    break;
                }
            }
            return true;
        }

        /// <summary>
        ///     For ranged attacks, creates a projectile to hit some target.
        /// </summary>
        /// <param name="start">The start position of the projectile.</param>
        /// <param name="end">The end position of the projectile.</param>
        /// <param name="target">The target we want to damage.</param>
        public void LaunchProjectile(Vector3 start, Vector3 end, Body target)
        {
            // Try to find a good launch velocity.
            Vector3 velocity = (end - start);
            float dist = velocity.Length();
            // This is the amount of time it will take to get to the target.
            float T = dist/LaunchSpeed;

            // This is a ballistic trajectory that is affected by gravity. Compensate
            // for the total velocity incurred downward by gravity, and add a linear velocity
            // required to reach the target.
            velocity = 1.0f/T*(end - start) - 0.5f*Vector3.Down*10*T;

            // Fill out the required data to create the projectile, and pop one out
            // using the entity factory. The projectile will only affect the target.
            var data = new Blackboard();
            data.SetData("Velocity", velocity);
            data.SetData("Target", target);
            EntityFactory.CreateEntity<Body>(ProjectileType, start, data);
        }

        /// <summary>
        ///     This triggers the attack against nobody toward a dummy position. This is useful for making
        ///     creatures play their attack animations without having a specific target. For example, if we want
        ///     creatures to play their attack animation while building something, they could use this function.
        /// </summary>
        /// <param name="performer">The creature doing the attack.</param>
        /// <param name="time">The current time.</param>
        /// <param name="pos">The target position to perform the attack toward.</param>
        public void PerformNoDamage(Creature performer, DwarfTime time, Vector3 pos)
        {
            switch (TriggerMode)
            {
                case AttackTrigger.Timer:
                    RechargeTimer.Update(time);
                    if (!RechargeTimer.HasTriggered) return;
                    break;
                case AttackTrigger.Animation:
                    if (performer.Sprite.CurrentAnimation == null ||
                        performer.Sprite.CurrentAnimation.CurrentFrame != TriggerFrame)
                        return;
                    break;
            }

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
                    IndicatorManager.DrawIndicator(HitAnimation, pos, 0.6f, 2.0f, MathFunctions.RandVector2Circle(),
                        Color.White, MathFunctions.Rand() > 0.5f);
                }
            }
        }

        /// <summary>
        ///     Attempts to attack a target entity.
        /// </summary>
        /// <param name="performer">The creature doing the attacking.</param>
        /// <param name="other">The target to attack.</param>
        /// <param name="time">The current time</param>
        /// <param name="bonus">Bonus damage to add to the attack.</param>
        /// <param name="pos">The position of the target.</param>
        /// <returns>True if the attack was successful, false otherwise.</returns>
        public bool Perform(Creature performer, Body other, DwarfTime time, float bonus, Vector3 pos)
        {
            // Determine if we should trigger the attack.
            switch (TriggerMode)
            {
                case AttackTrigger.Timer:
                    RechargeTimer.Update(time);
                    if (!RechargeTimer.HasTriggered) return false;
                    break;
                case AttackTrigger.Animation:
                    if (performer.Sprite.CurrentAnimation == null ||
                        performer.Sprite.CurrentAnimation.CurrentFrame != TriggerFrame)
                        return false;
                    break;
            }

            // The attack was triggered, attempt damage.
            switch (Mode)
            {
                    // Melee and dogfight attacks damage entities in the same way.
                case AttackMode.Melee:
                case AttackMode.Dogfight:
                {
                    // Get the target's health and attempt to damage it.
                    Health health = other.GetRootComponent().GetChildrenOfType<Health>(true).FirstOrDefault();
                    if (health != null)
                    {
                        health.Damage(DamageAmount + bonus);
                    }

                    PlayNoise(other.GlobalTransform.Translation);
                    if (HitParticles != "")
                    {
                        PlayState.ParticleManager.Trigger(HitParticles, other.LocalTransform.Translation, Color.White, 5);
                    }

                    if (HitAnimation != null)
                    {
                        HitAnimation.Reset();
                        HitAnimation.Play();
                        IndicatorManager.DrawIndicator(HitAnimation.Clone(), other.BoundingBox.Center(), 10.0f, 1.0f,
                            MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);
                    }


                    // If the target is a physical object, knock it back using a force.
                    var physics = other as Physics;

                    if (physics != null)
                    {
                        Vector3 force = other.Position - pos;

                        if (force.LengthSquared() > 0.01f)
                        {
                            force.Normalize();
                            physics.ApplyForce(force*Knockback, 1.0f);
                        }
                    }

                    break;
                }

                    // Ranged attacks produce a projectile toward the target.
                case AttackMode.Ranged:
                {
                    PlayNoise(other.GlobalTransform.Translation);
                    LaunchProjectile(pos, other.Position, other);
                    break;
                }
            }

            return true;
        }

        /// <summary>
        ///     Plays the hit noise at a position.
        /// </summary>
        /// <param name="position">The position to play the hit noise at.</param>
        public void PlayNoise(Vector3 position)
        {
            SoundManager.PlaySound(HitNoise, position, true);
        }
    }
}