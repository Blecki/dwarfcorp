using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature plans to a voxel and then follows the path to it.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GoToVoxelAct : CompoundCreatureAct
    {
        public Voxel Voxel { get; set; }

        public GoToVoxelAct() : base()
        {
            
        }

        public GoToVoxelAct(string voxel, PlanAct.PlanType planType, CreatureAI creature) :
            base(creature)
        {
            Name = "Go to Voxel " + voxel;
            Tree = new Sequence(
                    new ForLoop(
                        new Sequence(
                                      new PlanAct(Agent, "PathToVoxel", voxel, planType),
                                      new FollowPathAct(Agent, "PathToVoxel")
                                     )
                                       , 3, true),
                                      new StopAct(Agent));
        }

        public GoToVoxelAct(Voxel voxel, PlanAct.PlanType planType, CreatureAI creature) :
            base(creature)
        {
            Voxel = voxel;
            Name = "Go to Voxel";
            if(Voxel != null)
            {
                Tree = new Sequence(
                                      new SetBlackboardData<Voxel>(Agent, "TargetVoxel", Voxel),
                                      new PlanAct(Agent, "PathToVoxel", "TargetVoxel", PlanAct.PlanType.Adjacent),
                                      new FollowPathAct(Agent, "PathToVoxel"),
                                      new StopAct(Agent));
            }

        }

    }

}