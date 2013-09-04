using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class Condition : Act
    {
        Func<bool> Function { get; set; }

        public Condition(bool condition)
        {
            Name = "Condition";
            Function = new Func<bool>(() => { return condition; });
        }

        public Condition(Func<bool> condition)
        {
            Name = "Condition";
            Function = condition;
        }

        public override IEnumerable<Status> Run()
        {
            if (Function())
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
