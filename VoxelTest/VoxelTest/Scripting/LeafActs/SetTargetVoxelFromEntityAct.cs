using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class SetTargetVoxelFromEntityAct : CreatureAct
    {
        public string VoxelOutName { get; set; }

        public SetTargetVoxelFromEntityAct(CreatureAIComponent creature, string voxelOut) :
            base(creature)
        {
            Name = "Set Target Voxel";
            VoxelOutName = voxelOut;
        }

        public override IEnumerable<Status> Run()
        {
            if(Agent.TargetComponent == null)
            {
                yield return Status.Fail;
            }
            else
            {
                Voxel voxel = Creature.Chunks.GetFirstVisibleBlockUnder(Agent.TargetComponent.GlobalTransform.Translation, false);
                if(voxel == null)
                {
                    yield return Status.Fail;
                }
                else
                {
                    Agent.Blackboard.SetData(VoxelOutName, voxel.GetReference());
                    yield return Status.Success;
                }
            }
        }
    }

}