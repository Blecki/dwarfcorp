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

    public static class BehaviorExtensions
    {
        public static Act GetAct(this Func<bool> condition)
        {
            return new Condition(condition);
        }

        public static Act GetAct(this Func<IEnumerable<Act.Status>> func)
        {
            return new Wrap(func);
        }
    }

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
            return new Condition(() => { return condition; });
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

            Enumerator.MoveNext();
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
    }

}