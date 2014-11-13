using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class SatisfySleepiness : Goal
    {

        public SatisfySleepiness()
        {

        }

        public SatisfySleepiness(GOAP agent)
        {
            Name = "SatisfySleepiness";
            Priority = 0.0f;
            Reset(agent);
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            return (bool) (creature.Goap.Belief[GOAPStrings.IsSleepy]) == true;
        }

        public override void Reset(GOAP agent)
        {
            State[GOAPStrings.IsSleepy] = false;

            base.Reset(agent);
        }


        public override void ContextReweight(CreatureAIComponent creature)
        {
            if(creature.Status.Energy < creature.Stats.SleepyThreshold)
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