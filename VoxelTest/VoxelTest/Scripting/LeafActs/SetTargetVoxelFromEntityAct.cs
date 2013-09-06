using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class SetTargetVoxelFromEntityAct : CreatureAct
    {

        public SetTargetVoxelFromEntityAct(CreatureAIComponent creature) :
            base(creature)
        {

        }

        public override IEnumerable<Status> Run()
        {

            if (Agent.TargetComponent == null)
            {
                yield return Status.Fail;
            }
            else
            {
                Voxel voxel = Creature.Chunks.GetFirstVisibleBlockUnder(Agent.TargetComponent.GlobalTransform.Translation, false);
                if (voxel == null)
                {
                    yield return Status.Fail;
                }
                else
                {
                    Agent.TargetVoxel = voxel.GetReference();
                    yield return Status.Success;
                }
            }
        }

    }
}
