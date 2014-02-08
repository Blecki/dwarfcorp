using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class SatisfyHunger : Goal
    {
        public SatisfyHunger(GOAP agent)
        {
            Name = "SatisfyHunger";
            Priority = 0.0f;
            Reset(agent);
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            return (bool)(creature.Goap.Belief[GOAPStrings.IsHungry]) == true;
        }

        public override void Reset(GOAP agent)
        {
            State[GOAPStrings.IsHungry] = false;

            base.Reset(agent);
        }


        public override void ContextReweight(CreatureAIComponent creature)
        {
            if (creature.Status.Hunger > creature.Stats.HungerThreshold)
            {
                Priority = 100;
                Cost = 0;
            }
            else
            {
                Priority = 0;
                Cost = 100;
            }

            base.ContextReweight(creature);
        }

    }
}
