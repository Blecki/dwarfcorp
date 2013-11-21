using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class FoodComponent : GameComponent
    {
        public float FoodAmount { get; set; }

        public FoodComponent(ComponentManager manager, string name, GameComponent parent, float foodAmount) :
            base(manager, name, parent)
        {
            FoodAmount = foodAmount;
        }
    }

}