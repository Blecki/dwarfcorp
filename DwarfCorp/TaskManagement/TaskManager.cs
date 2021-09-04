using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public class TaskManager
    {
        [JsonProperty] private List<Task> Tasks = new List<Task>();
        [JsonIgnore] public WorldManager World;

        // By returning it as an IEnumerable, we can expose it without allowing it to be modified.
        public IEnumerable<Task> EnumerateTasks()
        {
            return Tasks;
        }

        public TaskManager()
        {


        }

        public void AddTask(Task task)
        {
            if (World.Overworld.GetDefaultTaskPriority(task.Category) != TaskPriority.NotSet)
                task.Priority = World.Overworld.GetDefaultTaskPriority(task.Category);

            // TODO(mklingen): do not depend on task name as ID.
            if (!Tasks.Any(t => t.Name == task.Name))
            {
                Tasks.Add(task);
                task.OnEnqueued(World);
            }
        }

        public void AddTasks(IEnumerable<Task> tasks)
        {
            foreach(var task in tasks)
            {
                AddTask(task);
            }
        }

        public void CancelTask(MaybeNull<Task> Task)
        {
            if (Task.HasValue(out var task))
            {
                var localAssigneeList = new List<CreatureAI>(task.AssignedCreatures);
                foreach (var actor in localAssigneeList)
                    actor.RemoveTask(task);
                Tasks.RemoveAll(t => Object.ReferenceEquals(t, task));
                task.OnDequeued(World);
                task.OnCancelled(this, World);
                task.WasCancelled = true;
            }
        }

        public Task GetBestTask(CreatureAI creatureAI, int minPriority=-1)
        {
            Task best = null;
            float bestCost = float.MaxValue;
            var bestPriority = TaskPriority.Eventually;
            var creature = creatureAI.Creature;
            foreach(var task in Tasks)
            {
                if (!creature.Stats.IsTaskAllowed(task.Category))
                    continue;
                if (task.AssignedCreatures.Count >= task.MaxAssignable)
                    continue;
                if (task.IsComplete(World))
                    continue;
                if (task.IsFeasible(creature) != Feasibility.Feasible)
                    continue;
                if (task.Priority < bestPriority)
                    continue;
                if ((int)task.Priority <= minPriority)
                    continue;

                var cost = task.ComputeCost(creature) * (1 + task.AssignedCreatures.Count);

                if (cost < bestCost || task.Priority > bestPriority)
                {
                    bestCost = cost;
                    best = task;
                    bestPriority = task.Priority;
                }

            }

            if (best != null)
                return best;

            return null;
        }

        public void OnVoxelChanged(VoxelEvent e)
        {
            foreach(var task in Tasks)
            {
                task.OnVoxelChange(e);
            }
        }

        private int updateIdx = 0;
        private int workGroupSize = 2048;

        public void Update(List<CreatureAI> creatures)
        {
            for (int k = 0; k < workGroupSize; k++)
            {
                int j = updateIdx + k;
                if (j >= Tasks.Count)
                {
                    updateIdx = 0;
                    return;
                }

                Tasks[j].CleanupInactiveWorkers();

                if (Tasks[j].IsComplete(World))
                {
                    Tasks[j].OnDequeued(World);
                    Tasks.RemoveAt(j);
                    k = Math.Max(k - 1, 0);
                }
                else
                    Tasks[j].OnUpdate(World);
            }

            updateIdx += workGroupSize;
        }

        public int GetMaxColumnValue(int[,] matrix, int column, int numRows, int numColumns)
        {
            int maxValue = int.MinValue;

            for(int r = 0; r < numRows; r++)
            {
                if(matrix[r, column] > maxValue)
                {
                    maxValue = matrix[r, column];
                }
            }

            return maxValue;
        }

        public int GetMaxRowValue(int[,] matrix, int row, int numRows, int numColumns)
        {
            int maxValue = int.MinValue;

            for(int c = 0; c < numColumns; c++)
            {
                if(matrix[row, c] > maxValue)
                {
                    maxValue = matrix[row, c];
                }
            }

            return maxValue;
        }

        public static int GetMax(int[,] matrix, int numRows, int numColumns)
        {
            int maxValue = int.MinValue;

            for(int c = 0; c < numColumns; c++)
            {
                for(int row = 0; row < numRows; row++)
                {
                    if(matrix[row, c] > maxValue)
                    {
                        maxValue = matrix[row, c];
                    }
                }
            }

            return maxValue;
        }

        public static List<Task> AssignTasksGreedy(List<Task> newGoals, List<CreatureAI> creatures, int maxPerDwarf=100, int maxToAssign=-1)
        {
            if (maxToAssign < 0)
            {
                maxToAssign = newGoals.Count;
            }
            // We are going to keep track of the unassigned goal count
            // to avoid having to parse the list at the end of the loop.
            int goalsUnassigned = newGoals.Count;

            // Randomized list changed from the CreatureAI objects themselves to an index into the
            // List passed in.  This is to avoid having to shift the masterCosts list around to match
            // each time we randomize.
            List<int> randomIndex = new List<int>(creatures.Count);
            for (int i = 0; i < creatures.Count; i++)
            {
                randomIndex.Add(i);
            }

            // We create the comparer outside of the loop.  It gets reused for each sort.
            CostComparer toCompare = new CostComparer();

            // One of the biggest issues with the old function was that it was recalculating the whole task list for
            // the creature each time through the loop, using one item and then throwing it away.  Nothing changed
            // in how the calculation happened between each time so we will instead make a costs list for each creature
            // and keep them all.  This not only avoids rebuilding the list but the sheer KeyValuePair object churn there already was.
            List<List<KeyValuePair<int, float>>> masterCosts = new List<List<KeyValuePair<int, float>>>(creatures.Count);
            List<int> creatureTaskCounts = new List<int>();

            // We will set this up in the next loop rather than make it's own loop.
            List<int> costsPositions = new List<int>(creatures.Count);
            for (int costIndex = 0; costIndex < creatures.Count; costIndex++)
            {
                creatureTaskCounts.Add(creatures[costIndex].CountFeasibleTasks(TaskPriority.Eventually));
                List<KeyValuePair<int, float>> costs = new List<KeyValuePair<int, float>>();
                CreatureAI creature = creatures[costIndex];

                // We already were doing an index count to be able to make the KeyValuePair for costs
                // and foreach uses Enumeration which is slower.
                for (int i = 0; i < newGoals.Count; i++)
                {
                    Task task = newGoals[i];
                    // We are checking for tasks the creature is already assigned up here to avoid having to check
                    // every task in the newGoals list against every task in the newGoals list.  The newGoals list
                    // should reasonably not contain any task duplicates.
                    if (creature.Tasks.Contains(task)) continue;

                    float cost = 0;
                    // We've swapped the order of the two checks to take advantage of a new ComputeCost that can act different
                    // if we say we've already called IsFeasible first.  This allows us to skip any calculations that are repeated in both.
                    if (task.IsFeasible(creature.Creature) == Feasibility.Infeasible)
                    {
                        cost += 1e10f;
                    }
                    cost += task.ComputeCost(creature.Creature, true);
                    cost += creature.Tasks.Sum(existingTask => existingTask.ComputeCost(creature.Creature, true));
                    costs.Add(new KeyValuePair<int, float>(i, cost));
                }
                // The sort lambda function has been replaced by an IComparer class.
                // This is faster but I mainly did it because VS can not Edit & Continue
                // any function with a Lambda function in it which was slowing down dev time.
                costs.Sort(toCompare);

                masterCosts.Add(costs);
                costsPositions.Add(0);
            }


            // We are going to precalculate the maximum iterations and count down
            // instead of up.
            int iters = goalsUnassigned * creatures.Count;
            int numAssigned = 0;
            while (goalsUnassigned > 0 && iters > 0 && numAssigned < maxToAssign)
            {
                randomIndex.Shuffle();
                iters--;
                for (int creatureIndex = 0; creatureIndex < randomIndex.Count; creatureIndex++)
                {
                    int randomCreature = randomIndex[creatureIndex];
                    CreatureAI creature = creatures[randomCreature];

                    List<KeyValuePair<int, float>> costs = masterCosts[randomCreature];
                    int costPosition = costsPositions[randomCreature];
                    // This loop starts with the previous spot we stopped.  This avoids us having to constantly run a task we
                    // know we have processed.
                    for (int i = costPosition; i < costs.Count; i++)
                    {
                        // Incremented at the start in case we find a task and break.
                        costPosition++;

                        KeyValuePair<int, float> taskCost = costs[i];
                        // We've swapped the checks here.  Tasks.Contains is far more expensive so being able to skip
                        // if it's going to fail the maxPerGoal check anyways is very good.
                        if (newGoals[taskCost.Key].AssignedCreatures.Count < newGoals[taskCost.Key].MaxAssignable && 
                            !creature.Tasks.Contains(newGoals[taskCost.Key]) && 
                            (creatureTaskCounts[randomCreature] < maxPerDwarf || newGoals[taskCost.Key].Priority >= TaskPriority.High) &&
                            newGoals[taskCost.Key].IsFeasible(creature.Creature) == Feasibility.Feasible)
                        {
                            
                            // We have to check to see if the task we are assigning is fully unassigned.  If so 
                            // we reduce the goalsUnassigned count.  If it's already assigned we skip it.
                            if (newGoals[taskCost.Key].AssignedCreatures.Count == 0) goalsUnassigned--;

                            creature.AssignTask(newGoals[taskCost.Key]);
                            creatureTaskCounts[randomCreature]++;
                            numAssigned++;
                            break;
                        }
                    }
                    // We have to set the position we'll start the loop at the next time based on where we found
                    // our task.
                    costsPositions[randomCreature] = costPosition;
                    // The loop at the end to see if all are unassigned is gone now, replaced by a countdown
                    // variable: goalsUnassigned.
                }

            }

            List<Task> unassigned = new List<Task>();
            for (int i = 0; i < newGoals.Count; i++)
            {
                if (newGoals[i].AssignedCreatures.Count < newGoals[i].MaxAssignable)
                {
                    unassigned.Add(newGoals[i]);
                }
            }
            return unassigned;
        }

        internal bool HasTask(Task task)
        {
            return Tasks.Contains(task);
        }

        public static List<Task> AssignTasks(List<Task> newGoals, List<CreatureAI> creatures, int maxPerDwarf=100)
        {

            if(newGoals.Count == 0 || creatures.Count == 0)
            {
                return newGoals;
            }

            List<Task> unassignedGoals = new List<Task>();
            unassignedGoals.AddRange(newGoals);
            int numFeasible = 1;
            while(unassignedGoals.Count > 0 && numFeasible > 0)
            {
                numFeasible = 0;
                int[] assignments = CalculateOptimalAssignment(unassignedGoals, creatures);
                List<Task> removals = new List<Task>();
                for(int i = 0; i < creatures.Count; i++)
                {
                    int assignment = assignments[i];

                    if (assignment >= unassignedGoals.Count || creatures[i].IsDead 
                        || unassignedGoals[assignment].IsFeasible(creatures[i].Creature) != Feasibility.Feasible ||
                        creatures[i].CountFeasibleTasks(unassignedGoals[assignment].Priority) >=  maxPerDwarf)
                    {
                        continue;
                    }
                    numFeasible++;
                    creatures[i].AssignTask(unassignedGoals[assignment]);
                    removals.Add(unassignedGoals[assignment]);
                }

                foreach(Task removal in removals)
                {
                    unassignedGoals.Remove(removal);
                }
            }
            return unassignedGoals;
        }

        public static int[] CalculateOptimalAssignment(List<Task> newGoals, List<CreatureAI> agents )
        {
            int numGoals = newGoals.Count;
            int numAgents = agents.Count;
            int maxSize = Math.Max(numGoals, numAgents);

            int[,] goalMatrix = new int[maxSize, maxSize];
            const float multiplier = 100;

            if (numGoals == 0 || numAgents == 0)
            {
                return null;
            }

            for (int goalIndex = 0; goalIndex < numGoals; goalIndex++)
            {
                Task goal = newGoals[goalIndex];

                for (int agentIndex = 0; agentIndex < numAgents; agentIndex++)
                {
                    CreatureAI agent = agents[agentIndex];
                    float floatCost = goal.ComputeCost(agent.Creature);

                    int cost = (int)(floatCost * multiplier);

                    if (goal.IsFeasible(agent.Creature) == Feasibility.Infeasible)
                    {
                        cost += 99999;
                    }

                    if (agent.Creature.Stats.IsAsleep || agent.IsDead || agent.GetRoot().IsDead)
                    {
                        cost += 99999;
                    }

                    cost += agents[agentIndex].Tasks.Count;

                    goalMatrix[agentIndex, goalIndex] = cost;
                }
            }

            // Add additional columns or rows
            if (numAgents > numGoals)
            {
                int maxValue = GetMax(goalMatrix, numAgents, numGoals) + 1;
                for (int dummyGoal = numGoals; dummyGoal < maxSize; dummyGoal++)
                {
                    for (int i = 0; i < numAgents; i++)
                    {
                        // If we have more agents than goals, we need to add additional fake goals
                        // Since goals are in columns, we are essentially adding a new column.
                        goalMatrix[i, dummyGoal] = maxValue;
                    }
                }
            }
            else if (numGoals > numAgents)
            {
                int maxValue = GetMax(goalMatrix, numAgents, numGoals) + 1;
                for (int dummyAngent = numAgents; dummyAngent < maxSize; dummyAngent++)
                {
                    for (int i = 0; i < numGoals; i++)
                    {
                        // If we have more goals than agents, we need to add additional fake agents
                        // Since goals are in columns, we are essentially adding a new row.
                        goalMatrix[dummyAngent, i] = maxValue;
                    }
                }
            }

            return goalMatrix.FindAssignments();

        }

        public class CostComparer : IComparer<KeyValuePair<int, float>>
        {
            public int Compare(KeyValuePair<int, float> pairA, KeyValuePair<int, float> pairB)
            {
                if (pairA.Key == pairB.Key)
                {
                    return 0;
                }
                else return pairA.Value.CompareTo(pairB.Value);
            }
        }
    }

}