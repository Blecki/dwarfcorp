// SatisfyTirednessTask.cs
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
    /// Tells a creature that it should find a bed and sleep (or else pass out).
    /// </summary>
    public class SatisfyTirednessTask : Task
    {
        public SatisfyTirednessTask()
        {
            ReassignOnDeath = false;
            Name = "Go to sleep";
            Priority = PriorityType.Medium;
        }

        public override Act CreateScript(Creature agent)
        {
            return new FindBedAndSleepAct(agent.AI);
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return agent.Sensors.Enemies.Count == 0 ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return agent.Status.Hunger.IsDissatisfied() ? 0.0f : 1e13f;
        }

    }

    /// <summary>
    /// Tells a creature that it should find a bed and sleep (or else pass out).
    /// </summary>
    public class GetHealedTask : Task
    {
        public GetHealedTask()
        {
            Name = "Heal thyself";
            Priority = PriorityType.Urgent;
        }

        public override Act CreateScript(Creature agent)
        {
            return new GetHealedAct(agent.AI);
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            Body closestItem = agent.Faction.FindNearestItemWithTags("Bed", agent.AI.Position, true);

            return closestItem != null || agent.AI.Status.Health.IsCritical() ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 0.0f;
        }

    }
}
