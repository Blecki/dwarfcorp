using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GuardVoxelAct : CompoundCreatureAct
    {
        public VoxelRef Voxel { get; set; }


        public GuardVoxelAct()
        {

        }

        public bool IsGuardDesignation()
        {
            return Agent.Faction.IsGuardDesignation(Voxel);
        }

        public GuardVoxelAct(CreatureAIComponent agent, VoxelRef voxel) :
            base(agent)
        {
            Voxel = voxel;
            Name = "Guard Voxel " + voxel;

            Tree = new Sequence(new GoToVoxelAct(voxel, agent),
                new StopAct(Agent),
                new WhileLoop(new WanderAct(Agent, 1.0f, 0.5f, 0.1f), new Condition(IsGuardDesignation)));
        }
    }

}