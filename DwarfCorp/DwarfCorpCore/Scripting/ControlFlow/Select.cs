using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Runs all of its children in sequence  until one of them succeeds (or all of them fail). Returns
    /// success if any child succeds. Returns fail if all of them fail.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class Select : Act
    {
        public int CurrentChildIndex { get; set; }

        public Act CurrentChild
        {
            get { if (CurrentChildIndex >= 0 && CurrentChildIndex < Children.Count) { return Children[CurrentChildIndex]; } else { return null; } }
        }

        public Select()
        {

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