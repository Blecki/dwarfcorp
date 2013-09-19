using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GuardVoxelAct : CompoundCreatureAct
    {
        public VoxelRef Voxel { get; set; }


        public bool IsGuardDesignation()
        {
            return Agent.Master.IsGuardDesignation(Voxel);
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
