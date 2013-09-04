using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class Parallel : Act
    {

        public Parallel(params Act[] children) :
            this(children.AsEnumerable())
        {
        
        }

        public Parallel(IEnumerable<Act> children)
        {
            Name = "Parallel";
            Children = new List<Act>();
            Children.AddRange(children);
        }

        public override void Initialize()
        {
            foreach (Act child in Children)
            {
                child.Initialize();
            }

            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            bool allSuccess = false;

            while (!allSuccess)
            {
                bool runEncountered = false;
                foreach (Act child in Children)
                {
                    Status childStatus = child.Tick();

                    if (childStatus == Status.Fail)
                    {
                        yield return Status.Fail;
                        break;
                    }
                    else if (childStatus != Status.Success)
                    {
                        runEncountered = true;
                    }

                }

                if (!runEncountered)
                {
                    allSuccess = true;
                }
                else
                {
                    yield return Status.Running;
                }

            }

            if (allSuccess)
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
