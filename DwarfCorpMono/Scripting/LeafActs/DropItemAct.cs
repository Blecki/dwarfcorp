using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class DropItemAct : CreatureAct
    {
        public DropItemAct(CreatureAIComponent creature) :
            base(creature)
        {
            Name = "Drop Item";
        }

        public override IEnumerable<Act.Status> Run()
        {
            LocatableComponent grabbed = Creature.Hands.GetFirstGrab();

            if (grabbed == null)
            {
                yield return Act.Status.Fail;
            }
            else
            {

                Creature.Hands.UnGrab(grabbed);
                Matrix m = Matrix.Identity;
                m.Translation = Creature.Physics.GlobalTransform.Translation;
                Agent.Blackboard.SetData("HeldObject", null);
                grabbed.LocalTransform = m;
                grabbed.HasMoved = true;
                grabbed.IsActive = true;

                yield return Act.Status.Success;
            }
        }
    }
}
