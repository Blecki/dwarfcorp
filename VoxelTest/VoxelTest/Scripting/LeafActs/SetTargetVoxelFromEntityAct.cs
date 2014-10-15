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
        public string EntityName { get; set; }

        public SetTargetVoxelFromEntityAct(CreatureAI creature, string entityName, string voxelOut) :
            base(creature)
        {
            Name = "Set Target Voxel";
            VoxelOutName = voxelOut;
            EntityName = entityName;
        }

        public override IEnumerable<Status> Run()
        {
            Body target = Creature.AI.Blackboard.GetData<Body>(EntityName);
            if (target == null)
            {
                yield return Status.Fail;
            }
            else
            {
                Voxel voxel = Creature.Chunks.ChunkData.GetFirstVoxelUnder(target.BoundingBox.Center());
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