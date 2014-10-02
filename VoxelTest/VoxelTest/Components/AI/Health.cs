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
        public float Hp { get; set; }
        public float MaxHealth { get; set; }
        public float MinHealth { get; set; }

        public Health()
        {
            
        }

        public Health(ComponentManager manager, string name, GameComponent parent, float maxHealth, float minHealth, float currentHp) :
            base(manager, name, parent)
        {
            MaxHealth = maxHealth;
            MinHealth = minHealth;

            Hp = currentHp;
        }

        public void Heal(float amount)
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

        public void Damage(float amount)
        {
            if(!IsDead)
            {
                Heal(-amount);
                Parent.ReceiveMessageRecursive(new Message(Message.MessageType.OnHurt, amount.ToString(CultureInfo.InvariantCulture)));
            }
        }
    }

}