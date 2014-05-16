using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    class PutItemWithTag : CompoundGoal
    {
        public Zone m_zone;
        public TagList m_tags;

        public PutItemWithTag(GOAP agent, TagList tags, Zone zone)
        {
            Name = "Put Item with tag: " + tags.ToString() + " in zone " + zone.ID;
            m_tags = tags;
            m_zone = zone;
            Priority = 1.0f;
            CurrentGoalIndex = -1;
            Reset(agent);
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            
            if (m_zone is Room)
            {
                Room r = (Room)m_zone;

                if (r.IsBuilt && r.RoomType.Name != "BalloonPort")
                {
                    return false;
                }

                if (!creature.Master.RoomDesignator.IsBuildDesignation(r) && r.RoomType.Name != "BalloonPort")
                {
                    return false;
                }
                VoxelBuildDesignation des = creature.Master.RoomDesignator.GetBuildDesignation(r);
                if (des != null)
                {
                    if(m_tags.Tags.Count == 0)
                    {
                        return false;
                    }
                    else 
                    {
                        bool anyUnsatisfied = false;
                        foreach(string tag in m_tags.Tags)
                        {
                            anyUnsatisfied = !des.BuildDesignation.IsResourceSatisfied(tag);
                        }

                        if (!anyUnsatisfied)
                        {
                            return false;
                        }
                    }
                }
            }
             

            if (CurrentGoalIndex == -1 || CurrentGoalIndex >= Goals.Count)
            {
                return true;
            }
            else
            {

                return Goals[CurrentGoalIndex].ContextValidate(creature);
            }
        }

        public override void Reset(GOAP agent)
        {
            Goals.Clear();
            Goals.Add(new GetItemWithTags(agent, m_tags));
            Goals.Add(new MoveToZone(agent, m_zone));
            Goals.Add(new PutHeldItemInZone(agent, m_zone));
           
            base.Reset(agent);
        }

        public override Act GetBehaviorTree(CreatureAIComponent creature)
        {
            if (m_zone is Room)
            {
                Room room = (Room)m_zone;

                if (creature.Master.RoomDesignator.IsBuildDesignation(room))
                {
                    VoxelBuildDesignation voxDesignation = creature.Master.RoomDesignator.GetBuildDesignation(room);

                    if (voxDesignation != null)
                    {
                        RoomBuildDesignation designation = voxDesignation.BuildDesignation;
                        return new PutTaggedRoomItemAct(creature, designation, m_tags);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {

                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
