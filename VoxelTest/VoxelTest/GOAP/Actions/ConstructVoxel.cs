using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class ConstructVoxel : Action
    {
        public VoxelRef voxel;
        public ConstructVoxel(VoxelRef vox)
        {
            voxel = vox;
            Name = "ConstructVoxel : " + vox.WorldPosition;
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.HandState] = GOAP.HandState.Full;
            PreCondition[GOAPStrings.TargetType] = GOAP.TargetType.Voxel;
            PreCondition[GOAPStrings.AtTarget] = true;
            PreCondition[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;
            PreCondition[GOAPStrings.TargetVoxel] = voxel;

            Effects = new WorldState();
            Effects[GOAPStrings.HandState] = GOAP.HandState.Empty;
            Effects[GOAPStrings.HeldObject] = null;
            Effects[GOAPStrings.TargetType] = GOAP.TargetType.None;
            Effects[GOAPStrings.HeldItemTags] = null;
            Effects[GOAPStrings.TargetVoxel] = voxel;
           
            Cost = 0.1f;
        }

        public override void Apply(WorldState state)
        {
            Item item = (Item)(state[GOAPStrings.HeldObject]);

            if (item != null)
            {
                state[GOAPStrings.TargetEntity] = new Item(item.ID, item.Zone, item.userData);
            }
            else
            {
                state[GOAPStrings.TargetEntity] = null;
            }

            base.Apply(state);
        }

        public override void UnApply(WorldState state)
        {
            Item item = (Item)(state[GOAPStrings.TargetEntity]);

            if (item != null)
            {
                state[GOAPStrings.HeldObject] = new Item(item.ID, null, item.userData);
            }
            else
            {
                state[GOAPStrings.HeldObject] = null;
            }

            base.UnApply(state);
        }

        public override Action.PerformStatus PerformContextAction(CreatureAIComponent creature, GameTime time)
        {
            Item item = (Item)creature.Goap.Belief[GOAPStrings.HeldObject];


            if (item == null)
            {
                return Action.PerformStatus.Failure;
            }

          
            LocatableComponent grabbed = creature.Hands.GetFirstGrab();
            creature.Hands.UnGrab(grabbed);

            grabbed.Die();

            if (creature.Master.PutDesignator.IsDesignation(voxel))
            {
                PutDesignation put = creature.Master.PutDesignator.GetDesignation(voxel);
                put.Put(creature.Master.Chunks);
                creature.Master.PutDesignator.Designations.Remove(put);
            }
                

            return PerformStatus.Success;
                
        }

        public override ValidationStatus ContextValidate(CreatureAIComponent creature)
        {
            Item item = (Item)creature.Goap.Belief[GOAPStrings.HeldObject];

            if (item == null)
            {
                return ValidationStatus.Replan;
            }

            return ValidationStatus.Ok;
        }
    }

}
