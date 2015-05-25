using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature goes to a voxel, and then hits the voxel until it is destroyed.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class KillVoxelAct : CompoundCreatureAct
    {
        public Voxel Voxel { get; set; }

        public KillVoxelAct()
        {

        }


        public IEnumerable<Status> IncrementAssignment( CreatureAI creature, string designation, int amount)
        {
            Voxel vref = creature.Blackboard.GetData<Voxel>(designation);
            if(vref != null)
            {
                BuildOrder digBuildOrder = creature.Faction.GetDigDesignation(vref);

                if(digBuildOrder != null)
                {
                    digBuildOrder.NumCreaturesAssigned += amount;
                    yield return Status.Success;
                }
                else
                {
                    yield return Status.Fail;
                }
            }
            else
            {
                yield return Status.Fail;
            }
             
        }

        public IEnumerable<Status> CheckIsDigDesignation(CreatureAI creature, string designation)
        {
            Voxel vref = creature.Blackboard.GetData<Voxel>(designation);
            if (vref != null)
            {
                BuildOrder digBuildOrder = creature.Faction.GetDigDesignation(vref);

                if (digBuildOrder != null)
                {
                    yield return Status.Success;
                }
                else
                {
                    yield return Status.Fail;
                }

            }

            yield return Status.Fail;
        }

        public KillVoxelAct(Voxel voxel, CreatureAI creature) :
            base(creature)
        {
            Voxel = voxel;
            Name = "Kill Voxel " + voxel.Position;
            Tree = new Sequence(
                new SetBlackboardData<Voxel>(creature, "DigVoxel", voxel),
                new Sequence(
                              new Wrap(() => IncrementAssignment(creature, "DigVoxel", 1)),
                              new GoToVoxelAct(voxel, PlanAct.PlanType.Adjacent, creature),
                              new Wrap(() => CheckIsDigDesignation(creature, "DigVoxel")),
                              new DigAct(Agent, "DigVoxel"),
                              new ClearBlackboardData(creature, "DigVoxel")
                            ) 
                            | new Wrap(() => IncrementAssignment(creature, "DigVoxel", -1)) & false);
        }
    }

}