using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class SetTargetVoxel : Action
    {
        public VoxelRef vox;

        public SetTargetVoxel(VoxelRef v)
        {
            vox = v;
            Name = "SetTargetVoxel(" + v.WorldPosition + ")";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.TargetType] = GOAP.TargetType.None;
            PreCondition[GOAPStrings.TargetVoxel] = null;

            Effects = new WorldState();
            Effects[GOAPStrings.TargetType] = GOAP.TargetType.Voxel;
            Effects[GOAPStrings.TargetVoxel] = vox;
            Effects[GOAPStrings.AtTarget] = false;
        }

        public override ValidationStatus ContextValidate(CreatureAIComponent creature)
        {
            return ValidationStatus.Ok;
        }

        public override PerformStatus PerformContextAction(CreatureAIComponent creature, GameTime time)
        {
            creature.TargetVoxel = vox;
            return PerformStatus.Success;
        }

    }
}
