using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Repeatedly runs a child until a condition is true. Returns failure
    /// if the child fails. Returns success when the condition is true, and the child succeeds.
    /// Otherwise returns running.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class WhileLoop : Act
    {
        public Act Child { get; set; }
        public Act Condition { get; set; }

        public WhileLoop(Act child, Act condition)
        {
            Name = "While : " + condition.Name;
            Condition = condition;
            Child = child;
        }

        public override void Initialize()
        {
            Children.Clear();
            Children.Add(Child);
            Child.Initialize();
            Condition.Initialize();
            base.Initialize();
        }

        public bool CheckCondition()
        {
            Condition.Initialize();
            Status conditionStatus = Condition.Tick();
            return conditionStatus != Status.Fail;
        }

        public override IEnumerable<Status> Run()
        {
            bool failEncountered = false;
            while(CheckCondition())
            {
                Child.Initialize();

                bool childDone = false;

                while(!childDone && CheckCondition())
                {
                    Status childStatus = Child.Tick();

                    if(childStatus == Status.Fail)
                    {
                        failEncountered = true;
                        yield return Status.Fail;
                        break;
                    }
                    else if(childStatus == Status.Success)
                    {
                        yield return Status.Running;
                        childDone = true;
                        break;
                    }
                    else
                    {
                        yield return Status.Running;
                    }
                }

                if(failEncountered)
                {
                    break;
                }
            }

            if(failEncountered)
            {
                yield return Status.Fail;
            }
            else
            {
                yield return Status.Success;
            }
        }
    }

}