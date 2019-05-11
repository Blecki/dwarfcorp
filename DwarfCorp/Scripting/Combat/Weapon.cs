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
    public class Weapon
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

        public float DamageAmount;

        public float RechargeRate;
        public string Name;
        public float Range;
        public SoundSource HitNoise;
        public Color HitColor = Color.White;
        public AttackMode Mode = AttackMode.Melee;
        public float Knockback = 0.0f;
        public string AnimationAsset;

        [JsonIgnore] private Animation _hitAnimation = null;
        [JsonIgnore] public Animation HitAnimation
        {
            get
            {
                if (_hitAnimation == null)
                    _hitAnimation = Library.CreateSimpleAnimation(AnimationAsset);
                return _hitAnimation;
            }
        }

        public string HitParticles = "";
        public string ProjectileType = "";
        public float LaunchSpeed;
        public bool HasTriggered;
        public AttackTrigger TriggerMode;
        public int TriggerFrame;

        public string DiseaseToSpread = null;
        public bool ShootLaser = false;

        public Weapon()
        {
            
        }

        public Weapon(Weapon other)
        {
            Name = other.Name;
            DamageAmount = other.DamageAmount;
            Range = other.Range;
            HitNoise = other.HitNoise;
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

        public Weapon(string name, float damage, float time, float range, SoundSource noise, string animation)
        {
            Name = name;
            DamageAmount = damage;
            Range = range;
            HitNoise = noise;
            AnimationAsset = animation;
        }
    }
}