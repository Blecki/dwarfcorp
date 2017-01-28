// Health.cs
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
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// When attached to another component, gives it health. When the health reaches its minimum, the object (and its parents/children) are destroyed.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Health : GameComponent
    {
        public enum DamageType
        {
            Normal,
            Slashing,
            Bashing,
            Radiant,
            Fire,
            Poison,
            Acid,
            Cold,
            Lightning,
            Necrotic,
            Psychological
        }

        public struct DamageAmount
        {
            public float Amount { get; set; }
            public DamageType DamageType { get; set; }
        }

        public Dictionary<DamageType, float> Resistances { get; set; } 
        public float Hp { get; set; }
        public float MaxHealth { get; set; }
        public float MinHealth { get; set; }

        public Health()
        {
            InitializeResistance();
        }

        public void InitializeResistance()
        {
            Resistances = new Dictionary<DamageType, float>();

            DamageType[] types = (DamageType[])Enum.GetValues(typeof(DamageType));

            foreach (DamageType damage in types)
            {
                Resistances[damage] = 0.0f;
            }
        }

        public Health(ComponentManager manager, string name, GameComponent parent, float maxHealth, float minHealth, float currentHp) :
            base(name, parent)
        {
            InitializeResistance();
            MaxHealth = maxHealth;
            MinHealth = minHealth;

            Hp = currentHp;
        }

        public virtual void Heal(float amount)
        {
            Hp = Math.Min(Math.Max(Hp + amount, MinHealth), MaxHealth);

            if(!(Hp <= MinHealth))
            {
                return;
            }

            if(Parent != null)
            {
                Parent.Die();
            }
        }

        public virtual float Damage(float amount, DamageType type = DamageType.Normal)
        {
            if(!IsDead)
            {
                float damage = Math.Max(amount - Resistances[type], 0.0f);
                Heal(-damage);
                Parent.ReceiveMessageRecursive(new Message(Message.MessageType.OnHurt, damage.ToString(CultureInfo.InvariantCulture)));
                return damage;
            }

            return 0.0f;
        }
    }

}