using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GatherPotionsTask : Task
    {
        public GatherPotionsTask()
        {
            Name = "Gather Potions";
            ReassignOnDeath = false;
            AutoRetry = false;
            Priority = TaskPriority.Medium;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 1.0f;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return false;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return agent.World.ListResourcesWithTag(Resource.ResourceTags.Potion).Count > 0 ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override Act CreateScript(Creature agent)
        {
            return new GetResourcesAct(agent.AI, new List<Quantitiy<Resource.ResourceTags>>() { new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Potion)});
        }
    }
}
