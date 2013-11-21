using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class HealthComponent : GameComponent
    {
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float MinHealth { get; set; }

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

            if(Health <= MinHealth)
            {
                if(Parent != null)
                {
                    Parent.Die();
                }
            }
        }

        public void Damage(float amount)
        {
            if(!IsDead)
            {
                Heal(-amount);
            }
        }
    }

}