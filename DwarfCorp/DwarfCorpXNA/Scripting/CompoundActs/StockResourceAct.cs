// StockResourceAct.cs
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

namespace DwarfCorp
{
    /// <summary>
    /// A creature takes an item to an open stockpile and leaves it there.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class StockResourceAct : CompoundCreatureAct
    {
        public ResourceAmount ItemToStock { get; set; }
        public string ItemID { get; set; }

        public StockResourceAct()
        {

        }

        public StockResourceAct(CreatureAI agent, string item) :
            base(agent)
        {
            ItemID = item;
            Tree = null;
            ItemToStock = null;
            Name = "Stock Item";
        }

        public StockResourceAct(CreatureAI agent, ResourceAmount item) :
            base(agent)
        {
            ItemToStock = item.CloneResource();
            Name = "Stock Item";
            Tree = null;
        }

        public override void Initialize()
        {
            base.Initialize();
        }


        public IEnumerable<Status> OnFail()
        {
            if (ItemToStock != null && ItemToStock.NumResources >= 0 && Agent.Creature.Inventory.Resources.HasResource(ItemToStock))
            {
                Agent.GatherManager.StockOrders.Add(new GatherManager.StockOrder()
                {
                    Resource = ItemToStock
                });
                yield return Status.Success;
                yield break;
            }
            yield return Status.Fail;
        }

        public override IEnumerable<Status> Run()
        {
            if (Tree == null)
            {
                if (ItemToStock == null)
                {
                    ItemToStock = Agent.Blackboard.GetData<ResourceAmount>(ItemID);
                }


                if (ItemToStock != null)
                {

                    Tree = new Sequence(
                        new SetBlackboardData<ResourceAmount>(Agent, "GatheredResource", ItemToStock.CloneResource()),
                        new SearchFreeStockpileAct(Agent, "TargetStockpile", "FreeVoxel", ItemToStock),
                        
                                        new Select(
                                                    new Sequence(
                                                                    new GoToVoxelAct("FreeVoxel", PlanAct.PlanType.Adjacent, Agent),
                                                                    new PutResourceInZone(Agent, "TargetStockpile", "FreeVoxel", "GatheredResource")
                                                                )
                                                  )
                                         
                        ) | new Wrap(OnFail)
                     ;

                    Tree.Initialize();
                }
            }

            return base.Run();
        }
    }

}