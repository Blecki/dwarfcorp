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