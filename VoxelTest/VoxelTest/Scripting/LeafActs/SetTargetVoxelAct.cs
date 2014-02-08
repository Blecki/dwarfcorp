using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature sets its current target voxel to the given item.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class SetTargetVoxelAct : CreatureAct
    {
        public VoxelRef Voxel { get; set; }
        public string VoxelName { get; set; }

        public SetTargetVoxelAct(string voxel, CreatureAIComponent creature) :
            base(creature)
        {
            Name = "Set Target Voxel";
            VoxelName = voxel;
            Voxel = null;
        }


        public SetTargetVoxelAct(VoxelRef voxel, CreatureAIComponent creature) :
            base(creature)
        {
            Name = "Set Target Voxel";
            Voxel = voxel;
            VoxelName = "";
        }

        public override IEnumerable<Status> Run()
        {
            if(Voxel == null && VoxelName == "")
            {
                yield return Status.Fail;
            }
            else if(VoxelName == "")
            {
                Agent.TargetVoxel = Voxel;
                yield return Status.Success;
            }
            else
            {
                Agent.TargetVoxel = Agent.Blackboard.GetData<VoxelRef>(VoxelName);
                Voxel = Agent.TargetVoxel;

                if(Agent.TargetVoxel != null)
                {
                    yield return Status.Success;
                }
                else
                {
                    yield return Status.Fail;
                }
            }
        }
    }

}