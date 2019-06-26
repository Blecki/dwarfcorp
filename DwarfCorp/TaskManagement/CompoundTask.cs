using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class CompoundTask : Task
    {
        private List<Task> SubTasks = new List<Task>();
        private String NameFormat;

        public CompoundTask(String Name, TaskCategory Category, PriorityType Priority)
        {
            this.NameFormat = Name + " ({0} left)";
            this.Name= String.Format(NameFormat, 0);
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

        public override Act CreateScript(Creature creature)
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

        private void Cleanup(WorldManager World)
        {
            SubTasks.RemoveAll(t => t.IsComplete(World));
        }

        public override bool ShouldDelete(Creature agent)
        {
            Cleanup(agent.World);
            return SubTasks.Count == 0;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return float.PositiveInfinity;
        }

        public override bool IsComplete(WorldManager World)
        {
            Cleanup(World);
            return SubTasks.Count == 0;
        }

        public override void OnCancelled(TaskManager Manager, WorldManager World)
        {
            Cleanup(World);
            foreach (var task in SubTasks)
                Manager.CancelTask(task);
        }

        public override void OnUpdate()
        {
            Name = String.Format(NameFormat, SubTasks.Count);
            foreach (var sub in SubTasks)
                sub.Priority = Priority;
        }
    }
}