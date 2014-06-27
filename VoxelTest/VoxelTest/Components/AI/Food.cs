using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    /// <summary>
    /// Component which is attached to a food object (like an apple)
    /// </summary>
    public class Food : GameComponent
    {
        public float FoodAmount { get; set; }

        public Food(ComponentManager manager, string name, GameComponent parent, float foodAmount) :
            base(manager, name, parent)
        {
            FoodAmount = foodAmount;
        }
    }

}