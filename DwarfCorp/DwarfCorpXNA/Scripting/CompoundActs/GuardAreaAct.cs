// GuardVoxelAct.cs
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
    /// A creature goes to a voxel, and then waits there until cancelled.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GuardAreaAct : CompoundCreatureAct
    {
        public GuardAreaAct()
        {

        }

        public bool LoopCondition()
        {
            return GuardDesignationExists() && !EnemiesNearby() && !Creature.Status.Energy.IsDissatisfied() && !Creature.Status.Hunger.IsDissatisfied();
        }

        public bool GuardDesignationExists()
        {
            return Agent.Faction.GuardedVoxels.Count > 0;
        }

        public bool ExitCondition()
        {
            if (EnemiesNearby())
            {
                Creature.AI.OrderEnemyAttack();
            }

            return !GuardDesignationExists();
        }


        public bool EnemiesNearby()
        {
            return (Agent.Sensor.Enemies.Count > 0);
        }

        public IEnumerable<Act.Status> GetRandomGuardDesignation(CreatureAI agent)
        {
            // Todo: Less expensive algorithm.

            var voxels = Agent.Faction.GuardedVoxels
                .Select(d => d.Value).ToList();

            voxels.Sort((a, b) => 
            {
                if (a == b)
                {
                    return 0;
                }
                return (a.WorldPosition - agent.Position).LengthSquared() < (b.WorldPosition - agent.Position).LengthSquared() ? -1 : 1;

            });


            foreach (var vox in voxels)
            {
                if (MathFunctions.RandEvent(0.25f))
                {
                    agent.Blackboard.SetData("GuardVoxel", vox);
                    yield return Act.Status.Success;
                    yield break;
                }
            }
            yield return Act.Status.Fail;
            yield break;
        }

        public GuardAreaAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Guard Area";

            Tree = new WhileLoop(new Sequence 
                (
                    new Wrap(() => GetRandomGuardDesignation(agent)),
                    new Domain(LoopCondition, new GoToNamedVoxelAct("GuardVoxel", PlanAct.PlanType.Adjacent, agent)),
                    new StopAct(Agent),
                    new Domain(LoopCondition, new WanderAct(Agent, 10.0f, 5.0f, 1.0f))
                ), new Condition(LoopCondition));
        }
    }

}