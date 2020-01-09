using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// An act is an another Name for a "Behavior". Behaviors are linked together into an "behavior tree". Each behavior is a coroutine
    /// which can either be running, succeed, or fail. 
    /// </summary>
    public class Act
    {
        public enum Status
        {
            Running,
            Fail,
            Success
        }


        public List<Act> Children { get; set; }

        public string Name = "Act";
        public bool IsInitialized { get; set; }

        [JsonIgnore]
        public IEnumerator<Status> Enumerator;

        [JsonIgnore]
        public Act LastTickedChild { get; set; }

        public bool IsCanceled { get; set; }

        public Act()
        {
            IsInitialized = false;
            Children = new List<Act>();
        }

        public static implicit operator Act(Func<bool> enumerator)
        {
            return enumerator.GetAct();
        }

        public static implicit operator Act(bool condition)
        {
            return new Condition(() => condition);
        }

        public static implicit operator Act(Act.Status status)
        {
            return new Always(status);
        }

        public static implicit operator Act(Act[] acts)
        {
            return new Sequence(acts);
        }

        public static implicit operator Act(List<Act> acts)
        {
            return new Sequence(acts.ToArray());
        }

        public static Act operator+(Func<bool> domain, Act act)
        {
            return new Domain(domain, act);
        }

        public static Act operator+(Sequence seq, Act act)
        {
            seq.Children.Add(act);
            return seq;
        }

        public static Act operator +(Select select, Act act)
        {
            select.Children.Add(act);
            return select;
        }

        public static Act operator &(Act b1, Act b2)
        {
            return new Sequence(b1, b2);
        }

        public static Act operator |(Act b1, Act b2)
        {
            return new Select(b1, b2);
        }

        public static Act operator *(Act b1, Act b2)
        {
            return new Parallel(b1, b2);
        }

        public static Act operator !(Act b1)
        {
            return new Not(b1);
        }


        public Status Tick()
        {
            if(Enumerator == null)
            {
                Initialize();
            }
            if (Enumerator != null)
            {
                Enumerator.MoveNext();
            }
            else
            {
                return Status.Fail;
            }
            return Enumerator.Current;
        }


        public virtual void Initialize()
        {
            Enumerator = Run().GetEnumerator();
            IsInitialized = true;
        }

        public virtual IEnumerable<Status> Run()
        {
            throw new NotImplementedException();
        }

        public virtual Task AsTask()
        {
            return new ActWrapperTask(this);
        }

        public virtual void OnCanceled()
        {
            IsCanceled = true;
            if(Children != null)
                foreach (Act child in Children)
                {
                    child.OnCanceled();
                }
        }
    }
}