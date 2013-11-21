using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    internal class PutItemInZone : Goal
    {
        public Zone m_zone;
        public Item m_item;

        public PutItemInZone(GOAP agent, Item item, Zone zone)
        {
            Name = "Put Item : " + item.ID + " in zone " + zone.ID;
            m_item = item;
            m_zone = zone;
            Priority = 1.0f;
            Reset(agent);
        }

        public override Act GetBehaviorTree(CreatureAIComponent creature)
        {
            return new MoveItemAct(creature, m_item, m_zone);
        }


        public override List<Action> GetPresetPlan(CreatureAIComponent creature, GOAP agent)
        {
            List<Action> toReturn = new List<Action>();
            toReturn.Add(new SetTargetEntity(m_item));
            toReturn.Add(new GoToTargetEntity());
            toReturn.Add(new Stop());
            toReturn.Add(new PickupTargetEntity(agent));
            toReturn.Add(new SetTargetZone(creature, m_zone));
            toReturn.Add(new GoToTargetZone());
            toReturn.Add(new PutHeldObjectInZone(creature, m_zone));
            return toReturn;
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            if(m_zone is Room)
            {
                Room r = (Room) m_zone;

                if(r.IsBuilt && r.RoomType.Name != "BalloonPort")
                {
                    return false;
                }

                if(!creature.Master.RoomDesignator.IsBuildDesignation(r) && r.RoomType.Name != "BalloonPort")
                {
                    return false;
                }
            }

            if((m_item.ReservedFor != null && m_item.ReservedFor != creature) || m_item.Zone == m_zone)
            {
                return false;
            }


            return true;
        }
    }

}