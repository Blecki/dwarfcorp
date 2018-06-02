// GatherItemAct.cs
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
    public class GatherItemAct : CompoundCreatureAct
    {
        public Body ItemToGather { get; set; }
        public string ItemID { get; set; }

        public GatherItemAct()
        {
            
        }

        public IEnumerable<Status> AddItemToGatherManager()
        {
            Agent.GatherManager.ItemsToGather.Add(ItemToGather);
            yield return Status.Success;
        }

        public IEnumerable<Status> Finally(CreatureAI creature)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
            {
                var designation = creature.Faction.Designations.GetEntityDesignation(ItemToGather, DesignationType.Gather);
                if (designation != null)
                {
                    creature.World.MakeAnnouncement(String.Format("{0} cancelled gather task because it is unreachable", creature.Stats.FullName));
                    if (creature.Faction == creature.World.PlayerFaction)
                    {
                        creature.World.Master.TaskManager.CancelTask(designation.Task);
                    }
                }
                yield return Act.Status.Fail;
            }

            yield return Status.Fail;
        }

        public IEnumerable<Status> RemoveItemFromGatherManager()
        {
            //if (!ItemToGather.IsDead)
            //{
            //    yield return Status.Fail;
            //}

            if(Agent.GatherManager.ItemsToGather.Contains(ItemToGather))
            {
                Agent.GatherManager.ItemsToGather.Remove(ItemToGather);
            }
            yield return Status.Success;
        }

        public IEnumerable<Status> AddStockOrder()
        {
            if (ItemToGather is CoinPile)
            {
                Agent.GatherManager.StockMoneyOrders.Add(new GatherManager.StockMoneyOrder()
                {
                    Destination = null,
                    Money = (ItemToGather as CoinPile).Money
                });   
            }
            else
            {
                Agent.GatherManager.StockOrders.Add(new GatherManager.StockOrder()
                {
                    Destination = null,
                    Resource = new ResourceAmount(ItemToGather)
                });   
            }

            yield return Status.Success;
        }

        public GatherItemAct(CreatureAI agent, string item) :
            base(agent)
        {
            ItemID = item;
            Tree = null;
            ItemToGather = null;
            Name = "Gather Item";
        }

        public GatherItemAct(CreatureAI agent, Body item) :
            base(agent)
        {
            ItemToGather = item;
            Name = "Gather Item";
            Tree = null;
        }

        public override void Initialize()
        {
            base.Initialize();
        }



        public override IEnumerable<Status> Run()
        {
            if(Tree == null)
            {
                if(ItemToGather == null)
                {
                    ItemToGather = Agent.Blackboard.GetData<Body>(ItemID);
                }


                if(ItemToGather != null)
                {
                    Tree = new Sequence(
                        new SetBlackboardData<Body>(Agent, "GatherItem", ItemToGather),
                        new Wrap(AddItemToGatherManager),
                        new GoToEntityAct(ItemToGather, Agent),
                        new StashAct(Agent, StashAct.PickUpType.None, null, "GatherItem", "GatheredResource"),
                        new Wrap(RemoveItemFromGatherManager),
                        new Wrap(AddStockOrder)
                        ) 
                        | (new Wrap(RemoveItemFromGatherManager) & new Wrap(() => Finally(Agent)) & false);

                    Tree.Initialize();
                }
            }

            if(Tree == null)
            {
                yield return Status.Fail;
            }
            else
            {
                foreach(Status s in base.Run())
                {
                    yield return s;
                }
            }
        }
    }

}