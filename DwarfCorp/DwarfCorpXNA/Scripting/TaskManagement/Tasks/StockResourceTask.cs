// StockResourceTask.cs
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
    /// Tells a creature that it should pick up an item and put it in a stockpile.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class StockResourceTask : Task
    {
        public ResourceAmount EntityToGather = null;
        public string ZoneType = "Stockpile";

        public StockResourceTask()
        {
            Category = TaskCategory.Gather;
            Priority = PriorityType.Low;
        }

        public StockResourceTask(ResourceAmount entity)
        {
            Category = TaskCategory.Gather;
            EntityToGather = entity.CloneResource();
            Name = "Stock Entity: " + entity.ResourceType + " " + entity.NumResources;
            Priority = PriorityType.Low;
        }

        public override Act CreateScript(Creature creature)
        {
            return new StockResourceAct(creature.AI, EntityToGather);
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent.AI.Status.IsAsleep)
                return Feasibility.Infeasible;

            return agent.Faction.HasFreeStockpile(EntityToGather) && 
                !agent.AI.Movement.IsSessile && agent.Inventory.HasResource(EntityToGather) ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return IsFeasible(agent) == Feasibility.Feasible ? 1.0f : 1000.0f;
        }

    }

}