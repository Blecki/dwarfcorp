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
                agent.AI.SetMessage("Failed to clear blackboard data because it was null.");
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
            GameComponent closestItem = agent.Faction.FindNearestItemWithTags(tag, agent.AI.Position, true, agent.AI);

            if (closestItem != null)
            {
                //PlayState.AnnouncementManager.Announce("Creature " + agent.GlobalID + " reserves " + closestItem.Name + " " + closestItem.GlobalID, "");
                closestItem.ReservedFor = agent.AI;
                agent.AI.Blackboard.Erase(thing);
                agent.AI.Blackboard.SetData(thing, closestItem);
                yield return Act.Status.Success;
                yield break;
            }
            agent.AI.SetMessage(String.Format("Failed to reserve item with tag {0}, no items.", tag));
            yield return Act.Status.Fail;
        }

        public static IEnumerable<Act.Status> Reserve(this Creature agent, string thing)
        {
            GameComponent objectToHit = agent.AI.Blackboard.GetData<GameComponent>(thing);

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
            GameComponent objectToHit = agent.AI.Blackboard.GetData<GameComponent>(thing);

            if (objectToHit != null && objectToHit.ReservedFor == agent.AI)
            {
                //PlayState.AnnouncementManager.Announce("Creature " + agent.GlobalID + " unreserves " + objectToHit.Name + " " + objectToHit.GlobalID, "");
                objectToHit.ReservedFor = null;
            }

            yield return Act.Status.Success;
            yield break;
        }

        public static IEnumerable<Act.Status> RestockAll(this Creature agent)
        {
            AssignRestockAllTasks(agent, TaskPriority.Medium, false);
            yield return Act.Status.Success;
        }

        public static void AssignRestockAllTasks(this Creature agent, TaskPriority Priority, bool IgnoreMarks)
        {
            var aggregatedResources = new Dictionary<string, ResourceAmount>();

            foreach (var resource in agent.Inventory.Resources)
            {
                if (!IgnoreMarks && resource.MarkedForUse)
                    continue;

                resource.MarkedForRestock = true;

                if (!aggregatedResources.ContainsKey(resource.Resource))
                    aggregatedResources[resource.Resource] = new ResourceAmount(resource.Resource, 0);

                aggregatedResources[resource.Resource].Count++;
            }

            foreach (var resource in aggregatedResources)
            {
                var task = new StockResourceTask(resource.Value.CloneResource())
                {
                    Priority = Priority
                };

                if (task.IsFeasible(agent) == Feasibility.Feasible && !agent.AI.Tasks.Contains(task))
                    agent.AI.AssignTask(task);
            }
        }
    }
}
