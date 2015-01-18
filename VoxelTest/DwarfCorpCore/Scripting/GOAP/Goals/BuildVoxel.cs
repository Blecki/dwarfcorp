using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class BuildVoxel : CompoundGoal
    {
        public TagList Tags { get; set; }
        public VoxelType VoxType { get; set; }
        public VoxelRef Voxel { get; set; }

        public BuildVoxel(GOAP agent, TagList tags, VoxelRef voxel, VoxelType type)
        {
            Name = "Put voxel of type: " + type.name + " on voxel " + voxel.WorldPosition;
            Tags = tags;
            Voxel = voxel;
            VoxType = type;
            Priority = 1.0f;
            CurrentGoalIndex = -1;
            Reset(agent);
            Agent = agent;
        }


        public override void ContextReweight(CreatureAIComponent creature)
        {
            if(Voxel == null || Voxel.TypeName != "empty"
               || !creature.Master.PutDesignator.IsDesignation(Voxel))
                //|| creature.Master.PutDesignator.GetReservedCreature(Voxel) != creature)
            {
                Priority = 0.0f;
                Cost = 100;
            }
            else
            {
                Priority = 1.0f;
                Cost = (Voxel.WorldPosition - creature.Physics.GlobalTransform.Translation).LengthSquared();
            }
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            if(Voxel == null || Voxel.TypeName != "empty"
               || !creature.Master.PutDesignator.IsDesignation(Voxel))
                // || creature.Master.PutDesignator.GetReservedCreature(Voxel) != creature)
            {
                return false;
            }

            if(CurrentGoalIndex == -1 || CurrentGoalIndex >= Goals.Count)
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
            Goals.Add(new GetItemWithTags(agent, Tags));
            Goals.Add(new PutHeldItemOnVoxel(agent, Voxel));

            base.Reset(agent);
        }

        public override Act GetBehaviorTree(CreatureAIComponent creature)
        {
            return new BuildVoxelAct(creature, Voxel, Tags);
        }
    }

}