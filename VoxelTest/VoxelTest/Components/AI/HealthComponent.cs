using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// When attached to another component, gives it health. When the health reaches its minimum, the object (and its parents/children) are destroyed.
    /// </summary>
    public class HealthComponent : GameComponent
    {
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float MinHealth { get; set; }

        public HealthComponent()
        {
            
        }

        public HealthComponent(ComponentManager manager, string name, GameComponent parent, float maxHealth, float minHealth, float currentHealth) :
            base(manager, name, parent)
        {
            MaxHealth = maxHealth;
            MinHealth = minHealth;

            Health = currentHealth;
        }

        public void Heal(float amount)
        {
            Health = Math.Min(Math.Max(Health + amount, MinHealth), MaxHealth);

            if(!(Health <= MinHealth))
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