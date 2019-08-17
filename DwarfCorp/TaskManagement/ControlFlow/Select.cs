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
        }

        public override void Initialize()
        {
            foreach(Act child in Children)
                if(child != null)
                    child.Initialize();

            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            int childIndex = 0;
            while (childIndex < Children.Count)
            {
                switch (Children[childIndex].Tick())
                {
                    case Status.Fail:
                        childIndex += 1;
                        break;
                    case Status.Running:
                        yield return Status.Running;
                        break;
                    case Status.Success:
                        yield return Status.Success;
                        yield break;
                }
            }

            yield return Status.Fail;
        }
    }

}