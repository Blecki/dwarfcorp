using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class DeconstructObjectAct : CompoundCreatureAct
    {
        public GameComponent Entity { get; set; }

        public DeconstructObjectAct()
        {
        }

        public bool Verify(CreatureAI creature)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
                return false;

            if (Entity == null || Entity.IsDead)
                return false;

            return true;
        }

        public IEnumerable<Act.Status> RemoveObject(CreatureAI creature)
        {
            Entity.Die();
            yield return Act.Status.Success;
        }

        public DeconstructObjectAct(GameComponent entity, CreatureAI creature) :
            base(creature)
        {
            Entity = entity;
            Name = "Remove Entity";

            Tree = new Domain(() => Verify(creature),
                  new Sequence(
                    new GoToEntityAct(entity, creature) { MovingTarget = false, PlanType = PlanAct.PlanType.Adjacent, Radius = 0.0f },
                    new Wrap(() => RemoveObject(creature))));
        }

    }
}