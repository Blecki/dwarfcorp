using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// The creature finds food in a stockpile or BuildRoom, and eats it.
    /// </summary>
    public class FindAndEatFoodAct : CompoundCreatureAct
    {

        public FindAndEatFoodAct()
        {
            Name = "Find and Eat Food";
        }

        public FindAndEatFoodAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Find and Eat Food";
        }

    

        public override void Initialize()
        {
            /*
            if(Agent.Status.Hunger.IsUnhappy())
            {
                Tree = new Sequence(
                    new GetItemWithTagsAct(Agent, "Food"),
                    (new ConsumeItemAct(Agent) | new DropItemAct(Agent))
                    );
            }
            else
            {
                Tree = null;
            }
             */
            base.Initialize();
        }
    }
}
