// CreatureAI.cs
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
using System.Runtime.Serialization;
//using System.Windows.Forms;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary> Task telling the creature to exit the world. </summary>
    public class LeaveWorldTask : Task
    {
        public Timer DieTimer = new Timer(60, true);

        public LeaveWorldTask()
        {
            ReassignOnDeath = false;
        }

        public IEnumerable<Act.Status> GreedyFallbackBehavior(Creature agent)
        {
            var edgeGoal = new EdgeGoalRegion();

            while (true)
            {
                DieTimer.Update(DwarfTime.LastTime);
                if (DieTimer.HasTriggered)
                {
                    foreach(var status in Die(agent))
                    {
                        continue;
                    }
                    yield break;
                }
                var creatureVoxel = agent.Physics.CurrentVoxel;
                List<MoveAction> path = new List<MoveAction>();
                var storage = new MoveActionTempStorage();
                for (int i = 0; i < 10; i++)
                {
                    if (edgeGoal.IsInGoalRegion(creatureVoxel))
                    {
                        foreach (var status in Die(agent))
                            continue;
                        yield return Act.Status.Success;
                        yield break;
                    }

                    var actions = agent.AI.Movement.GetMoveActions(new MoveState { Voxel = creatureVoxel }, agent.World.OctTree, new List<Body>(), storage);

                    float minCost = float.MaxValue;
                    var minAction = new MoveAction();
                    bool hasMinAction = false;
                    foreach (var action in actions)
                    {
                        var vox = action.DestinationVoxel;

                        float cost = edgeGoal.Heuristic(vox) * 10 + MathFunctions.Rand(0.0f, 0.1f) + agent.AI.Movement.Cost(action.MoveType);

                        if (cost < minCost)
                        {
                            minAction = action;
                            minCost = cost;
                            hasMinAction = true;
                        }
                    }

                    if (hasMinAction)
                    {
                        path.Add(minAction);
                        creatureVoxel = minAction.DestinationVoxel;
                    }
                    else
                    {
                        foreach (var status in Die(agent))
                            continue;
                        yield return Act.Status.Success;
                        yield break;
                    }
                }
                if (path.Count == 0)
                {
                    foreach (var status in Die(agent))
                        continue;
                    yield return Act.Status.Success;
                    yield break;
                }
                agent.AI.Blackboard.SetData("GreedyPath", path);
                var pathAct = new FollowPathAct(agent.AI, "GreedyPath");
                pathAct.Initialize();

                foreach (Act.Status status in pathAct.Run())
                {
                    yield return Act.Status.Running;
                }
                yield return Act.Status.Running;
            }
        }

        public IEnumerable<Act.Status> Die(Creature agent)
        {
            agent.GetRoot().Delete();
            yield return Act.Status.Success;
        }

        public override Act CreateScript(Creature agent)
        {
            return new Select(
                new Sequence(new SetBlackboardData<VoxelHandle>(agent.AI, "EdgeVoxel", VoxelHandle.InvalidHandle),
                             new Repeat(
                                 new Sequence(
                                    new PlanAct(agent.AI, "PathToVoxel", "EdgeVoxel", PlanAct.PlanType.Edge) { MaxTimeouts = 1 },
                                    new FollowPathAct(agent.AI, "PathToVoxel"))
                                    , 4, true),
                             new Wrap(() => Die(agent)) { Name = "Die" }
                             ),
                new Wrap(() => GreedyFallbackBehavior(agent)) { Name = "Go to edge of world." }
                );
        }
    }
}