using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class ForLoop : Act
    {
        public Act Child { get; set; }
        public int Iters { get; set; }

        public ForLoop(Act child, int iters)
        {
            Name = "For(" + iters + ")";
            Iters = iters;
            Child = child;
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
            for (int i = 0; i < Iters; i++)
            {
                Child.Initialize();

                bool childDone = false;
                while (!childDone)
                {
                    Status childStatus = Child.Tick();

                    if (childStatus == Status.Fail)
                    {
                        failEncountered = true;
                        yield return Status.Fail;
                        break;
                    }
                    else if (childStatus == Status.Success)
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
            }

            if (failEncountered)
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
