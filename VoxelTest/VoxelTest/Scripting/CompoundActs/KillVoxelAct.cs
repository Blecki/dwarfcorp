using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class KillVoxelAct : CompoundCreatureAct
    {
        public VoxelRef Voxel { get; set; }

        public KillVoxelAct()
        {

        }

        public IEnumerable<Status> IncrementAssignment( CreatureAIComponent creature, string designation, int amount)
        {

            VoxelRef vref = creature.Blackboard.GetData<VoxelRef>(designation);
            if(vref != null)
            {
                Designation digDesignation = creature.Faction.GetDigDesignation(vref.GetVoxel(false));

                if(digDesignation != null)
                {
                    digDesignation.numCreaturesAssigned += amount;
                    yield return Status.Success;
                }
                else
                {
                    yield return Status.Success;
                }
            }
            else
            {
                yield return Status.Fail;
            }
             
        }


        public KillVoxelAct(VoxelRef voxel, CreatureAIComponent creature) :
            base(creature)
        {
            Voxel = voxel;
            Name = "Kill Voxel ";
            Tree = new Sequence(
                new SetBlackboardData<VoxelRef>(creature, "DigVoxel", voxel),
                new Wrap(() => IncrementAssignment(creature, "DigVoxel", 1)),
                new GoToVoxelAct(voxel, creature),
                new DigAct(Agent, "DigVoxel"),
                new ClearBlackboardData(creature, "DigVoxel")
                ) | new Wrap(() => IncrementAssignment(creature, "DigVoxel", -1));
        }
    }

}