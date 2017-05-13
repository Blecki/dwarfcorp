// FindAndEatFoodAct.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
            Name = "Find and Eat Edible";
        }

        public FindAndEatFoodAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Find and Eat Edible";
        }


        public override void Initialize()
        {
            if(Agent.Status.Hunger.IsUnhappy())
            {
                Tree = new Sequence(new GetResourcesAct(Agent, Resource.ResourceTags.Edible), 
                                    new Select(new GoToChairAndSitAct(Agent), true),
                                    new Wrap(Creature.EatStockedFood));
                /*
                Stockpile stockRoom = Creature.Faction.GetNearestStockpile(Agent.Position);

                if (stockRoom != null && stockRoom.IsBuilt)
                {
                    Tree = new GoToZoneAct(Agent, stockRoom) & new Wrap(Creature.EatStockedFood);
                }

                Room commonRoom = Creature.Faction.GetNearestRoomOfType("CommonRoom", Agent.Position);

                if (commonRoom != null && commonRoom.IsBuilt)
                {
                    Tree =  (new GoToZoneAct(Agent, commonRoom) & new GoToChairAndSitAct(Agent) & new Wrap(Creature.EatStockedFood));
                }
                 */
            }
            else
            {
                Tree = null;
            }
             
            base.Initialize();
        }
    }
}
