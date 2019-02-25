using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class Potion
    {
        public String Name;
        public string Description;
        public Buff Effects;
        public List<Quantitiy<Resource.ResourceTags>> Ingredients;
        public int Icon;
        public Potion()
        {

        }

        public void Drink(Creature creature)
        {
            creature.AddBuff(Effects.Clone());
        }

        public bool ShouldDrink(Creature creature)
        {
            if (Effects == null)
            {
                return false;
            }
            return Effects.IsRelevant(creature);
        }
    }
}
