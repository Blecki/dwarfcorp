using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;

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
            
            if(Agent.Status.Hunger.IsUnhappy())
            {
                Room commonRoom = Creature.Faction.GetNearestRoomOfType("CommonRoom", Agent.Position);

                if (commonRoom != null && commonRoom.IsBuilt)
                {
                    Tree =  (new GoToZoneAct(Agent, commonRoom) & new GoToChairAndSitAct(Agent) & new Wrap(Creature.EatStockedFood));
                }
                else
                {
                    Stockpile stockRoom = Creature.Faction.GetNearestStockpile(Agent.Position);

                    if (stockRoom != null && stockRoom.IsBuilt)
                    {
                        Tree = new GoToZoneAct(Agent, stockRoom) & new Wrap(Creature.EatStockedFood);
                    }
                }
            }
            else
            {
                Tree = null;
            }
             
            base.Initialize();
        }
    }
}
