using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Implements sequential composition. Runs all of its children in sequence starting
    /// from the first child each tick. Upon success, returns.
    /// Upon failure, skips to the next child. This is useful for writing robust behaviors
    /// that can have preconditions that change during every tick.
    /// It is exactly like 'Select', except it starts over from the first child every tick.
    /// </summary>
    class SeqComp : Act
    {
        public int CurrentChildIndex { get; set; }

        public Act CurrentChild
        {
            get { if (CurrentChildIndex >= 0 && CurrentChildIndex < Children.Count) { return Children[CurrentChildIndex]; } else { return null; } }
        }

        public SeqComp()
        {

        }

        public SeqComp(params Act[] children) :
            this(children.AsEnumerable())
        {
        }

        public SeqComp(IEnumerable<Act> children)
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
                if(child != null)
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
                LastTickedChild = CurrentChild;
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
                        CurrentChildIndex = 0;
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
                    CurrentChildIndex = 0;
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
