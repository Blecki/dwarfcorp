using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class SequentialTask : Task
    {
        [JsonProperty] private List<Task> SubTasks = new List<Task>();
        [JsonProperty] private int CurrentTaskIndex = -1;

        private Task CurrentTask => SubTasks[CurrentTaskIndex];
        
        public SequentialTask(String Name, TaskCategory Category, TaskPriority Priority)
        {
            this.Name = System.Guid.NewGuid().ToString(); // OMG
            //this.NameFormat = Name + "{0} (Step {1} of {2})";
            this.Category = Category;
            this.Priority = Priority;
        }

        public void AddSubTask(Task Task)
        {
            SubTasks.Add(Task);
        }

        public void AddSubTasks(IEnumerable<Task> Tasks)
        {
            SubTasks.AddRange(Tasks);
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
        {
            throw new InvalidOperationException();
        }

        public override bool ShouldRetry(Creature agent)
        {
            return false;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return Feasibility.Infeasible;
        }

        public override bool ShouldDelete(Creature agent)
        {
            return CurrentTaskIndex >= SubTasks.Count;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return float.PositiveInfinity;
        }

        public override bool IsComplete(WorldManager World)
        {
            return CurrentTaskIndex >= SubTasks.Count;
        }

        public override void OnCancelled(TaskManager Manager, WorldManager World)
        {
            Manager.CancelTask(CurrentTask);
        }

        public override void OnUpdate(WorldManager World)
        {
            if (SubTasks.Count == 0)
                throw new InvalidProgramException("SequentialTask was allowed to update without having any sub tasks.");

            if (CurrentTaskIndex == -1 || CurrentTask.IsComplete(World))
            {
                CurrentTaskIndex += 1;
                if (CurrentTaskIndex < SubTasks.Count)
                {
                    World.TaskManager.AddTask(CurrentTask);
                    Priority = CurrentTask.Priority;
                    Category = CurrentTask.Category;
                }
            }
           
            if (CurrentTaskIndex < SubTasks.Count)
            {
                if (CurrentTask.WasCancelled)
                    World.TaskManager.CancelTask(this);

                Name = String.Format("{0} (Step {1} of {2})", CurrentTask.Name, CurrentTaskIndex + 1, SubTasks.Count);
                CurrentTask.Priority = Priority;
            }
        }

        public override Vector3? GetCameraZoomLocation()
        {
            return CurrentTask.GetCameraZoomLocation();           
        }
    }
}