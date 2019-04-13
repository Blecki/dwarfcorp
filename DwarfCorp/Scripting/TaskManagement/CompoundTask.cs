// KillVoxelTask.cs
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

        private void Cleanup(Faction Faction)
        {
            SubTasks.RemoveAll(t => t.IsComplete(Faction));
        }

        public override bool ShouldDelete(Creature agent)
        {
            Cleanup(agent.Faction);
            return SubTasks.Count == 0;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return float.PositiveInfinity;
        }

        public override bool IsComplete(Faction faction)
        {
            Cleanup(faction);
            return SubTasks.Count == 0;
        }

        public override void OnCancelled(TaskManager Manager, Faction Faction)
        {
            Cleanup(Faction);
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