using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    /// <summary>
    /// A creature drops the item currently in its hands.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class DropItemAct : CreatureAct
    {
        public DropItemAct(CreatureAIComponent creature) :
            base(creature)
        {
            Name = "Drop Item";
        }

        public override IEnumerable<Status> Run()
        {
            Body grabbed = Creature.Hands.GetFirstGrab();

            if(grabbed == null)
            {
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                yield return Status.Fail;
            }
            else
            {
                Creature.Hands.UnGrab(grabbed);
                Matrix m = Matrix.Identity;
                m.Translation = Creature.Physics.GlobalTransform.Translation;
                Agent.Blackboard.SetData<object>("HeldObject", null);
                grabbed.LocalTransform = m;
                grabbed.HasMoved = true;
                grabbed.IsActive = true;

                yield return Status.Success;
            }
        }
    }

}