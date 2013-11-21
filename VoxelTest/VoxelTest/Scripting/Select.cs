using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class Select : Act
    {
        public int CurrentChildIndex { get; set; }

        public Act CurrentChild
        {
            get { return Children[CurrentChildIndex]; }
        }

        public Select(params Act[] children) :
            this(children.AsEnumerable())
        {
        }

        public Select(IEnumerable<Act> children)
        {
            Name = "Select";
            Children = new List<Act>();
            Children.AddRange(children);
            CurrentChildIndex = 0;
        }

        public override void Initialize()
        {
            CurrentChildIndex = 0;
            foreach(Act child in Children)
            {
                child.Initialize();
            }

            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            bool failed = false;
            while(CurrentChildIndex < Children.Count)
            {
                Status childStatus = CurrentChild.Tick();

                if(childStatus == Status.Fail)
                {
                    CurrentChildIndex++;

                    if(Children.Count <= CurrentChildIndex)
                    {
                        failed = true;
                        yield return Status.Fail;
                        break;
                    }
                    else
                    {
                        yield return Status.Running;
                    }
                }
                else if(childStatus == Status.Success)
                {
                    CurrentChildIndex++;
                    yield return Status.Success;
                    break;
                }
                else
                {
                    yield return Status.Running;
                }
            }

            if(failed)
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