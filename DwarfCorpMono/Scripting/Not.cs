using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class Not : Act
    {
        Act Child { get; set; }
        public Not(Act behavior)
        {
            Name = "Not";
            Child = behavior;
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
            bool done = false;

            while (!done)
            {
                Status childStatus = Child.Tick();

                if (childStatus == Status.Running)
                {
                    yield return Status.Running;
                }
                else if (childStatus == Status.Success)
                {
                    yield return Status.Fail;
                    done = true;
                    break;
                }
                else if (childStatus == Status.Fail)
                {
                    yield return Status.Success;
                    done = true;
                    break;
                }

            }
        }
    }
}
