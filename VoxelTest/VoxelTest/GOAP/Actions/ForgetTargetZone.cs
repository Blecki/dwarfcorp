using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class ForgetTargetZone : Action
    {
        public ForgetTargetZone()
        {
            Name = "ForgetTargetZone";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.TargetType] = GOAP.TargetType.Zone;

            Effects = new WorldState();
            Effects[GOAPStrings.AtTarget] = false;
            Effects[GOAPStrings.TargetType] = GOAP.TargetType.Zone;
            Effects[GOAPStrings.ZoneTags] = null;
            Effects[GOAPStrings.CurrentZone] = null;
            Effects[GOAPStrings.TargetZoneType] = null;
            Effects[GOAPStrings.TargetZoneFull] = false;

            Cost = 1.0f;
        }
    }

}