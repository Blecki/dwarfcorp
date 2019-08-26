using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
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

        public Health(ComponentManager manager, string name, float maxHealth, float minHealth, float currentHp) :
            base(name, manager)
        {
            InitializeResistance();
            MaxHealth = maxHealth;
            MinHealth = minHealth;

            Hp = currentHp;

            BoundingBoxSize = new Microsoft.Xna.Framework.Vector3(0.2f, 0.2f, 0.2f);
            UpdateBoundingBox();
        }

        public virtual void Heal(float amount)
        {
            bool wasHealthNegative = (int)Hp <= (int)MinHealth;

            Hp = Math.Min(Math.Max(Hp + amount, MinHealth), MaxHealth);

            if(!(Hp <= MinHealth))
            {
                return;
            }


            // Only die when damaged after health already reached (integer) zero.
            // otherwise, go unconscious. This is similar to D&D rules.
            if (wasHealthNegative && amount < 0 && Parent != null)
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
                // Todo: Implement this using a callback to get rid of this messaging system once and for all.
                Parent.ReceiveMessageRecursive(new Message(Message.MessageType.OnHurt, damage.ToString(CultureInfo.InvariantCulture)));
                return damage;
            }

            return 0.0f;
        }
    }

}