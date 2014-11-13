using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Wraps a boolean function so that it returns success when the function
    /// evaluates to "true", and failure otherwise.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class Condition : Act
    {
        private Func<bool> Function { get; set; }

        public Condition()
        {

        }

        public Condition(bool condition)
        {
            Name = "Condition";
            Function = () => condition;
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