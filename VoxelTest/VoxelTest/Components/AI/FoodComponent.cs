using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    /// <summary>
    /// Component which is attached to a food object (like an apple)
    /// </summary>
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