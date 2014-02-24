using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    class BuildVoxel : CompoundGoal
    {
        public TagList m_tags { get; set; }
        public VoxelType m_voxType { get; set; }
        public VoxelRef m_voxel { get; set; }

        public BuildVoxel(GOAP agent, TagList tags, VoxelRef voxel, VoxelType type)
        {
            Name = "Put voxel of type: " + type.name + " on voxel " + voxel.WorldPosition;
            m_tags = tags;
            m_voxel = voxel;
            m_voxType = type;
            Priority = 1.0f;
            CurrentGoalIndex = -1;
            Reset(agent);
            Agent = agent;
        }


        public override void ContextReweight(CreatureAIComponent creature)
        {
            if (m_voxel == null || m_voxel.TypeName != "empty" 
                || !creature.Master.PutDesignator.IsDesignation(m_voxel) )
                //|| creature.Master.PutDesignator.GetReservedCreature(m_voxel) != creature)
            {
                Priority = 0.0f;
                Cost = 100;
            }
            else
            {
                Priority = 1.0f;
                Cost = (m_voxel.WorldPosition - creature.Physics.GlobalTransform.Translation).LengthSquared();
            }
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {

            if (m_voxel == null || m_voxel.TypeName != "empty" 
                || !creature.Master.PutDesignator.IsDesignation(m_voxel))
               // || creature.Master.PutDesignator.GetReservedCreature(m_voxel) != creature)
            {
                return false;
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
            Goals.Add(new PutHeldItemOnVoxel(agent, m_voxel));
           
            base.Reset(agent);
        }

        public override Act GetBehaviorTree(CreatureAIComponent creature)
        {
            return new BuildVoxelAct(creature, m_voxel, m_tags);
        }


    }
}
