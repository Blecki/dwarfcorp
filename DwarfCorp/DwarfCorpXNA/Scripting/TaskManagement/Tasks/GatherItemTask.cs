// GatherItemTask.cs
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
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should pick up an item and put it in a stockpile.
    /// </summary>
    internal class GatherItemTask : Task
    {
        public Body EntityToGather = null;
        public string ZoneType = "Stockpile";

        public GatherItemTask()
        {
            Priority = PriorityType.Low;
            Category = TaskCategory.Gather;
        }

        public GatherItemTask(Body entity)
        {
            EntityToGather = entity;
            Name = "Gather Entity: " + entity.Name + " " + entity.GlobalID;
            Priority = PriorityType.Low;
            Category = TaskCategory.Gather;
        }

        public override Act CreateScript(Creature creature)
        {
            return new GatherItemAct(creature.AI, EntityToGather);
        }

        public override bool ShouldDelete(Creature agent)
        {
            return IsFeasible(agent) == Feasibility.Infeasible;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return agent.Stats.IsTaskAllowed(Category) &&
                      EntityToGather != null
                   && EntityToGather.Active
                   && !agent.AI.Movement.IsSessile
                   && !agent.AI.Status.IsAsleep ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return EntityToGather != null &&
                  EntityToGather.Active &&
                  !EntityToGather.IsDead &&
                  !agent.AI.GatherManager.ItemsToGather.Contains(EntityToGather);
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return EntityToGather == null  || EntityToGather.IsDead ? 1000 : (agent.AI.Position - EntityToGather.GlobalTransform.Translation).LengthSquared();
        }
        
        public override bool IsComplete(Faction Faction)
        {
            return EntityToGather == null || !EntityToGather.Active || EntityToGather.IsDead;
        }

        public override void OnEnqueued(Faction Faction)
        {
            Faction.Designations.AddEntityDesignation(EntityToGather, DesignationType.Gather, null, this);
        }

        public override void OnDequeued(Faction Faction)
        {
            Faction.Designations.RemoveEntityDesignation(EntityToGather, DesignationType.Gather);
        }
    }

}