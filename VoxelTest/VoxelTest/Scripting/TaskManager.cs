using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class TaskManager
    {
        public GameMaster Master;
        public int MaxTasks = 10;

        public class Task
        {
            public Goal Goal;
            public GOAP Agent;

            public Task(Goal goal, GOAP agent)
            {
                Goal = goal;
                Agent = agent;
            }
        }

        public Dictionary<GOAP, Queue<Task>> TaskQueue { get; set; }

        public TaskManager(GameMaster master)
        {
            Master = master;
            TaskQueue = new Dictionary<GOAP, Queue<Task>>();
        }

        public bool TaskIsAssigned(Goal goal)
        {
            foreach(KeyValuePair<GOAP, Queue<Task>> assignment in TaskQueue)
            {
                foreach(Task t in assignment.Value)
                {
                    if(t.Goal.Equals(goal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<Goal> CreateGoals()
        {
            List<Goal> goals = new List<Goal>();

            if(Master.Stockpiles.Count > 0)
            {
                goals.AddRange(Master.GatherDesignations.Select(i => new GatherItem(null, i)).Where(g => !TaskIsAssigned(g)));
            }

            foreach(GameMaster.Designation i in Master.DigDesignations)
            {
                if (i.vox == null || i.vox.GetVoxel(Master.Chunks, true).Health <= 0)
                {
                    continue;
                }

                VoxelChunk chunk = Master.Chunks.GetVoxelChunkAtWorldLocation(i.vox.WorldPosition);

                if(chunk != null)
                {
                    if(chunk.IsCompletelySurrounded(i.vox))
                    {
                        continue;
                    }
                }


                Goal g = new KillVoxel(null, i.vox);

                if(!TaskIsAssigned(g))
                {
                    goals.Add(g);
                }
            }

            goals.AddRange(Master.GuardDesignations.Select(i => new GuardVoxel(null, i.vox)).Where(g => !TaskIsAssigned(g)));

            goals.AddRange(Master.ChopDesignations.Select(i => new KillEntity(null, i)).Where(g => !TaskIsAssigned(g)));

            if(Master.Stockpiles.Count <= 0)
            {
                return goals;
            }

            foreach(RoomBuildDesignation buildDesignation in Master.RoomDesignator.BuildDesignations)
            {
                if(buildDesignation.IsBuilt)
                {
                    continue;
                }

                HashSet<string> strings = new HashSet<string>();
                foreach(string item in buildDesignation.ToBuild.RoomType.RequiredResources.Keys)
                {
                    if(buildDesignation.IsResourceSatisfied(item))
                    {
                        continue;
                    }

                    strings.Add(item);
                    break;
                }

                if(strings.Count == 0)
                {
                    continue;
                }

                Goal g = new PutItemWithTag(null, new TagList(strings), buildDesignation.ToBuild);


                if(!TaskIsAssigned(g))
                {
                    goals.Add(g);
                }
            }


            foreach(PutDesignation put in Master.PutDesignator.Designations)
            {
                TagList tags = new TagList(put.type.resourceToRelease);

                bool foundCandidateItem = Master.Stockpiles.Select(s => s.FindItemWithTags(tags.Tags)).Any(i => i != null);

                if(!foundCandidateItem)
                {
                    continue;
                }
                Goal g = (new BuildVoxel(null, new TagList(put.type.resourceToRelease), put.vox, put.type));

                if(!TaskIsAssigned(g))
                {
                    goals.Add(g);
                }
            }

            foreach(GameMaster.ShipDesignation ship in Master.ShipDesignations)
            {
                List<LocatableComponent> componentsToShip = new List<LocatableComponent>();
                int remaining = ship.GetRemainingNumResources();

                if(remaining == 0)
                {
                    continue;
                }


                foreach(Stockpile s in Master.Stockpiles)
                {
                    for(int i = componentsToShip.Count; i < remaining; i++)
                    {
                        LocatableComponent r = s.FindItemWithTag(ship.Resource.ResourceType.ResourceName, componentsToShip);

                        if(r != null)
                        {
                            componentsToShip.Add(r);
                        }
                    }
                }

                foreach(LocatableComponent loc in componentsToShip)
                {
                    if(!ship.Port.ContainsItem(loc))
                    {
                        Goal g = new PutItemInZone(null, Item.FindItem(loc), ship.Port);
                        if(!TaskIsAssigned(g))
                        {
                            ship.Assignments.Add(g);
                            goals.Add(g);
                        }
                    }
                }
            }

            return goals;
        }

        public void ManageTasks()
        {
            int i = 0;
            foreach(KeyValuePair<GOAP, Queue<Task>> assignment in TaskQueue)
            {
                if(assignment.Value.Count > 0)
                {
                    Task task = assignment.Value.Peek();

                    if(!assignment.Key.Goals.ContainsKey(task.Goal.Name))
                    {
                        task.Goal.Agent = assignment.Key;
                        assignment.Key.AddGoal(task.Goal);
                        assignment.Value.Dequeue();
                    }
                    else
                    {
                        assignment.Value.Dequeue();
                    }
                }
                i++;
            }


            foreach(CreatureAIComponent minion in Master.Minions.Where(minion => minion.Goap.Goals.Count == 0))
            {
                minion.Goap.AddGoal(new LookInteresting(minion.Goap));
            }
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

        public int GetMax(int[,] matrix, int numRows, int numColumns)
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

        public void AssignTasks()
        {
            List<Goal> newGoals = CreateGoals();

            int numGoals = Math.Min(newGoals.Count, MaxTasks);
            int numAgents = Master.Minions.Count;
            int maxSize = Math.Max(numGoals, numAgents);

            int[,] GoalMatrix = new int[maxSize, maxSize];
            float multiplier = 100;

            if(numGoals == 0 || numAgents == 0)
            {
                return;
            }

            for(int goalIndex = 0; goalIndex < numGoals; goalIndex++)
            {
                Goal goal = newGoals[goalIndex];

                for(int agentIndex = 0; agentIndex < numAgents; agentIndex++)
                {
                    CreatureAIComponent agent = Master.Minions[agentIndex];
                    goal.ContextReweight(agent);


                    int cost = (int) (goal.Cost * multiplier);

                    if(TaskQueue.ContainsKey(agent.Goap))
                    {
                        cost += TaskQueue[agent.Goap].Count;
                    }

                    if(!goal.ContextValidate(agent))
                    {
                        cost += 99999;
                    }

                    GoalMatrix[agentIndex, goalIndex] = cost;
                }
            }

            // Add additional columns or rows
            if(numAgents > numGoals)
            {
                int maxValue = GetMax(GoalMatrix, numAgents, numGoals) + 1;
                for(int dummyGoal = numGoals; dummyGoal < maxSize; dummyGoal++)
                {
                    for(int i = 0; i < numAgents; i++)
                    {
                        // If we have more agents than goals, we need to add additional fake goals
                        // Since goals are in columns, we are essentially adding a new column.
                        GoalMatrix[i, dummyGoal] = maxValue;
                    }
                }
            }
            else if(numGoals > numAgents)
            {
                int maxValue = GetMax(GoalMatrix, numAgents, numGoals) + 1;
                for(int dummyAngent = numAgents; dummyAngent < maxSize; dummyAngent++)
                {
                    for(int i = 0; i < numGoals; i++)
                    {
                        // If we have more goals than agents, we need to add additional fake agents
                        // Since goals are in columns, we are essentially adding a new row.
                        GoalMatrix[dummyAngent, i] = maxValue;
                    }
                }
            }

            int[] assignments = GoalMatrix.FindAssignments();

            for(int i = 0; i < numAgents; i++)
            {
                int assignment = assignments[i];

                if(assignment >= numGoals)
                {
                    continue;
                }

                if(!TaskQueue.ContainsKey(Master.Minions[i].Goap))
                {
                    TaskQueue.Add(Master.Minions[i].Goap, new Queue<Task>());
                }
                TaskQueue[Master.Minions[i].Goap].Enqueue(new Task(newGoals[assignments[i]], Master.Minions[i].Goap));
            }
        }
    }

}