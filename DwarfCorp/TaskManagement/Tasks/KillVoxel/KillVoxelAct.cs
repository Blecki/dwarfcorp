using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature goes to a voxel, and then hits the voxel until it is destroyed.
    /// </summary>
    public class KillVoxelAct : CompoundCreatureAct
    {
        public KillVoxelAct()
        {

        }

        public bool CheckIsDigDesignation(CreatureAI creature, KillVoxelTask OwnerTask)
        {
            if (OwnerTask.Voxel.IsValid)
                return creature.World.PersistentData.Designations.IsVoxelDesignation(OwnerTask.Voxel, DesignationType.Dig);

            return false;
        }

        public IEnumerable<Act.Status> Cleanup(CreatureAI creature, KillVoxelTask OwnerTask)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
            {
                if (creature.Faction == creature.World.PlayerFaction)
                {
                    //creature.World.MakeAnnouncement(String.Format("{0} cancelled dig task because it is unreachable", creature.Stats.FullName));
                    //creature.World.TaskManager.CancelTask(OwnerTask);
                }
            }
            yield return Act.Status.Success;
        }

        public KillVoxelAct(CreatureAI creature, KillVoxelTask OwnerTask) :
            base(creature)
        {
            Name = "Kill DestinationVoxel " + OwnerTask.Voxel.WorldPosition;
            Tree = 
                new Domain(() => CheckIsDigDesignation(creature, OwnerTask),
                new Sequence(
                    ActHelper.CreateEquipmentCheckAct(creature, "Tool", ActHelper.EquipmentFallback.AllowDefault, "Pick"),
                    new GoToVoxelAct(OwnerTask.Voxel, PlanAct.PlanType.Radius, creature) { Radius = 2.0f },
                    new DigAct(Agent, OwnerTask)))
                | new Wrap(() => Cleanup(creature, OwnerTask));
        }
    }
}