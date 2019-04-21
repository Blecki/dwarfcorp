// KillEntityTask.cs
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
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class FleeEntityTask : Task
    {
        public GameComponent ScaryEntity = null;
        public int Distance = 10;

        public FleeEntityTask()
        {
        }

        public FleeEntityTask(GameComponent entity, int Distance)
        {
            Name = "Kill Entity: " + entity.Name + " " + entity.GlobalID;
            ScaryEntity = entity;
            Priority = PriorityType.Urgent;
            AutoRetry = true;
                Category = TaskCategory.Other;
            this.Distance = Distance;
            BoredomIncrease = GameSettings.Default.Boredom_ExcitingTask;
        }

        public override Act CreateScript(Creature creature)
        {
            Name = "Flee Entity: " + ScaryEntity.Name + " " + ScaryEntity.GlobalID;
            IndicatorManager.DrawIndicator(IndicatorManager.StandardIndicators.Exclaim, creature.AI.Position, 1.0f, 1.0f, Vector2.UnitY * -32);
            return new FleeEntityAct(creature.AI) { Entity = ScaryEntity, PathLength = Distance };
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 0.0f;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return ScaryEntity != null && !ScaryEntity.IsDead;
        }

        public override bool ShouldDelete(Creature agent)
        {
            if (ScaryEntity == null || ScaryEntity.IsDead || (ScaryEntity.Position - agent.AI.Position).Length() > Distance)
                return true;
 
            return false;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent == null || agent.IsDead || ScaryEntity == null || ScaryEntity.IsDead)
                return Feasibility.Infeasible;
            else
                return Feasibility.Feasible;
        }

        public override bool IsComplete(Faction faction)
        {
            return ScaryEntity == null || ScaryEntity.IsDead;
        }

    }
}
