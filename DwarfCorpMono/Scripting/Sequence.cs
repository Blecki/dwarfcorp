using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class Sequence : Act
    {
        public int CurrentChildIndex { get; set; }
        public Act CurrentChild { get { return Children[CurrentChildIndex]; } }

        public Sequence(params Act[] children) :
            this(children.AsEnumerable())
        {

        }

        public Sequence(IEnumerable<Act> children)
        {
            Name = "Sequence";
            Children = new List<Act>();
            Children.AddRange(children);
            CurrentChildIndex = 0;
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
            bool failed = false;
            while (CurrentChildIndex < Children.Count && !failed)
            {
                Status childStatus = CurrentChild.Tick();

                if (childStatus == Status.Fail)
                {
                    failed = true;
                    yield return Status.Fail;
                    break;
                }
                else if (childStatus == Status.Success)
                {
                    CurrentChildIndex++;
                    yield return Status.Running;
                }
                else
                {
                    yield return Status.Running;
                }

            }

            if (!failed)
            {
                yield return Status.Success;
            }
        }
    }

}
