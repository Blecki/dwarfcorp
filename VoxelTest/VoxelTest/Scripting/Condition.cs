using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class Condition : Act
    {
        private Func<bool> Function { get; set; }

        public Condition(bool condition)
        {
            Name = "Condition";
            Function = () => { return condition; };
        }

        public Condition(Func<bool> condition)
        {
            Name = "Condition: " + condition.Method.Name;
            Function = condition;
        }

        public override IEnumerable<Status> Run()
        {
            if(Function())
            {
                yield return Status.Success;
            }
            else
            {
                yield return Status.Fail;
            }
        }
    }

}