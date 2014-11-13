using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Wraps an act in a loop which occurs over a fixed number of iterations.
    /// Restarts its child on each iteration.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class ForLoop : Act
    {
        public Act Child { get; set; }
        public int Iters { get; set; }
        public bool BreakOnSuccess { get; set; }

        public ForLoop(Act child, int iters, bool breakOnSuccess)
        {
            Name = "For(" + iters + ")";
            Iters = iters;
            Child = child;
            BreakOnSuccess = breakOnSuccess;
        }

        public override void Initialize()
        {
            Children.Clear();
            Children.Add(Child);
            Child.Initialize();
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            bool failEncountered = false;
            for(int i = 0; i < Iters; i++)
            {
                Child.Initialize();

                while(true)
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
                        break;
                    }
                    else
                    {
                        yield return Status.Running;
                    }
                }

                if(!failEncountered && BreakOnSuccess)
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