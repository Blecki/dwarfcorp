using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature goes to a voxel named in the blackboard.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GoToNamedVoxelAct : CompoundCreatureAct
    {
        public string VoxelName;
        public PlanAct.PlanType PlanType;
        public float Radius;

        public GoToNamedVoxelAct() : base()
        {

        }

        public GoToNamedVoxelAct(string voxel, PlanAct.PlanType planType, CreatureAI creature, float radius = 3.0f) :
            base(creature)
        {
            Radius = radius;
            PlanType = planType;
            VoxelName = voxel;
            Name = "Go to DestinationVoxel " + voxel;
        }

        public override void Initialize()
        {
            if (!String.IsNullOrEmpty(VoxelName))
            {
                Tree = new Sequence(
                    new Repeat(new Sequence(
                    //new PlanWithGreedyFallbackAct() { Agent = Agent, PathName = "PathToVoxel", VoxelName = VoxelName, PlanType = PlanType, Radius = Radius},
                    new PlanAct(Agent, "PathToVoxel", VoxelName, PlanType) { MaxTimeouts = 1, Radius = Radius },
                    new FollowPathAct(Agent, "PathToVoxel")), 10, true),
                    new StopAct(Agent));
            }
            base.Initialize();
        }
    }
}