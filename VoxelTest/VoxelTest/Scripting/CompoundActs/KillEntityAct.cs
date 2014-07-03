using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature goes to an entity, and then hits it until the other entity is dead.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class KillEntityAct : CompoundCreatureAct
    {
        public Body Entity { get; set; }

        public KillEntityAct()
        {

        }

        public KillEntityAct(Body entity, CreatureAI creature) :
            base(creature)
        {
            Entity = entity;
            Name = "Kill Entity";
            Tree = new Sequence(new GoToEntityAct(entity, creature),
                                new MeleeAct(Agent));
        }
    }

}