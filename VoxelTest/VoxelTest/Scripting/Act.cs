using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

    public class Act
    {
        public enum Status
        {
            Running,
            Fail,
            Success
        }

        public string Name = "Act";

        public IEnumerator<Status> Enumerator;

        public static GameTime LastTime { get; set; }

        public Act()
        {

        }

        public static implicit operator Act(Func<IEnumerable<Status> > enumerator)
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
            Enumerator.MoveNext();
            return Enumerator.Current;
        }

        public virtual void Initialize()
        {
            Enumerator = Run().GetEnumerator();
        }

        public virtual IEnumerable<Status> Run()
        {
            throw new NotImplementedException();
        }


    }
}
