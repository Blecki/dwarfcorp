using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GoToVoxelAct : CompoundCreatureAct
    {
        public VoxelRef Voxel { get; set; }

        public GoToVoxelAct() : base()
        {
            
        }

        public GoToVoxelAct(string voxel, CreatureAIComponent creature) :
            base(creature)
        {
            Name = "Go to Voxel " + voxel;
            Tree = new Sequence(
                    new ForLoop(
                        new Sequence(
                                      new PlanAct(Agent, "PathToVoxel", voxel),
                                      new FollowPathAct(Agent, "PathToVoxel")
                                     )
                                       , 3, true),
                                      new StopAct(Agent));
        }

        public GoToVoxelAct(VoxelRef voxel, CreatureAIComponent creature) :
            base(creature)
        {
            Voxel = voxel;
            Name = "Go to Voxel";
            if(Voxel != null)
            {
                Tree = new Sequence(
                    new ForLoop(
                        new Sequence(
                                      new SetBlackboardData<VoxelRef>(Agent, "TargetVoxel", Voxel),
                                      new PlanAct(Agent, "PathToVoxel", "TargetVoxel"),
                                      new FollowPathAct(Agent, "PathToVoxel")
                                     )
                                       , 3, true),
                                      new StopAct(Agent));
            }

        }

    }

}