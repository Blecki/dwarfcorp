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
using System;
using System.Collections.Generic;
using System.Linq;
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
        [JsonIgnore]
        protected Animation HitAnimation { get; set; }
        public string HitParticles { get; set; }
        public string ProjectileType { get; set; }
        public float LaunchSpeed { get; set; }
        public bool HasTriggered { get; set; }
        public AttackTrigger TriggerMode { get; set; }
        public int TriggerFrame { get; set; }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            CreateHitAnimation();
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
            HitAnimation = other.HitAnimation;
            HitParticles = other.HitParticles;
            HitColor = other.HitColor;
            ProjectileType = other.ProjectileType;
            LaunchSpeed = other.LaunchSpeed;
            AnimationAsset = other.AnimationAsset;
            TriggerMode = other.TriggerMode;
            TriggerFrame = other.TriggerFrame;
            HasTriggered = false;
        }

        public void CreateHitAnimation()
        {
            Texture2D text = TextureManager.GetTexture(AnimationAsset);
            List<int> frames = new List<int>();
            for (int i = 0; i < text.Width/text.Height; i++)
            {
                frames.Add(i);
            }
            HitAnimation = new Animation(AnimationAsset, text.Height, text.Height, frames.ToArray());
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
            HitAnimation = null;
            HitColor = Color.White;
            HitParticles = "";
            ProjectileType = "";
            AnimationAsset = animation;
            CreateHitAnimation();
        }

        public IEnumerable<Act.Status> Perform(Creature performer, Vector3 pos, VoxelHandle other, DwarfTime time, float bonus, string faction)
        {
            while (true)
            {
                if (other == null)
                {
                    yield return Act.Status.Fail;
                    yield break;
                }

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
                        if (performer.Sprite.CurrentAnimation == null ||
                            performer.Sprite.CurrentAnimation.CurrentFrame != TriggerFrame)
                        {
                            if (performer.Sprite.CurrentAnimation != null)
                                performer.Sprite.CurrentAnimation.Play();
                            yield return Act.Status.Running;
                            continue;
                        }
                        break;
                }

                switch (Mode)
                {
                    case AttackMode.Melee:
                    {
                        other.Health -= DamageAmount + bonus;
                        other.Type.HitSound.Play(other.Position);
                        //PlayNoise(other.Position);
                        if (HitParticles != "")
                        {
                            performer.Manager.World.ParticleManager.Trigger(HitParticles, other.Position, Color.White, 5);
                        }

                        if (HitAnimation != null)
                        {
                            HitAnimation.Reset();
                            HitAnimation.Play();
                            IndicatorManager.DrawIndicator(HitAnimation.Clone(), other.Position + Vector3.One*0.5f,
                                10.0f, 1.0f, MathFunctions.RandVector2Circle()*10, HitColor, MathFunctions.Rand() > 0.5f);
                        }
                        break;
                    }
                    case AttackMode.Ranged:
                    {
                        LaunchProjectile(pos, other.Position, null);
                        break;
                    }

                }
                yield return Act.Status.Success;
                yield break;
            }

        }

        public void LaunchProjectile(Vector3 start, Vector3 end, Body target)
        {
            Vector3 velocity = (end - start);
            float dist = velocity.Length();
            float T = dist / LaunchSpeed;
            velocity = 1.0f/T*(end - start) - 0.5f*Vector3.Down*10*T;
            Blackboard data = new Blackboard();
            data.SetData("Velocity", velocity);
            data.SetData("Target", target);
            EntityFactory.CreateEntity<Body>(ProjectileType, start, data);
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
                    if (performer.Sprite.CurrentAnimation == null ||
                        performer.Sprite.GetAnimation("AttackingFORWARD").CurrentFrame != TriggerFrame)
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
                    HitAnimation.Reset();
                    HitAnimation.Play();
                    IndicatorManager.DrawIndicator(HitAnimation.Clone(), pos, 10.0f, 1.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);
                    PlayNoise(pos);
                }
            }
            HasTriggered = true;
            return true;
        }

        public bool Perform(Creature performer, Body other, DwarfTime time, float bonus, Vector3 pos, string faction)
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
                    if (performer.Sprite.CurrentAnimation == null ||
                        performer.Sprite.GetAnimation("AttackingFORWARD").CurrentFrame != TriggerFrame)
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
                        var health = other.GetRoot().EnumerateAll().OfType<Health>().FirstOrDefault();
                        if (health != null)
                        {
                            health.Damage(DamageAmount + bonus);
                            Vector3 knock = other.Position - performer.Physics.Position;
                            knock.Normalize();
                            knock *= 0.2f;
                            if (other.AnimationQueue.Count == 0)
                                other.AnimationQueue.Add(new KnockbackAnimation(0.15f, other.LocalTransform, knock));
                        }

                        PlayNoise(other.GlobalTransform.Translation);
                        if (HitParticles != "")
                        {
                            performer.Manager.World.ParticleManager.Trigger(HitParticles, other.LocalTransform.Translation, Color.White, 5);
                        }

                        if (HitAnimation != null)
                        {
                            HitAnimation.Reset();
                            HitAnimation.Play();
                            IndicatorManager.DrawIndicator(HitAnimation.Clone(), other.BoundingBox.Center(), 10.0f, 1.0f, MathFunctions.RandVector2Circle(), Color.White, MathFunctions.Rand() > 0.5f);
                        }

                        Physics physics = other as Physics;

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
                case AttackMode.Ranged:
                    {
                        PlayNoise(other.GlobalTransform.Translation);
                        LaunchProjectile(pos, other.Position, other);
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