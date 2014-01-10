﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should find food and eat it.
    /// </summary>
    public class SatisfyHungerTask : Task
    {
        public SatisfyHungerTask()
        {
            Name = "Satisfy Hunger";
        }

        public override Act CreateScript(Creature agent)
        {
            return new FindAndEatFoodAct(agent.AI);
        }

        public override float ComputeCost(Creature agent)
        {
            return agent.Status.Hunger.IsUnhappy() ? 0.0f : 1e13f;
        }

        public override bool IsFeasible(Creature agent)
        {
            return agent.Faction.FindItemsWithTags(new TagList("Food")).Count > 0;
        }
    }
}
