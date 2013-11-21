using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    internal class MoveToZone : Goal
    {
        private Zone m_zone = null;

        public MoveToZone(GOAP agent, Zone zone)
        {
            Name = "Go to Zone: " + zone.ID;
            Priority = 0.1f;
            m_zone = zone;
            Reset(agent);
        }

        public override void Reset(GOAP agent)
        {
            State[GOAPStrings.TargetType] = GOAP.TargetType.Zone;
            State[GOAPStrings.TargetZone] = m_zone;
            State[GOAPStrings.AtTarget] = true;
            State[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;

            base.Reset(agent);
        }


        public override void ContextReweight(CreatureAIComponent creature)
        {
            if(m_zone == null)
            {
                Priority = 0.0f;
                Cost = 999f;
            }
            else
            {
                Priority = 0.5f;
                Cost = 0.5f;
            }
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            if(m_zone == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

}