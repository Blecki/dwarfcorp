using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class CompoundCreatureAct : CreatureAct
    {
        public Act Tree { get; set; }

        public CompoundCreatureAct(CreatureAIComponent creature) :
            base(creature)
        {
            Name = "CompoundCreatureAct";
            Tree = null;
        }

        public override void Initialize()
        {
            if (Tree != null)
            {
                Tree.Initialize();
            }

            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            return Tree.Run();
        }

    }
}
