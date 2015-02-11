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
        public KillEntityTask.KillType Mode { get; set; }

        public KillEntityAct()
        {

        }

        public IEnumerable<Act.Status> Verify()
        {
            while (true)
            {
                switch (Mode)
                {
                    case KillEntityTask.KillType.Auto:
                    {
                        if (Entity != null && !Entity.IsDead) yield return Act.Status.Running;
                        else yield return Act.Status.Fail;
                        break;
                    }
                    case KillEntityTask.KillType.Attack:
                    {
                        if (Entity != null && !Entity.IsDead && Creature.Faction.AttackDesignations.Contains(Entity))
                            yield return Act.Status.Running;
                        else yield return Act.Status.Fail;
                        break;
                    }
                    case KillEntityTask.KillType.Chop:
                    {
                        if (Entity != null && !Entity.IsDead && Creature.Faction.ChopDesignations.Contains(Entity))
                            yield return Act.Status.Running;
                        else yield return Act.Status.Fail;
                        break;
                    }
                }
            }
        }

        public KillEntityAct(Body entity, CreatureAI creature, KillEntityTask.KillType mode) :
            base(creature)
        {
            Mode = mode;
            Entity = entity;
            Name = "Kill Entity";
            Tree = new ForLoop(new Parallel(new Sequence(new GoToEntityAct(entity, creature),
                                new MeleeAct(Agent, entity)), new Wrap(Verify)), 5, true);
        }
    }

}