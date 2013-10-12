using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GoToNamedVoxelAct : CompoundCreatureAct
    {
        public string Voxel { get; set; }

        public GoToNamedVoxelAct(string voxel, CreatureAIComponent creature) :
            base(creature)
        {
            Voxel = voxel;
            Name = "Go to Voxel " + voxel;
           
        }

        public override void Initialize()
        {
            Tree = new Sequence(new SetTargetVoxelAct(Voxel, Agent),
                                new PlanAct(Agent),
                                new FollowPathAct(Agent),
                                new StopAct(Agent));

            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            return base.Run();
        }
    }
}
