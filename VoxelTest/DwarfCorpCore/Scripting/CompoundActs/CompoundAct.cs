using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A compound act is an act which just wraps another act tree. (It's a collection of other acts).
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class CompoundAct : Act
    {
        public Act Tree { get; set; }

        public CompoundAct()
        {
            Children = new List<Act>();
        }

        public CompoundAct(Act tree)
        {
            Name = Tree.Name;
            Tree = tree;
        }

        public override void Initialize()
        {
            Children.Clear();
            Children.Add(Tree);
            Tree.Initialize();
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            return Tree.Run();
        }
    }

}