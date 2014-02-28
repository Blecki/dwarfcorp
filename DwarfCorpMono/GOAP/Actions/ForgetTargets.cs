using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class ForgetTargets : Action
    {
        public ForgetTargets()
        {
            Name = "ForgetTargets";
            PreCondition = new WorldState();

            Effects = new WorldState();
            Effects[GOAPStrings.AtTarget] = false;
            Effects[GOAPStrings.TargetType] = GOAP.TargetType.None;
            Effects[GOAPStrings.TargetTags] = null;
            Effects[GOAPStrings.ZoneTags] = null;
            Effects[GOAPStrings.CurrentZone] = null;
            Effects[GOAPStrings.TargetZoneType] = null;
            Effects[GOAPStrings.TargetZoneFull] = false;

            Cost = 1.0f;
        }

        public override Action.PerformStatus PerformContextAction(CreatureAIComponent creature, GameTime time)
        {
            creature.TargetComponent = null;
            creature.TargetRoom = null;
            creature.TargetStockpile = null;
            creature.TargetVoxDesignation = null;
            creature.TargetVoxel = null;
            return Action.PerformStatus.Success;
        }
    }

}
