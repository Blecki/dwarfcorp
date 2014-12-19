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
    [JsonObject(IsReference = true)]
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
        public static GameTime LastTime { get; set; }

        [JsonIgnore]
        public static float Dt
        {
            get { return (float) LastTime.ElapsedGameTime.TotalSeconds; }
        }


        public Act()
        {
            IsInitialized = false;
            Children = new List<Act>();
        }


        public static implicit operator Act(Func<IEnumerable<Status>> enumerator)
        {
            return enumerator.GetAct();
        }

        public static implicit operator Act(Func<bool> enumerator)
        {
            return enumerator.GetAct();
        }

        public static implicit operator Act(bool condition)
        {
            return new Condition(() => condition);
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
            if(Children != null)
                foreach (Act child in Children)
                {
                    child.OnCanceled();
                }
        }
    }

}