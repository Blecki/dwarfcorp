﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class CompoundCreatureAct : CreatureAct
    {
        public Act Tree { get; set; }

        public CompoundCreatureAct(CreatureAI creature) :
            base(creature)
        {
            Name = "CompoundCreatureAct";
            Tree = null;
        }

        public CompoundCreatureAct()
        {

        }

        public override void Initialize()
        {
            if(Tree != null)
            {
                Children.Clear();
                Tree.Initialize();
                Children.Add(Tree);
            }

            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            if(Tree == null)
                yield return Status.Fail;
            else
                foreach(Status s in Tree.Run())
                {
                    LastTickedChild = Tree.LastTickedChild;
                    yield return s;
                }
        }
    }
}