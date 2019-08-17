using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// Runs all of its children in sequence until one of them fails, or all of them succeed. 
    /// Returns failure if any child fails, and success if they all succeed.
    /// </summary>
    public class Sequence : Act
    {
        public int CurrentChildIndex { get; set; }

        [JsonIgnore]
        public Act CurrentChild
        {
            get { return Children[CurrentChildIndex]; }
        }

  

        public Sequence()
        {
            
        }

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
            CurrentChildIndex = 0;
            foreach(Act child in Children)
            {
                child.Initialize();
            }

            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            if (Children == null)
            {
                yield return Status.Fail;
                yield break;
            }
            
            while(CurrentChildIndex < Children.Count)
            {
                if (CurrentChild == null)
                {
                    yield return Status.Fail;
                    yield break;
                }
                Status childStatus = CurrentChild.Tick();
                LastTickedChild = CurrentChild;
                if(childStatus == Status.Fail)
                {
                    yield return Status.Fail;
                    yield break;
                }
                else if(childStatus == Status.Success)
                {
                    CurrentChildIndex++;
                    //yield return Status.Running;
                }
                else
                {
                    yield return Status.Running;
                }
            }

            yield return Status.Success;
        }
    }

}