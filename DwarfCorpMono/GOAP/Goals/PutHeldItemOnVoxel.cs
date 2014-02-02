using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    class PutHeldItemOnVoxel : Goal
    {
        Item entityToGater = null;
        VoxelRef voxel { get; set; }
        public PutHeldItemOnVoxel(GOAP agent, VoxelRef vox)
        {
            if (agent != null)
            {
                entityToGater = (Item)agent.Belief[GOAPStrings.HeldObject];
            }
            Name = "Put Held Object on: " + vox.WorldPosition;
            Priority = 0.1f;
            voxel = vox;
            Reset(agent);
        }

        public override void Reset(GOAP agent)
        {
            if (agent != null)
            {
                entityToGater = (Item)agent.Belief[GOAPStrings.HeldObject];
            }

            if (agent != null)
            {
                State[GOAPStrings.TargetType] = GOAP.TargetType.None;
                State[GOAPStrings.TargetEntity] = agent.Belief[GOAPStrings.HeldObject];
                entityToGater = (Item)agent.Belief[GOAPStrings.HeldObject];
                State[GOAPStrings.HeldObject] = null;
                State[GOAPStrings.HandState] = GOAP.HandState.Empty;
                State[GOAPStrings.AtTarget] = true;
                State[GOAPStrings.TargetVoxel] = voxel;
            }

            if (entityToGater != null)
            {
                agent.Items.Add(entityToGater);
            }

            base.Reset(agent);
        }

        public override  List<Action> GetPresetPlan(CreatureAIComponent creature, GOAP agent)
        {
            List<Action> toReturn = new List<Action>();
            toReturn.Add(new SetTargetVoxel(voxel));
            toReturn.Add(new GoToTargetVoxel());
            toReturn.Add(new Stop());
            toReturn.Add(new ConstructVoxel(voxel));
            return toReturn;
        }  

        public override void ContextReweight(CreatureAIComponent creature)
        {
            if (entityToGater == null)
            {
                Priority = 0.0f;
                Cost = 999f;
            }
            else
            {
                Priority = 0.1f / ((creature.Physics.GlobalTransform.Translation - entityToGater.userData.GlobalTransform.Translation).LengthSquared() + 0.01f) + (float)PlayState.random.NextDouble() * 0.1f;
                Cost = ((creature.Physics.GlobalTransform.Translation - entityToGater.userData.GlobalTransform.Translation).LengthSquared());
            }
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            Reset(creature.Goap);
            if (entityToGater == null || entityToGater.userData.IsDead)
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
