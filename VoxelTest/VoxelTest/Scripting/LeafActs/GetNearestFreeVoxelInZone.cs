using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    internal class GetNearestFreeVoxelInZone : CreatureAct
    {
        public Zone TargetZone { get; set; }
        public string OutputVoxel { get; set; }

        public GetNearestFreeVoxelInZone(CreatureAIComponent agent, Zone targetZone, string outputVoxel) :
            base(agent)
        {
            Name = "Get Free Voxel";
            OutputVoxel = outputVoxel;
            TargetZone = targetZone;
        }

        public override IEnumerable<Status> Run()
        {
            if(TargetZone == null)
            {
                yield return Status.Fail;
            }
            else
            {
                VoxelRef v = TargetZone.GetNearestFreeVoxel(Agent.Position);

                if(v != null)
                {
                    Agent.Blackboard.SetData(OutputVoxel, v);
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