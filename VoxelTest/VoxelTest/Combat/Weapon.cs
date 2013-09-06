using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
namespace DwarfCorp
{
    public class Weapon
    {
        [JsonIgnore]
        public CreatureAIComponent Creature { get; set; }
        public float DamageAmount { get; set; }
        public Timer HitTimer { get; set; }
        public string Name { get; set;}
        public float Range { get; set; }
        public string HitNoise { get; set; }

        public Weapon(string name, float damage, float time, float range,  CreatureAIComponent creature, string noise)
        {
            Name = name;
            DamageAmount = damage;
            HitTimer = new Timer(time + (float)PlayState.random.NextDouble() * time * 0.5f, false);
            Range = range;
            Creature = creature;
            Creature.Creature.Weapon = this;
            HitNoise = noise;
        }

        public void Update(GameTime time)
        {
            HitTimer.Update(time);
        }

        public void PlayNoise()
        {
            if (HitTimer.HasTriggered)
            {
                SoundManager.PlaySound(HitNoise, Creature.Creature.Physics.GlobalTransform.Translation);
            }
        }

    }
}
