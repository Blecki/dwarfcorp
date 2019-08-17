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
            LastTickedChild = this;
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

    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class Domain : Act
    {
        private Func<bool> Function { get; set; }
        public Act Child { get; set; }
        public Domain()
        {

        }

        public Domain(bool condition, Act child)
        {
            Name = "Domain";
            Function = () => condition;
            Child = child;
        }

        public Domain(Func<bool> condition, Act child)
        {
            Name = "Domain: " + condition.Method.Name;
            Function = condition;
            Child = child;
        }

        public override void Initialize()
        {
            Child.Initialize();
            base.Initialize();
        }

        public override void OnCanceled()
        {
            Child.OnCanceled();
            base.OnCanceled();
        }

        public override IEnumerable<Status> Run()
        {
            LastTickedChild = this;
            while (true)
            {
                if (Function())
                {
                    var childStatus = Child.Tick();
                    LastTickedChild = Child;
                    if (childStatus == Act.Status.Running)
                    {
                        yield return Act.Status.Running;
                        continue;
                    }

                    yield return childStatus;
                    yield break;
                }
                else
                {
                    yield return Status.Fail;
                }
            }
        }
    }

}