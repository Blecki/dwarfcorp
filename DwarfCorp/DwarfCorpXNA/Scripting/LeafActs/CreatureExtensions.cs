// CreatureExtensions.cs
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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    public static class CreatureExtensions
    {
        public static IEnumerable<Act.Status> ClearBlackboardData(this Creature agent, string data)
        {
            if(data == null)
            {
                yield return Act.Status.Fail;
            }
            else
            {
                agent.AI.Blackboard.Erase(data);
                yield return Act.Status.Success;
            }
        }

        public static IEnumerable<Act.Status> FindAndReserve(this Creature agent, string tag, string thing)
        {
            Body closestItem = agent.Faction.FindNearestItemWithTags(tag, agent.AI.Position, true, agent.AI);

            if (closestItem != null)
            {
                //PlayState.AnnouncementManager.Announce("Creature " + agent.GlobalID + " reserves " + closestItem.Name + " " + closestItem.GlobalID, "");
                closestItem.ReservedFor = agent.AI;
                agent.AI.Blackboard.Erase(thing);
                agent.AI.Blackboard.SetData(thing, closestItem);
                yield return Act.Status.Success;
                yield break;
            }

            yield return Act.Status.Fail;
        }

        public static IEnumerable<Act.Status> Reserve(this Creature agent, string thing)
        {
            Body objectToHit = agent.AI.Blackboard.GetData<Body>(thing);

            if (objectToHit != null && objectToHit.ReservedFor == null && !objectToHit.IsReserved)
            {
                //PlayState.AnnouncementManager.Announce("Creature " + agent.GlobalID + " reserves " + objectToHit.Name + " " + objectToHit.GlobalID, "");
                objectToHit.ReservedFor = agent.AI;
            }

            yield return Act.Status.Success;
        }

        public static IEnumerable<Act.Status> Unreserve(this Creature agent, string thing)
        {
            if (String.IsNullOrEmpty(thing))
            {
                yield return Act.Status.Success;
                yield break;
            }
            Body objectToHit = agent.AI.Blackboard.GetData<Body>(thing);

            if (objectToHit != null && objectToHit.ReservedFor == agent.AI)
            {
                //PlayState.AnnouncementManager.Announce("Creature " + agent.GlobalID + " unreserves " + objectToHit.Name + " " + objectToHit.GlobalID, "");
                objectToHit.ReservedFor = null;
            }

            yield return Act.Status.Success;
            yield break;
        }

        public static void RestockAllImmediately(this Creature agent)
        {
            Dictionary<string, ResourceAmount> aggregatedResources = new Dictionary<string, ResourceAmount>();
            foreach (var resource in agent.Inventory.Resources)
            {
                if (!resource.MarkedForRestock || agent.AI.GatherManager.StockOrders.Count == 0)
                {
                    if (!resource.MarkedForUse)
                    {
                        resource.MarkedForRestock = true;

                        if (!aggregatedResources.ContainsKey(resource.Resource))
                        {
                            aggregatedResources[resource.Resource] = new ResourceAmount(resource.Resource, 0);
                        }
                        aggregatedResources[resource.Resource].NumResources++;
                    }

                }
            }

            foreach(var resource in aggregatedResources)
            {
                var task = new StockResourceTask(resource.Value.CloneResource())
                {
                    Priority = Task.PriorityType.High
                };

                if (!agent.AI.Tasks.Contains(task))
                {
                    agent.AI.AssignTask(task);
                }
            }
        }

        public static IEnumerable<Act.Status> RestockAll(this Creature agent)
        {
            Dictionary<string, ResourceAmount> aggregatedResources = new Dictionary<string, ResourceAmount>();
            foreach (var resource in agent.Inventory.Resources)
            {
                resource.MarkedForRestock = true;

                if (!aggregatedResources.ContainsKey(resource.Resource))
                {
                    aggregatedResources[resource.Resource] = new ResourceAmount(resource.Resource, 0);
                }
                aggregatedResources[resource.Resource].NumResources++;
            }

            foreach (var resource in aggregatedResources)
            {
                var task = new StockResourceTask(resource.Value.CloneResource());
                if (task.IsFeasible(agent) == Task.Feasibility.Feasible && !agent.AI.Tasks.Contains(task))
                {
                    agent.AI.AssignTask(task);
                }
            }
            yield return Act.Status.Success;
        }
    }
}
