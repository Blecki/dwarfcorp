using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class SetTargetVoxelFromEntityAct : CreatureAct
    {
        public string VoxelOutName { get; set; }

        public SetTargetVoxelFromEntityAct(CreatureAI creature, string voxelOut) :
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
                Voxel voxel = Creature.Chunks.ChunkData.GetFirstVoxelUnder(Agent.TargetComponent.BoundingBox.Center());
                if(voxel == null)
                {
                    yield return Status.Fail;
                }
                else
                {
                    Agent.Blackboard.SetData(VoxelOutName, voxel);
                    yield return Status.Success;
                }
            }
        }
    }

}