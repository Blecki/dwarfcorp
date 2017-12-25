// Task.cs
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

using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// A task is an abstract object which describes a goal for a creature.
    /// Tasks construct acts (or behaviors) to solve them. Tasks have costs,
    /// and can either be feasible or infeasible for a crature.
    /// </summary>
    [JsonObject(IsReference = true)]
    public abstract class Task
    {
        public enum PriorityType
        {
            Eventually = 0,
            Low = 1,
            Medium = 2,
            High = 3,
            Urgent = 4
        }

        public enum TaskCategory
        {
            None        = 0,
            Other       = 1,
            Dig         = 2,
            Chop        = 4,
            Harvest     = 8,
            Attack      = 16,
            Hunt        = 32,
            Research    = 64,
            BuildBlock  = 128,
            BuildObject = 256,
            BuildZone   = 512,
            CraftItem   = 1024,
            Cook        = 2048,
            TillSoil    = 4096,
            Gather      = 8192,
            Guard       = 16384,
            Wrangle     = 32768,
            Plant       = 65536
        }

        public TaskCategory Category { get; set; }
        public PriorityType Priority { get; set; }
        public int MaxAssignable = 1;
        public int CurrentAssigned = 0;
        public bool ReassignOnDeath = true;

        public enum Feasibility
        {
            Feasible,
            Infeasible,
            Unknown
        }

        // The script is ignored and regenerated on load.
        [JsonIgnore]
        public Act Script { get; set; }
        public bool AutoRetry = false;

        protected bool Equals(Task other)
        {
            return string.Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public string Name { get; set; }

        public abstract Task Clone();

        public virtual void Render(DwarfTime time)
        {
            
        }

        public override bool Equals(object obj)
        {
            return obj is Task && Name.Equals(((Task) (obj)).Name);
        }


        public virtual void SetupScript(Creature agent)
        {
            if(Script != null)
                Script.OnCanceled();

            Script = CreateScript(agent);
        }


        public virtual Act CreateScript(Creature agent)
        {
            return null;
        }

        public virtual float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 1.0f;
        }

        public virtual Feasibility IsFeasible(Creature agent)
        {
            return Feasibility.Feasible;
        }

        public virtual bool ShouldRetry(Creature agent)
        {
            return AutoRetry;
        }

        public virtual bool ShouldDelete(Creature agent)
        {
            return false;
        }

        public virtual void OnAssign(Creature agent)
        {
            
        }

        public virtual void OnUnAssign(Creature agent)
        {
            
        }

        public virtual void Cancel()
        {
            if (Script != null)
            {
                Script.OnCanceled();
            }
        }
    }

    public class ActWrapperTask : Task
    {
        public ActWrapperTask()
        {
            
        }


        public ActWrapperTask(Act act)
        {
            ReassignOnDeath = false;
            Script = act;
            Name = Script.Name;
        }

        public override Task Clone()
        {
            return new ActWrapperTask(Script);
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return Script != null ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override bool ShouldDelete(Creature agent)
        {
            if (Script == null)
            {
                return true;
            }
            return base.ShouldDelete(agent);
        }

        public override Act CreateScript(Creature agent)
        {
            if (Script != null)
            {
                Script.Initialize();
            }
            return Script;
        }
    }

}